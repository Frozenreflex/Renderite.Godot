using System;
using System.Collections.Generic;
using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public partial class RendererManager : Node
{
    public static RendererManager Instance;
    
    public Dictionary<int, RenderSpace> Spaces = new();
    public AssetManager AssetManager = new();
    private RendererInitData _initData;
    private bool _initReceived;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
    }
    public void Update()
    {
        if (_initData is not null)
        {
            
        }
    }
    public void HandleFrameUpdate(FrameSubmitData submitData)
    {
        /*
        LastFrameIndex = submitData.frameIndex;
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
    public void HandleRenderCommand(RendererCommand command)
    {
        if (command is KeepAlive) return;
        
        if (!_initReceived)
        {
            _initData = command as RendererInitData ?? throw new Exception();
            _initReceived = true;
        }
        else
        {
            switch (command)
            {
                
                default:
                    AssetManager.HandleRenderCommand(command);
                    break;
            }
        }
    }
}
