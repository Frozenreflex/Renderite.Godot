using Godot;

namespace Renderite.Godot.Source.Scene;

public class MeshInstanceManager : AssetSceneInstanceManager
{
	public RenderingServer.ShadowCastingSetting ShadowCastingMode = (RenderingServer.ShadowCastingSetting)(-1);
}
