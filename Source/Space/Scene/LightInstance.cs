using Godot;

namespace Renderite.Godot.Source.Scene;

public class LightInstance : AssetSceneInstance
{
    public Rid LightRid;
    public RenderingServer.LightType Type = (RenderingServer.LightType)(-1);
    
    protected override void BaseOnGlobalTransformChanged(TransformNode obj)
    {
        var transform = obj.GlobalTransform;
        transform.Basis *= new Basis(new Quaternion(Vector3.Right, Mathf.Pi));
        RenderingServer.InstanceSetTransform(InstanceRid, transform);
    }
}
