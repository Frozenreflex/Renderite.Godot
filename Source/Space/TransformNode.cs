using System;
using System.Collections.Generic;
using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public partial class TransformNode : Node3D
{
    public event Action<TransformNode> GlobalTransformChanged = _ => { };
    public event Action<TransformNode> ParentChanged = _ => { };
    public event Action<TransformNode> LayerChanged = _ => { };
    public LayerType? Layer { get; private set; } = null;
    private bool _overlayRoot = false;

    public override void _Ready()
    {
        base._Ready();
        SetNotifyTransform(true);
    }

    public void InvokeParentChanged() => ParentChanged.Invoke(this);

    public override void _Notification(int what)
    {
        if (what != NotificationTransformChanged) return;
        if (_overlayRoot)
        {
            // TODO: temporary, gotta figure out how the unity code does this
            GlobalTransform =
                HeadOutputManager.Instance
                    .GetCameraTransform(); 
            RotateObjectLocal(Vector3.Up, (float)Math.PI);
            TranslateObjectLocal(new Vector3(0f, 0f, 0.5f));
        }

        GlobalTransformChanged.Invoke(this);
    }

    public void SetLayer(LayerType? type, bool root = false)
    {
        Layer = type;
        _overlayRoot = root && type is LayerType.Overlay;
        if (_overlayRoot)
        {
            GlobalTransform = HeadOutputManager.Instance.GetCameraTransform();
            RotateObjectLocal(Vector3.Up, (float)Math.PI);
            TranslateObjectLocal(new Vector3(0f, 0f, 0.5f));
        }

        foreach (var child in GetChildren())
        {
            if (child is TransformNode transformNode)
            {
                transformNode.SetLayer(type);
            }
        }

        LayerChanged.Invoke(this);
    }
}