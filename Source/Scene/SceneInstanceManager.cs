using Godot;

namespace Renderite.Godot.Source.Scene;

public class SceneInstanceManager
{
    public TransformNode Base { get; private set; }
    public Rid InstanceRid { get; private set; }
    public bool Initialized { get; private set; }
    public bool InstanceValid
    {
        get;
        set
        {
            if (field == value) return;
            if (value) UpdateTransform();
            field = value;
        }
    }

    public SceneInstanceManager(TransformNode b) => Initialize(b);
    public SceneInstanceManager()
    {
    }
    public void Initialize(TransformNode b)
    {
        if (Initialized) return;
        Base = b;
        InstanceRid = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetScenario(InstanceRid, Main.Scenario);
        Base.GlobalTransformChanged += BaseOnGlobalTransformChanged;
        Base.VisibilityChanged += OnVisibilityChanged;
        OnInitialize();
        Initialized = true;
    }
    protected virtual void OnInitialize()
    {
        
    }
    protected virtual void OnVisibilityChanged()
    {
        if (InstanceValid) RenderingServer.InstanceSetVisible(InstanceRid, Base.IsVisibleInTree());
    }
    protected void UpdateTransform()
    {
        if (InstanceValid) RenderingServer.InstanceSetTransform(InstanceRid, Base.GlobalTransform);
    }
    protected virtual void BaseOnGlobalTransformChanged(TransformNode obj) => UpdateTransform();

    public virtual void Cleanup()
    {
        Base.GlobalTransformChanged -= BaseOnGlobalTransformChanged;
        Base.VisibilityChanged -= OnVisibilityChanged;
        Base = null;
        RenderingServer.FreeRid(InstanceRid);
        InstanceRid = new Rid();
        Initialized = false;
    }
}
