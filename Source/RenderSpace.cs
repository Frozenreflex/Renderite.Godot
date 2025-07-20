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
    private bool _lastPrivate;
    private bool _lastActive;

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

        if (data.transformsUpdate is not null)
        {
            var removals = SharedMemoryManager.Instance.Read(data.transformsUpdate.removals);
            foreach (var remove in removals)
            {
                if (remove < 0) continue;
                
            }
        }
    }
}
