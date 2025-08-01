using System;
using System.Collections.Generic;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;

namespace Renderite.Godot.Source;

[Flags]
public enum TextureFlags
{
    UWrap = 0b0000_0000_0000_0011, //3
    VWrap = 0b0000_0000_0000_1100, //12
    Filter = 0b0000_0000_0011_0000, //48
    
    UWrapRepeat = 0,
    UWrapClamp = 1 << 0,
    UWrapMirror = 2 << 0,
    UWrapMirrorOnce = 3 << 0,
    
    VWrapRepeat = 0,
    VWrapClamp = 1 << 2,
    VWrapMirror = 2 << 2,
    VWrapMirrorOnce = 3 << 2,
    
    FilterNearest = 0,
    FilterLinear = 1 << 4,
    FilterAnisotropic = 2 << 4,
}
public class TextureEntry
{
    public Rid Rid;
    //public Image Image;
    public TextureFlags Flags;

    public int Width;
    public int Height;
    public int MipmapCount;
    public TextureFormat Format;

    public bool Instantiated;

}
public class TextureManager
{
    public Dictionary<int, TextureEntry> Texture2Ds = new();

    private TextureEntry GetOrCreate(int index)
    {
        if (!Texture2Ds.TryGetValue(index, out var entry))
        {
            entry = new TextureEntry
            {
                Rid = RenderingServer.Texture2DCreate(Image.CreateEmpty(4,4,false,Image.Format.Rgb8)),
            };
            Texture2Ds[index] = entry;
        }
        return entry;
    }
    public void Handle(SetTexture2DFormat command)
    {
        var entry = GetOrCreate(command.assetId);
        entry.Width = command.width;
        entry.Height = command.height;
        entry.MipmapCount = command.mipmapCount;
        entry.Format = command.format;
        //TODO: this should clear the image
        
        var result = new SetTexture2DResult
        {
            assetId = command.assetId,
            type = TextureUpdateResultType.FormatSet,
            instanceChanged = entry.Instantiated,
        };
        RendererManager.Instance.BackgroundMessagingManager.SendCommand(result);
        entry.Instantiated = true;
    }
    public void Handle(SetTexture2DProperties command)
    {
        var entry = GetOrCreate(command.assetId);
        entry.Flags = EnumHelpers.Convert(command.filterMode, command.wrapU, command.wrapV);
        
        var result = new SetTexture2DResult
        {
            assetId = command.assetId,
            type = TextureUpdateResultType.PropertiesSet,
            instanceChanged = false,
        };
        RendererManager.Instance.BackgroundMessagingManager.SendCommand(result);
    }
    public void Handle(SetTexture2DData command)
    {
        var entry = GetOrCreate(command.assetId);
        //TODO: is this data somehow mangled for unity, and we need to somehow unmangle it
        var size = ImageHelpers.GetDstImageSize(entry.Width, entry.Height, entry.Format.ToGodotAll(), out _, 0);
        var slice = SharedMemoryAccessor.Instance.AccessSlice(command.data);
        Span<byte> data;
        if (slice.SizeBytes >= size) data = slice.Data.Slice(0, size);
        else
        {
            //TODO
            GD.Print("image was too small, given null array");
            data = new byte[size];
        }
        var image = ImageHelpers.Create(entry.Width, entry.Height, false, entry.Format, data);
        //var image = Image.CreateFromData(entry.Width, entry.Height, /*entry.MipmapCount > 0*/ false, entry.Format.ToGodot(), SharedMemoryAccessor.Instance.AccessSlice(command.data).Data[..size]);
        
        if (true) image.SavePng($"user://testImage{command.assetId}.{entry.Width}x{entry.Height}.{entry.Format}.png");
        
        var tempRid = RenderingServer.Texture2DCreate(image);
        RenderingServer.TextureReplace(entry.Rid, tempRid);
        RenderingServer.FreeRid(tempRid);
        
        var result = new SetTexture2DResult
        {
            assetId = command.assetId,
            type = TextureUpdateResultType.DataUpload,
            instanceChanged = false,
        };
        RendererManager.Instance.BackgroundMessagingManager.SendCommand(result);
    }
    public void Handle(UnloadTexture2D command)
    {
        if (Texture2Ds.TryGetValue(command.assetId, out var value))
        {
            RenderingServer.FreeRid(value.Rid);
            Texture2Ds.Remove(command.assetId);
        }
    }
}
