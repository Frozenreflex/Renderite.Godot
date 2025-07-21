using System;
using System.Collections.Generic;
using Godot;

namespace Renderite.Godot.Source;
public partial class TransformNode : Node3D
{
    public event Action<TransformNode> GlobalTransformChanged = _ => { };
    public event Action<TransformNode> ParentChanged = _ => { };

    public override void _Ready()
    {
        base._Ready();
        SetNotifyTransform(true);
    }
    public void InvokeParentChanged() => ParentChanged.Invoke(this);
    public override void _Notification(int what)
    {
        if (what == NotificationTransformChanged) GlobalTransformChanged.Invoke(this);
    }
}