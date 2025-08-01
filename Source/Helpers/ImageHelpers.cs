using System;
using System.Linq;
using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source.Helpers;

public static class ImageHelpers
{
    public static readonly TextureFormat[] SwizzleFormats =
    [
        TextureFormat.ARGB32, //RGBA32
        TextureFormat.BGRA32, //RGBA32
        TextureFormat.BGR565, //RGB565
        TextureFormat.ARGBHalf, //RGBAHalf
        TextureFormat.ARGBFloat, //RGBAFloat
    ];
    public static Image Create(int width, int height, bool useMipmaps, TextureFormat format, ReadOnlySpan<byte> data)
    {
        var toGodot = format.ToGodot();
        if (toGodot != (Image.Format)(-1)) return Image.CreateFromData(width, height, false, toGodot, data);
        switch (format)
        {
            case TextureFormat.ARGB32:
            {
                var newData = new byte[data.Length];
                for (var i = 0; i < data.Length; i += 4)
                {
                    newData[i + 0] = data[i + 3];
                    newData[i + 1] = data[i + 0];
                    newData[i + 2] = data[i + 1];
                    newData[i + 3] = data[i + 2];
                }
                return Image.CreateFromData(width, height, false, toGodot, newData);
            }
            case TextureFormat.BGRA32:
            {
                var newData = new byte[data.Length];
                for (var i = 0; i < data.Length; i += 4)
                {
                    newData[i + 0] = data[i + 2];
                    newData[i + 1] = data[i + 1];
                    newData[i + 2] = data[i + 0];
                    newData[i + 3] = data[i + 3];
                }
                return Image.CreateFromData(width, height, false, toGodot, newData);
            }
            case TextureFormat.ARGBHalf:
            {
                var newData = new byte[data.Length];
                for (var i = 0; i < data.Length; i += 8)
                {
                    newData[i + 0] = data[i + 6];
                    newData[i + 1] = data[i + 7];
                    newData[i + 2] = data[i + 0];
                    newData[i + 3] = data[i + 1];
                    newData[i + 4] = data[i + 2];
                    newData[i + 5] = data[i + 3];
                    newData[i + 6] = data[i + 4];
                    newData[i + 7] = data[i + 5];
                }
                return Image.CreateFromData(width, height, false, toGodot, newData);
            }
            case TextureFormat.BGR565: //TODO: swizzling this one is a bit harder, since we need to swizzle individual bits
            default: return Image.CreateEmpty(width, height, false, Image.Format.Dxt5);
        }
    }
    
    //all of these are copied from Godot's Image implementation and converted from C++ to C#
    public static int GetDstImageSize(int p_width, int p_height, Image.Format p_format, out int r_mipmaps, int p_mipmaps) 
    {
        // Data offset in mipmaps (including the original texture).
        var size = 0;

        var w = p_width;
        var h = p_height;

        // Current mipmap index in the loop below. p_mipmaps is the target mipmap index.
        // In this function, mipmap 0 represents the first mipmap instead of the original texture.
        var mm = 0;

        var pixsize = GetFormatPixelSize(p_format);
        var pixshift = GetFormatPixelRShift(p_format);
        var block = GetFormatBlockSize(p_format);
        
        const int minw = 1, minh = 1;

        while (true) 
        {
            var bw = w % block != 0 ? w + (block - w % block) : w;
            var bh = h % block != 0 ? h + (block - h % block) : h;

            var s = bw * bh;

            s *= pixsize;
            s >>= pixshift;

            size += s;

            if (p_mipmaps < 0 && w == minw && h == minh) break;
            
            w = Mathf.Max(minw, w >> 1);
            h = Mathf.Max(minh, h >> 1);

            if (p_mipmaps >= 0 && mm == p_mipmaps) break;
            
            mm++;
        }

        r_mipmaps = mm;
        return size;
    }
    public static int GetFormatPixelSize(Image.Format p_format) =>
        p_format switch
        {
            Image.Format.L8 or Image.Format.R8 or Image.Format.Dxt1 or Image.Format.Dxt3 or Image.Format.Dxt5 or Image.Format.RgtcR or Image.Format.RgtcRg or Image.Format.BptcRgba 
                or Image.Format.BptcRgbf or Image.Format.BptcRgbfu or Image.Format.Etc or Image.Format.Etc2R11 or Image.Format.Etc2R11S or Image.Format.Etc2Rg11 or Image.Format.Etc2Rg11S 
                or Image.Format.Etc2Rgb8 or Image.Format.Etc2Rgba8 or Image.Format.Etc2Rgb8A1 or Image.Format.Etc2RaAsRg or Image.Format.Dxt5RaAsRg or Image.Format.Astc4X4 or Image.Format.Astc4X4Hdr
                or Image.Format.Astc8X8 or Image.Format.Astc8X8Hdr => 1,
            Image.Format.La8 or Image.Format.Rg8 or Image.Format.Rgba4444 or Image.Format.Rgb565 or Image.Format.Rh => 2,
            Image.Format.Rgb8 => 3,
            Image.Format.Rgba8 or Image.Format.Rf or Image.Format.Rgh or Image.Format.Rgbe9995 => 4,
            Image.Format.Rgbh => 6,
            Image.Format.Rgbah or Image.Format.Rgf => 8,
            Image.Format.Rgbf => 12,
            Image.Format.Rgbaf => 16,
            _ => 0,
        };
    public static int GetFormatPixelRShift(Image.Format p_format) =>
        p_format switch
        {
            Image.Format.Astc8X8 => 2,
            Image.Format.Dxt1 or Image.Format.RgtcR or Image.Format.Etc or Image.Format.Etc2R11 or Image.Format.Etc2R11S or Image.Format.Etc2Rgba8 or Image.Format.Etc2Rgb8A1 => 1,
            _ => 0,
        };
    public static int GetFormatBlockSize(Image.Format p_format)
    {
        return p_format switch
        {
            Image.Format.Dxt1 or Image.Format.Dxt3 or Image.Format.Dxt5 or Image.Format.RgtcR or Image.Format.RgtcRg or Image.Format.Etc or Image.Format.BptcRgba or Image.Format.BptcRgbf
                or Image.Format.BptcRgbfu or Image.Format.Etc2R11 or Image.Format.Etc2R11S or Image.Format.Etc2Rg11 or Image.Format.Etc2Rg11S or Image.Format.Etc2Rgb8 or Image.Format.Etc2Rgba8
                or Image.Format.Etc2Rgb8A1 or Image.Format.Etc2RaAsRg or Image.Format.Dxt5RaAsRg or Image.Format.Astc4X4 or Image.Format.Astc4X4Hdr => 4,
            Image.Format.Astc8X8 or Image.Format.Astc8X8Hdr => 8,
            _ => 1,
        };
    }
}
