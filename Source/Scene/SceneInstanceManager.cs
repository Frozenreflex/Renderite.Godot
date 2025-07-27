using Godot;

namespace Renderite.Godot.Source.Scene;

public class SceneInstanceManager
{
    public TransformNode Base { get; private set; }
    public Rid InstanceRid { get; private set; }
    public bool Initialized { get; private set; }
    private bool _listening = false;

    public SceneInstanceManager(TransformNode b) => Initialize(b);
    public SceneInstanceManager()
    {
    }
    public void Initialize(TransformNode b, bool listenToTransformChanges = true)
    {
        if (Initialized) return;
        Base = b;
        if (listenToTransformChanges)
        {
            Base.GlobalTransformChanged += BaseOnGlobalTransformChanged;
            UpdateTransform();
            _listening = true;
        }
        InstanceRid = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetScenario(InstanceRid, Main.Scenario);
        Initialized = true;
    }
    private void UpdateTransform() => RenderingServer.InstanceSetTransform(InstanceRid, Base.GlobalTransform);
    private void BaseOnGlobalTransformChanged(TransformNode obj) => UpdateTransform();

    public void Cleanup()
    {
        if (_listening)
        {
            Base.GlobalTransformChanged -= BaseOnGlobalTransformChanged;
            _listening = false;
        }
        Base = null;
        RenderingServer.FreeRid(InstanceRid);
        InstanceRid = new Rid();
        Initialized = false;
    }
}
