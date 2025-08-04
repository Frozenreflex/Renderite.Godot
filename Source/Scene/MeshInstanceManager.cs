using System.Collections.Generic;
using System.Linq;
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
            if (InstanceRid == NullRid) return;
            UpdateRenderingServerRid();
            if (field is not null) field.MeshChanged += OnMeshAssetChanged;
            OnMeshChanged();
        }
    }
    private void UpdateRenderingServerRid()
    {
        InstanceValid = Mesh is not null;
        RenderingServer.InstanceSetBase(InstanceRid, Mesh?.AssetID ?? NullRid);
        UpdateMaterials();
    }
    public RenderingServer.ShadowCastingSetting ShadowCastingMode = (RenderingServer.ShadowCastingSetting)(-1);

    public MaterialInstance[] Materials = [];

    public void UpdateMaterials()
    {
        if (!InstanceValid) return;
        var matCount = Mesh.SurfaceCount;
        for (var i = 0; i < matCount; i++)
        {
            var mat = Materials.ElementAtOrDefault(i);
            RenderingServer.InstanceSetSurfaceOverrideMaterial(InstanceRid, i, mat?.MaterialRid ?? new Rid());
        }
    }
    
    protected virtual void OnMeshChanged()
    {
        
    }
    protected virtual void OnMeshAssetChanged()
    {
        UpdateRenderingServerRid();
    }
    public override void Cleanup()
    {
        base.Cleanup();
        Mesh = null;
    }
}
