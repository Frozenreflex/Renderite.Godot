using Godot;

namespace Renderite.Godot.Source.Scene;

public class LightInstance : AssetSceneInstance
{
    public Rid LightRid;
    public RenderingServer.LightType Type = (RenderingServer.LightType)(-1);
}
