using Renderite.Shared;

namespace Renderite.Godot.Source.Scene;

public class LayerInstance : SceneInstance
{
    public void SetLayer(LayerType layer)
    {
        Base.SetLayer(layer, true);
    }

    public override void Cleanup()
    {
        Base.SetLayer(null, true);
        base.Cleanup();
    }
}