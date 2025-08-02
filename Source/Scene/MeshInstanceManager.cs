using Godot;

namespace Renderite.Godot.Source.Scene;

public class MeshInstanceManager : AssetSceneInstanceManager
{
    public MeshAsset Mesh
    {
        get;
        set
        {
            if (field == value) return;
            if (field is not null) field.MeshChanged -= OnMeshAssetChanged;
            field = value;
            UpdateRenderingServerRid();
            if (field is not null) field.MeshChanged += OnMeshAssetChanged;
            OnMeshChanged();
        }
    }
    private void UpdateRenderingServerRid()
    {
        InstanceValid = Mesh is not null;
        RenderingServer.InstanceSetBase(InstanceRid, Mesh?.AssetID ?? NullRid);
    }
    public RenderingServer.ShadowCastingSetting ShadowCastingMode = (RenderingServer.ShadowCastingSetting)(-1);
    protected virtual void OnMeshChanged()
    {
        
    }
    protected virtual void OnMeshAssetChanged()
    {
        UpdateRenderingServerRid();
    }
}
