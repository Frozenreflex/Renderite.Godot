using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Shared;
using Renderite.Godot.Source.SharedMemory;
using Cloudtoid.Interprocess;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Renderite.Godot.Source;

public partial class RendererManager : Node
{
    public static RendererManager Instance;

    private Bootstrapper _bootstrapper;
    private bool _bootstrapped;

    public SharedMemoryAccessor SharedMemory { get; private set; }
    public MessagingManager PrimaryMessagingManager;
    public MessagingManager BackgroundMessagingManager;

    public Dictionary<int, RenderSpace> Spaces = new();
    public AssetManager AssetManager = new();
    private RendererInitData _initData;
    private bool _initReceived;
    private bool _initFinalized;

    private readonly FrameStartData _frameStartData = new FrameStartData
    {
        performance = new PerformanceState()
    };

    public int LastFrameIndex { get; private set; } = -1;
    private volatile FrameSubmitData _frameData;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        var args = OS.GetCmdlineArgs();

        GD.Print("Starting...");
        _bootstrapper = new Bootstrapper(() =>
        {
            GD.Print("Quitting because the Resonite process quit.");
            GetTree().CallDeferred("quit");
        });

        var resonitePath = OS.HasFeature("windows")
            ? @"C:\Program Files (x86)\Steam\steamapps\common\Resonite\"
            : System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                ".local", "share", "Steam", "steamapps", "common", "Resonite");
        var dotnetExecutable = "dotnet";
        var launchResonite = true;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();
            if (arg.Contains("resonitepath"))
            {
                var next = args[i + 1];
                i++;
                resonitePath = next;
            }
            else if (arg.Contains("executable"))
            {
                var next = args[i + 1];
                i++;
                dotnetExecutable = next;
            }
            else if (arg.Contains("noautolaunch"))
            {
                launchResonite = false;
            }
        }

        if (!launchResonite)
        {
            GD.Print(
                $"Resonite auto launch disabled, please run Renderite.Host.dll manually with -shmprefix {_bootstrapper.ShmPrefix}");
            return;
        }

        var resoniteArgs = args.SkipWhile(arg => arg.ToLower() != "--resoniteargs").Skip(1).ToArray().Join(" ");
        _bootstrapper.LaunchResonite(resonitePath, dotnetExecutable, resoniteArgs);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            GD.Print("Requesting shutdown...");
            PrimaryMessagingManager.SendCommand(new RendererShutdownRequest());
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!_initFinalized)
        {
            if (!_bootstrapped && _bootstrapper.TryGetQueueConnection(out var queueName, out var queueCapacity))
            {
                GD.Print($"Received queue info, connecting to {queueName} (capacity: {queueCapacity})");
                PrimaryMessagingManager = new MessagingManager(PackerMemoryPool.Instance);
                PrimaryMessagingManager.CommandHandler = HandleRenderCommand;
                PrimaryMessagingManager.FailureHandler = HandleMessagingFailure;
                PrimaryMessagingManager.WarningHandler = GD.Print;
                PrimaryMessagingManager.Connect(queueName + "Primary", isAuthority: false, queueCapacity);
                BackgroundMessagingManager = new MessagingManager(PackerMemoryPool.Instance);
                BackgroundMessagingManager.CommandHandler = HandleRenderCommand;
                BackgroundMessagingManager.FailureHandler = HandleMessagingFailure;
                BackgroundMessagingManager.WarningHandler = GD.Print;
                BackgroundMessagingManager.Connect(queueName + "Background", isAuthority: false, queueCapacity);
                GD.Print("Connected!");
                _bootstrapped = true;
            }

            return;
        }

        _frameStartData.lastFrameIndex = LastFrameIndex;
        _frameStartData.inputs = InputManager.Instance.GetInputState();
        _frameStartData.performance.fps = (float)Performance.GetMonitor(Performance.Monitor.TimeFps);
        //TODO: other performance stats
        PrimaryMessagingManager.SendCommand(_frameStartData);

        while (_frameData == null)
        {
            if (_bootstrapper.ResoniteQuit)
                return;
        } // Maybe don't tight loop here?

        if (_frameData != null)
        {
            HandleFrameUpdate(_frameData);
            PackerMemoryPool.Instance.Return(_frameData);
            _frameData = null;
        }

        //DebugDraw();
    }

    private void HandleFrameUpdate(FrameSubmitData submitData)
    {
        LastFrameIndex = submitData.frameIndex;
        RenderSpace activeRenderSpace = null;
        RenderSpace activeOverlayRenderSpace = null;
        foreach (var spaceData in submitData.renderSpaces)
        {
            if (!Spaces.TryGetValue(spaceData.id, out var renderSpace))
            {
                renderSpace = new RenderSpace
                {
                    Id = spaceData.id,
                };
                AddChild(renderSpace);
                Spaces.Add(spaceData.id, renderSpace);
            }

            renderSpace.HandleUpdate(spaceData);
            if (renderSpace.IsActive && !renderSpace.IsOverlay)
                activeRenderSpace = activeRenderSpace == null
                    ? renderSpace
                    : throw new Exception("Multiple spaces are set to active");
            if (renderSpace.IsActive && renderSpace.IsOverlay)
            {
                activeOverlayRenderSpace = renderSpace;
            }
        }

        if (activeRenderSpace is not null)
        {
            if (activeOverlayRenderSpace is not null)
                activeOverlayRenderSpace.UpdateOverlayPositioning(activeRenderSpace.RootTransform);
            HeadOutputManager.Instance.Handle(submitData, activeRenderSpace);
        }

        if (submitData.outputState is not null)
            InputManager.Instance.Handle(submitData.outputState);
    }

    private void HandleRenderCommand(RendererCommand command)
    {
        if (command is KeepAlive) return;

        if (!_initReceived)
        {
            _initData = command as RendererInitData ?? throw new Exception();
            _initReceived = true;

            GD.Print($"Shared memory prefix: {_initData.sharedMemoryPrefix}");
            GD.Print($"Main process PID: {_initData.mainProcessId}");
            GD.Print($"Debug frame pacing: {_initData.debugFramePacing}");
            GD.Print($"Output device: {_initData.outputDevice}");

            // Send some fake data for now
            SharedMemory = new SharedMemoryAccessor(_initData.sharedMemoryPrefix);
            RendererInitResult rendererInitResult = new RendererInitResult
            {
                rendererIdentifier = "Renderite.Godot",
                actualOutputDevice =
                    HeadOutputManager.Instance.IsXR
                        ? HeadOutputDevice.SteamVR
                        : HeadOutputDevice.Screen, // This is a lie, no SteamVR to be found here
                stereoRenderingMode = "MultiPass",
                maxTextureSize = 16384,
                isGPUTexturePOTByteAligned = true,
                supportedTextureFormats = Enum.GetValues<TextureFormat>().Where(i => i.Supported()).ToList()
            };
            PrimaryMessagingManager.SendCommand(rendererInitResult);
        }
        else
        {
            switch (command)
            {
                case RendererInitFinalizeData initFinalize:
                    _initFinalized = true;
                    PackerMemoryPool.Instance.Return(initFinalize);
                    GD.Print("Init finalized!");
                    break;
                case FrameSubmitData submitData:
                    _frameData = submitData;
                    break;
                default:
                    AssetManager.HandleRenderCommand(command);
                    break;
            }
        }
    }

    private void HandleMessagingFailure(Exception ex)
    {
        GD.Print("Exception in messaging system:\n" + ex);
    }

    private void DebugDraw()
    {
        const int textSize = 3;
        foreach (var (spaceIndex, space) in Spaces)
        {
            foreach (var (skinnedMesh, index) in space.SkinnedMeshes.WithIndex())
            {
                DebugDraw3D.DrawText(skinnedMesh.Base.GlobalPosition,
                    $"Space {spaceIndex}\nSkinned Mesh {index}\n{skinnedMesh.Mesh is not null}", textSize);
                foreach (var bone in skinnedMesh.TrackedBones)
                {
                    if (bone.Node is not null)
                        DebugDraw3D.DrawText(bone.Node.GlobalPosition,
                            $"Space {spaceIndex}\nSkinned Mesh {index}\nBone {bone.BoneIndex}", textSize);
                }
            }

            foreach (var (mesh, index) in space.Meshes.WithIndex())
            {
                DebugDraw3D.DrawText(mesh.Base.GlobalPosition,
                    $"Space {spaceIndex}\nMesh {index}\n{mesh.Mesh is not null}", textSize);
            }
        }
    }
}