using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source.Helpers;

public static class TransformHelpers
{
    private static readonly Transform3D FlipXInverse = Transform3D.FlipX.AffineInverse();
    
    /// <summary>
    /// Converts a RenderTransform to a Godot Transform3D, accounting for the flipped X axis
    /// </summary>
    public static Transform3D ToGodot(this RenderTransform transform)
    {
        // TODO: formula for converting a transform is taken from here https://github.com/V-Sekai/unidot_importer/blob/main/object_adapter.gd#L503
        // i'm assuming this works, but we need to test it
        var pos = transform.position.ToGodotLiteral();
        var rot = transform.rotation.ToGodotLiteral();
        var scale = transform.scale.ToGodotLiteral();
        return (FlipXInverse * (new Transform3D(new Basis(rot), pos).ScaledLocal(scale))) * Transform3D.FlipX;
    }
    /// <summary>
    /// Converts a RenderVector3 to a Godot Vector3, without accounting for the flipped X axis
    /// </summary>
    public static Vector3 ToGodotLiteral(this RenderVector3 vector) => new(vector.x, vector.y, vector.z);
    /// <summary>
    /// Converts a RenderVector3 to a Godot Vector3, accounting for the flipped X axis
    /// </summary>
    public static Vector3 ToGodot(this RenderVector3 vector) => new(-vector.x, vector.y, vector.z);
    /// <summary>
    /// Converts a RenderQuaternion to a Godot Quaternion, without accounting for the flipped X axis
    /// </summary>
    public static Quaternion ToGodotLiteral(this RenderQuaternion quaternion) => new(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
}
