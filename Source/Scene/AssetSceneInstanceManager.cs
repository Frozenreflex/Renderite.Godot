using Godot;

namespace Renderite.Godot.Source.Scene;

public class AssetSceneInstanceManager : SceneInstanceManager
{
    public int AssetIndex = -1;
    public RenderingServer.ShadowCastingSetting ShadowCastingMode = (RenderingServer.ShadowCastingSetting)(-1);
}
