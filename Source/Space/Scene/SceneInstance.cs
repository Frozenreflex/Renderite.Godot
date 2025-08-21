using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.NativeInterop;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;

namespace Renderite.Godot.Source.Scene;

public class SceneInstance
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

    public void Initialize(TransformNode b)
    {
        if (Initialized) return;
        Base = b;
        InstanceRid = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetScenario(InstanceRid, Main.Scenario);
        Base.GlobalTransformChanged += BaseOnGlobalTransformChanged;
        Base.VisibilityChanged += OnVisibilityChanged;
        Base.LayerChanged += OnLayerChanged;
        OnInitialize();
        Initialized = true;
    }

    protected virtual void OnInitialize()
    {
        OnLayerChanged(Base);
    }

    protected virtual void OnVisibilityChanged()
    {
        if (InstanceValid) RenderingServer.InstanceSetVisible(InstanceRid, Base.IsVisibleInTree());
    }

    protected virtual void OnLayerChanged(TransformNode obj)
    {
        var isHidden = obj.Layer is LayerType.Hidden;
        if (InstanceValid)
            RenderingServer.InstanceSetLayerMask(InstanceRid, isHidden ? 2u : 1u);
    }

    private void UpdateTransform()
    {
        RenderingServer.InstanceSetTransform(InstanceRid, Base.GlobalTransform);
        OnLayerChanged(Base);
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
        InstanceValid = false;
    }
}

public class SceneInstanceList<T>(RenderSpace renderSpace) : List<T>
    where T : SceneInstance, new()
{
    public void HandleAdditionRemoval(RenderablesUpdate update)
    {
        if (!update.removals.IsEmpty)
        {
            var removals = SharedMemoryAccessor.Instance.AccessData(update.removals);
            foreach (var remove in removals)
            {
                if (remove < 0) break;
                if (remove >= Count) continue;
                this[remove].Cleanup();
                this[remove] = this.Last();
                RemoveAt(Count - 1);
            }
        }

        if (!update.additions.IsEmpty)
        {
            var additions = SharedMemoryAccessor.Instance.AccessData(update.additions);

            foreach (var addition in additions)
            {
                if (addition < 0) break;
                if (addition >= renderSpace.Nodes.Count) continue;
                var node = renderSpace.Nodes[addition];
                if (!GodotObject.IsInstanceValid(node)) throw new Exception();
                var instance = new T();
                instance.Initialize(node);
                Add(instance);
            }
        }
    }
}