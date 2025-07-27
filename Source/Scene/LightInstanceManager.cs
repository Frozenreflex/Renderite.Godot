using Godot;

namespace Renderite.Godot.Source.Scene;

public class LightInstanceManager : AssetSceneInstanceManager
{
    public Rid LightRid;
    public RenderingServer.LightType Type = (RenderingServer.LightType)(-1);
}
