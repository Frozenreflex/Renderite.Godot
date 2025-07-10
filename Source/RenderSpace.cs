using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public class RenderSpace
{
    public int Id;
    public bool IsActive;
    public bool IsOverlay;
    public Transform3D RootTransform = Transform3D.Identity;
    private bool _lastPrivate;
    private bool _lastActive;
    public void HandleUpdate(RenderSpaceUpdate data)
    {
        IsActive = data.isActive;
        IsOverlay = data.isOverlay;

        if (IsActive != _lastActive)
        {
            _lastActive = IsActive;
            
        }

        if (IsOverlay)
        {
            var newRoot = data.rootTransform.ToGodot();
            if (!RootTransform.IsEqualApprox(newRoot))
            {
                //TODO: update every transform here

                RootTransform = newRoot;
            }
        }
    }
}
