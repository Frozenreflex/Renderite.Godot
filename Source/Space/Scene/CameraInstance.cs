using System;
using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source.Scene;

public class CameraInstance : SceneInstance
{
    private Camera3D _camera;
    private bool _useTransformScale;
    private float _nearClip;
    private float _farClip;
    private float _orthographicSize;

    protected override void OnInitialize()
    {
        base.OnInitialize();
        _camera = new Camera3D();
    }

    protected override void BaseOnGlobalTransformChanged(TransformNode obj)
    {
        var transform = obj.GlobalTransform;
        transform.Basis *= new Basis(new Quaternion(Vector3.Right, Mathf.Pi));
        _camera.Transform = transform;
        RecalculateParams();
    }

    public void UpdateState(CameraState state)
    {
        if (state.renderTextureAssetId == -1) return;
        var viewport = RendererManager.Instance.AssetManager.TextureManager
            .GetOrCreateRenderTexture(state.renderTextureAssetId).Viewport;
        if (!InstanceValid)
        {
            viewport.AddChild(_camera);
            RenderingServer.ViewportAttachCamera(viewport.GetViewportRid(), _camera.GetCameraRid());
            InstanceValid = true;
        }

        viewport.RenderTargetUpdateMode =
            state.enabled ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
        viewport.DebugDraw = state.renderShadows ? Viewport.DebugDrawEnum.Disabled : Viewport.DebugDrawEnum.Unshaded;

        _useTransformScale = state.useTransformScale;
        _nearClip = state.nearClip;
        _farClip = state.farClip;
        _orthographicSize = state.orthographicSize;
        _camera.KeepAspect = Camera3D.KeepAspectEnum.Height; // ??? this shouldn't be right?
        _camera.Projection = state.projection switch
        {
            CameraProjection.Perspective => Camera3D.ProjectionType.Perspective,
            CameraProjection.Orthographic => Camera3D.ProjectionType.Orthogonal,
            _ => Camera3D.ProjectionType.Perspective, // TODO: what is panoramic?
        };
        _camera.Fov = state.fieldOfView;
        _camera.HOffset = state.viewport.x;
        _camera.VOffset = state.viewport.y; // TODO: width height? I think we need something custom for this...
        _camera.CullMask = state is { renderPrivateUI: true, selectiveRenderCount: > 0 }
            ? 2u
            : 1u; // TODO: actually implement this, this is to get the dash working for now
        viewport.RenderTargetClearMode = state.clearMode switch
        {
            CameraClearMode.Nothing => SubViewport.ClearMode.Never,
            _ => SubViewport.ClearMode.Always
            // TODO: handle other clear modes
        };
        // TODO: depth, and a bunch of other props that might not be possible in godot...
        RecalculateParams();
    }

    private void RecalculateParams()
    {
        var scale = _camera.Scale;
        var scaleModifier = _useTransformScale ? (scale.X + scale.Y + scale.Z) * (1f / 3f) : 1f;
        if (float.IsNaN(scaleModifier))
            scaleModifier = 1f;
        scaleModifier = Mathf.Clamp(scaleModifier, 1E-05f, 1000000f);
        _camera.Far = _farClip * scaleModifier;
        _camera.Near = _nearClip * scaleModifier;
        _camera.Size = _orthographicSize * scaleModifier * 2f;
        scale.X = -scale.X; // cameras are always mirrored???
        _camera.Scale = scale;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        _camera.QueueFree();
    }
}