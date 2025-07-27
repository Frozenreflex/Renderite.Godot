using Godot;

namespace Renderite.Godot.Source.Scene;

public class MeshInstanceManager : AssetSceneInstanceManager
{
    public MeshAsset Mesh
    {
        get;
        set
        {
            if (field is not null && TrackMeshAssetChanges) field.MeshChanged -= OnMeshAssetChanged;
            field = value;
            if (value is null) RenderingServer.InstanceSetBase(InstanceRid, NullRid);
            else
            {
                RenderingServer.InstanceSetBase(InstanceRid, Mesh.AssetID);
                if (TrackMeshAssetChanges) field.MeshChanged += OnMeshAssetChanged;
            }
            OnMeshChanged();
        }
    }
    protected virtual bool TrackMeshAssetChanges => false;
    public RenderingServer.ShadowCastingSetting ShadowCastingMode = (RenderingServer.ShadowCastingSetting)(-1);
    protected virtual void OnMeshChanged()
    {
        
    }
    protected virtual void OnMeshAssetChanged()
    {
        
    }
}
