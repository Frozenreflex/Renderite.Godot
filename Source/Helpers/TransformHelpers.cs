using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source.Helpers;

public static class TransformHelpers
{
    private static readonly Transform3D FlipXInverse = Transform3D.FlipX.AffineInverse();

    //TODO this name sucks balls
    /// <summary>
    /// Constructs a Transform3D, given a translation (position), rotation, and scale.
    /// </summary>
    public static Transform3D TransformFromTRS(Vector3 p, Quaternion r, Vector3 s) => new Transform3D(new Basis(r), p).ScaledLocal(s);
    //TODO this name also sucks balls
    /// <summary>
    /// Constructs a Transform3D, given a translation (position) and rotation.
    /// </summary>
    public static Transform3D TransformFromTR(Vector3 p, Quaternion r) => new(new Basis(r), p);

    /// <summary>
    /// Converts a RenderTransform to a Godot Transform3D, accounting for the flipped X axis
    /// </summary>
    public static Transform3D ToGodot(this RenderTransform transform)
    {
        // TODO: formula for converting a transform is taken from here https://github.com/V-Sekai/unidot_importer/blob/main/object_adapter.gd#L503
        // i'm assuming this works, but we need to test it
        return (FlipXInverse * transform.ToGodotLiteral()) * Transform3D.FlipX;
    }
    /// <summary>
    /// Converts a RenderTransform to a Godot Transform3D, not accounting for the flipped X axis
    /// </summary>
    public static Transform3D ToGodotLiteral(this RenderTransform transform)
    {
        var pos = transform.position.ToGodotLiteral();
        var rot = transform.rotation.ToGodotLiteral();
        var scale = transform.scale.ToGodotLiteral();
        return TransformFromTRS(pos, rot, scale);
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
    //todo: what the fuck?
    //https://gamedev.stackexchange.com/a/201978
    /// <summary>
    /// Converts a RenderQuaternion to a Godot Quaternion, accounting for the flipped X axis
    /// </summary>
    public static Quaternion ToGodot(this RenderQuaternion quaternion) => new(quaternion.x, -quaternion.y, -quaternion.z, quaternion.w);
    public static Color ToGodotColor(this RenderVector4 vec) => new(vec.x, vec.y, vec.z, vec.w);
    public static Aabb ToGodot(this RenderBoundingBox boundingBox)
    {
        var center = boundingBox.center.ToGodot();
        var size = boundingBox.extents.ToGodotLiteral();
        var topCorner = center - (size * 0.5f);
        return new Aabb(topCorner, size);
    }
}
