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
    public static bool Supported(this TextureFormat format) => format.ToGodot() != (Image.Format)(-1);
    public static Image.Format ToGodot(this TextureFormat format) => 
        format switch
        {
            TextureFormat.Alpha8 => Image.Format.L8,
            TextureFormat.R8 => Image.Format.R8,
            TextureFormat.RGB24 => Image.Format.Rgb8,
            TextureFormat.RGBA32 => Image.Format.Rgba8,
            TextureFormat.RGB565 => Image.Format.Rgb565,
            TextureFormat.RGBAHalf => Image.Format.Rgbah,
            TextureFormat.RHalf => Image.Format.Rh,
            TextureFormat.RGHalf => Image.Format.Rgh,
            TextureFormat.RGBAFloat => Image.Format.Rgbaf,
            TextureFormat.RFloat => Image.Format.Rf,
            TextureFormat.RGFloat => Image.Format.Rgf,
            TextureFormat.BC1 => Image.Format.Dxt1, //TODO: these names are fucking weird, is this correct?
            TextureFormat.BC2 => Image.Format.Dxt3,
            TextureFormat.BC3 => Image.Format.Dxt5,
            //TextureFormat.BC4 => Image.Format.RgtcR,
            //TextureFormat.BC5 => Image.Format.RgtcRg,
            TextureFormat.ETC2_RGB => Image.Format.Etc2Rgb8,
            TextureFormat.ETC2_RGBA1 => Image.Format.Etc2Rgb8A1,
            TextureFormat.ETC2_RGBA8 => Image.Format.Etc2Rgba8,
            TextureFormat.ASTC_4x4 => Image.Format.Astc4X4,
            TextureFormat.ASTC_8x8 => Image.Format.Astc8X8,
            _ => (Image.Format)(-1)
        };
    public static TextureFlags Convert(TextureFilterMode filter, TextureWrapMode wrapU, TextureWrapMode wrapV)
    {
        var result = (TextureFlags)0;
        result |= filter switch
        {
            TextureFilterMode.Point => TextureFlags.FilterNearest,
            TextureFilterMode.Anisotropic => TextureFlags.FilterAnisotropic,
            _ => TextureFlags.FilterLinear,
        };
        result |= wrapU switch
        {
            TextureWrapMode.Clamp => TextureFlags.UWrapClamp,
            TextureWrapMode.Mirror => TextureFlags.UWrapMirror,
            TextureWrapMode.MirrorOnce => TextureFlags.UWrapMirrorOnce,
            _ => TextureFlags.UWrapRepeat,
        };
        result |= wrapV switch
        {
            TextureWrapMode.Clamp => TextureFlags.VWrapClamp,
            TextureWrapMode.Mirror => TextureFlags.VWrapMirror,
            TextureWrapMode.MirrorOnce => TextureFlags.VWrapMirrorOnce,
            _ => TextureFlags.VWrapRepeat,
        };
        return result;
    }
}
