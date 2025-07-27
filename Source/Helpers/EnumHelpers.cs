using System;
using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source.Helpers;

public static class EnumHelpers
{
    public static RenderingServer.ShadowCastingSetting ToGodot(this ShadowCastMode mode) =>
        mode switch
        {
            ShadowCastMode.Off => RenderingServer.ShadowCastingSetting.Off,
            ShadowCastMode.On => RenderingServer.ShadowCastingSetting.On,
            ShadowCastMode.ShadowOnly => RenderingServer.ShadowCastingSetting.ShadowsOnly,
            ShadowCastMode.DoubleSided => RenderingServer.ShadowCastingSetting.DoubleSided,
            _ => RenderingServer.ShadowCastingSetting.Off,
        };
    public static RenderingServer.LightType ToGodot(this LightType type) =>
        type switch
        {
            LightType.Point => RenderingServer.LightType.Omni,
            LightType.Directional => RenderingServer.LightType.Directional,
            LightType.Spot => RenderingServer.LightType.Spot,
            _ => RenderingServer.LightType.Omni,
        };

    public static RenderingServer.PrimitiveType ToGodot(this SubmeshTopology type) =>
        type switch
        {
            SubmeshTopology.Points => RenderingServer.PrimitiveType.Points,
            _ => RenderingServer.PrimitiveType.Triangles,
        };
}
