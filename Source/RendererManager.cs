using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Shared;
using Renderite.Godot.Source.SharedMemory;
using System.IO;

namespace Renderite.Godot.Source;

public partial class RendererManager : Node
{
    public static RendererManager Instance;

    public Dictionary<int, RenderSpace> Spaces = new();
    public AssetManager AssetManager = new();
    private RendererInitData _initData;
    private bool _initReceived;
    private bool _initFinalized;

    public SharedMemoryAccessor SharedMemory { get; private set; }
    public MessagingManager PrimaryMessagingManager;
    public MessagingManager BackgroundMessagingManager;

    public int LastFrameIndex { get; private set; } = -1;
    private volatile FrameSubmitData _frameData;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        string queueName = null;
        var queueCapacity = 8388608;

        var args = OS.GetCmdlineUserArgs();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();
            if (arg.Contains("queuename"))
            {
                var next = args[i + 1];
                i++;
                queueName = next;
            }
            else if (arg.Contains("queuecapacity"))
            {
                var next = args[i + 1];
                i++;
                queueCapacity = int.Parse(next);
            }
        }

        if (queueName is null)
        {
            const string shmPath = "/dev/shm";
            const string prefix = "sem.ct.ip.";
            const string suffix = "PrimaryA";

            var files = Directory.EnumerateFiles(shmPath)
                .Where(f => Path.GetFileName(f).StartsWith(prefix) && f.EndsWith(suffix))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            if (files.Count > 0)
            {
                queueName = Path.GetFileName(files[0])[prefix.Length..^suffix.Length];
                GD.Print($"Detected queueName from file: {files[0]}");
            }
            else
            {
                GD.PrintErr("No queue name specified!");
                GetTree().Quit();
                return;
            }
        }

        GD.Print($"Connecting to {queueName} (capacity: {queueCapacity})");
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
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!_initFinalized)
            return;

        var frameStartData = new FrameStartData
        {
            lastFrameIndex = LastFrameIndex,
            inputs = InputManager.Instance.GetInputState()
        };
        PrimaryMessagingManager.SendCommand(frameStartData);

        while (_frameData == null)
        {
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
        /*
        NearClip = submitData.nearClip;
        FarClip = submitData.farClip;
        DesktopFOV = submitData.desktopFOV;
        */
        RenderSpace activeRenderSpace = null;
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
            if (spaceData.isActive && !spaceData.isOverlay)
                activeRenderSpace = activeRenderSpace == null
                    ? renderSpace
                    : throw new Exception("Multiple spaces are set to active");
        }

        if (submitData.outputState is not null)
            InputManager.Instance.Handle(submitData.outputState);
        /*
        HeadOutput headOutput = this.UpdateVR_Active(submitData.vrActive);
        if ((UnityEngine.Object) renderSpace1 != (UnityEngine.Object) null)
            headOutput.UpdatePositioning(renderSpace1);
            */
        /*
        foreach (var (index, space) in Spaces)
        {
            if (space.IsActive && space.IsOverlay) space.UpdateOverlayPositioning(headOutput.transform);
        }
        */
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
            RendererInitResult rendererInitResult = new RendererInitResult();
            rendererInitResult.actualOutputDevice = HeadOutputDevice.Screen;
            rendererInitResult.stereoRenderingMode = "MultiPass";
            rendererInitResult.maxTextureSize = 16384;
            rendererInitResult.isGPUTexturePOTByteAligned = true;
            rendererInitResult.supportedTextureFormats = Enum.GetValues<TextureFormat>().Where(i => i.Supported()).ToList();
            /*
            rendererInitResult.supportedTextureFormats =
            [
                TextureFormat.RGB24,
                TextureFormat.RGBA32,
            ];
            */
            PrimaryMessagingManager.SendCommand(rendererInitResult);
        }
        else
        {
            switch (command)
            {
                case RendererInitFinalizeData initFinalize:
                    _initFinalized = true;
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
                DebugDraw3D.DrawText(skinnedMesh.Base.GlobalPosition, $"Space {spaceIndex}\nSkinned Mesh {index}\n{skinnedMesh.Mesh is not null}", textSize);
                foreach (var bone in skinnedMesh.TrackedBones)
                {
                    if (bone.Node is not null) DebugDraw3D.DrawText(bone.Node.GlobalPosition, $"Space {spaceIndex}\nSkinned Mesh {index}\nBone {bone.BoneIndex}", textSize);
                }
            }
            foreach (var (mesh, index) in space.Meshes.WithIndex())
            {
                DebugDraw3D.DrawText(mesh.Base.GlobalPosition, $"Space {spaceIndex}\nMesh {index}\n{mesh.Mesh is not null}", textSize);
            }
        }
    }
}
