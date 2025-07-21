using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public partial class RenderSpace : Node3D
{
    public int Id;
    public bool IsActive;
    public bool IsOverlay;
    public Vector3 RootPosition;
    public Quaternion RootRotation;
    public Vector3 RootScale;
    public readonly List<TransformNode> Nodes = new();
    private bool _lastPrivate;
    private bool _lastActive;
    private Transform3D RootTransform => TransformHelpers.TransformFromTRS(RootPosition, RootRotation, RootScale);

    public void UpdateOverlayPositioning(Node3D referenceNode)
    {
        if (IsOverlay)
        {
            Transform = referenceNode.GlobalTransform * TransformHelpers.TransformFromTR(-RootPosition, RootRotation);
            //TODO: is this correct?
            /*
            transform.position = referenceTransform.position - this.RootPosition;
            transform.rotation = referenceTransform.rotation * this.RootRotation;
            transform.localScale = referenceTransform.localScale;
            */
        }
    }
    
    public void HandleUpdate(RenderSpaceUpdate data)
    {
        IsActive = data.isActive;
        IsOverlay = data.isOverlay;

        if (IsActive != _lastActive)
        {
            _lastActive = IsActive;
            
        }
        if (IsActive)
        {
            
        }

        RootPosition = data.rootTransform.position.ToGodot();
        RootRotation = data.rootTransform.rotation.ToGodot();
        RootScale = data.rootTransform.scale.ToGodotLiteral(); //we don't want to convert (1,1,1) to (-1,1,1)

        if (data.transformsUpdate is not null) HandleTransformUpdate(data.transformsUpdate);
    }
    public void HandleTransformUpdate(TransformsUpdate update)
    {
        var removals = SharedMemoryManager.Instance.Read(update.removals);
        foreach (var remove in removals)
        {
            if (remove < 0) break;
            if (remove >= Nodes.Count) continue;
            var toRemove = Nodes[remove];
            foreach (var c in toRemove.GetChildren()) toRemove.RemoveChild(c);
            toRemove.QueueFree();
            Nodes[remove] = Nodes.Last();
            Nodes.RemoveAt(Nodes.Count - 1);
        }
        while (Nodes.Count < update.targetTransformCount)
        {
            var node = new TransformNode();
            AddChild(node);
            Nodes.Add(node);
        }
        if (!update.parentUpdates.IsEmpty)
        {
            var parentUpdates = SharedMemoryManager.Instance.Read(update.parentUpdates);
            // i just want to mention i compressed around 25-30 loc down to this
            foreach (var parentUpdate in parentUpdates)
            {
                if (parentUpdate.transformId < 0) break;
                var node = Nodes[parentUpdate.transformId];
                node.Reparent(Nodes[parentUpdate.newParentId], false);
                node.InvokeParentChanged();
            }
            //
        }
        if (!update.poseUpdates.IsEmpty)
        {
            var poseUpdates = SharedMemoryManager.Instance.Read(update.poseUpdates);
            foreach (var poseUpdate in poseUpdates)
            {
                if (poseUpdate.transformId < 0) break;
                var node = Nodes[poseUpdate.transformId];
                node.Transform = poseUpdate.pose.ToGodot();
            }
        }
    }
    public void HandleMeshRenderablesUpdate(MeshRenderablesUpdate update)
    {
        if (!update.removals.IsEmpty)
        {
            var removals = SharedMemoryManager.Instance.Read(update.removals);
            foreach (var remove in removals)
            {
                if (remove < 0) break;
                //if (remove >= Nodes.Count) continue;
                //var toRemove = Nodes[remove];
                //foreach (var c in toRemove.GetChildren()) toRemove.RemoveChild(c);
                //toRemove.QueueFree();
                //Nodes[remove] = Nodes.Last();
                //Nodes.RemoveAt(Nodes.Count - 1);
            }
        }
        if (!update.additions.IsEmpty)
        {
            var additions = SharedMemoryManager.Instance.Read(update.additions);

            foreach (var addition in additions)
            {
                if (addition < 0) break;
                //if (addition >= Nodes.Count) continue;
                var node = Nodes[addition];
                if (!IsInstanceValid(node)) throw new Exception();
                //var meshInstanceRid = RenderingServer.InstanceCreate
            }
        }
    }
}
