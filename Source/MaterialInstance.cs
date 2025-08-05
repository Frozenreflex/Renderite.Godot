using System.Collections.Generic;
using System.Linq;
using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public class MaterialInstance
{
    public Rid MaterialRid = RenderingServer.MaterialCreate();
    
    public MaterialRenderType Type
    {
        get;
        set
        {
            field = value;
            UpdateBlendMode();
        }
    } = MaterialRenderType.Opaque;
    public float SourceBlendProp
    {
        get;
        set
        {
            field = value;
            UpdateBlendMode();
        }
    }
    public float DestinationBlendProp
    {
        get;
        set
        {
            field = value;
            UpdateBlendMode();
        }
    }
    public bool UseBlendMode;
    private void UpdateBlendMode()
    {
        //format: MaterialRenderType, SrcBlendProp, DstBlendProp
        //Opaque = Opaque, 1, 0
        //Cutout = TransparentCutout, 1, 0
        //Alpha = Transparent, 5, 10
        //Transparent = Transparent, 1, 10
        //Additive = Transparent, 1, 1
        //Multiply = Transparent, 2, 0
        if (!UseBlendMode) return;
        //for some reason, resonite still uses unity's frankly shit blending system, so we have to convert it here
        ShaderVariant variant;
        switch (Type)
        {
            case MaterialRenderType.Opaque:
                variant = ShaderVariant.BlendModeOpaque;
                break;
            case MaterialRenderType.TransparentCutout:
                variant = ShaderVariant.BlendModeCutout;
                break;
            case MaterialRenderType.Transparent:
            default:
            {
                if (Mathf.IsEqualApprox(SourceBlendProp, 1))
                    variant = Mathf.IsEqualApprox(DestinationBlendProp, 10) ? ShaderVariant.BlendModeTransparent : ShaderVariant.BlendModeAdditive;
                else if (Mathf.IsEqualApprox(SourceBlendProp, 5))
                    variant = ShaderVariant.BlendModeAlpha;
                else
                    variant = ShaderVariant.BlendModeMultiply;
                break;
            }
        }
        if ((Variant & ShaderVariant.BlendModeMask) == variant) return;
        //GD.Print($"Changing blend mode: {variant}");
        ChangeBaseShader(variant, ShaderVariant.BlendModeMask);
    }
    public void SetValue(StringName name, Variant value) => RenderingServer.MaterialSetParam(MaterialRid, name, value);
    public void SetTexture(int id, TextureEntry entry)
    {
        var name = MaterialManager.PropertyIdMap[id];
        var str = name.ToString();
        var rid = entry?.Rid ?? new Rid();
        RenderingServer.MaterialSetParam(MaterialRid, $"{str}_Nearest", rid);
        RenderingServer.MaterialSetParam(MaterialRid, $"{str}_Linear", rid);
        RenderingServer.MaterialSetParam(MaterialRid, $"{str}_Anisotropic", rid);
        RenderingServer.MaterialSetParam(MaterialRid, $"{str}_Flags", entry is not null ? ((int)entry.Flags) : 0);

        if (_textureCache.TryGetValue(id, out var existing) && existing is not null) existing.FlagsChanged -= EntryOnFlagsChanged;
        _textureCache[id] = entry;
        if (entry is not null) entry.FlagsChanged += EntryOnFlagsChanged;
    }
    private void EntryOnFlagsChanged(TextureEntry obj)
    {
        var ids = _textureCache.Where(i => i.Value == obj).Select(i => i.Key);
        foreach (var id in ids)
        {
            var name = MaterialManager.PropertyIdMap[id];
            var str = name.ToString();
            RenderingServer.MaterialSetParam(MaterialRid, $"{str}_Flags", (int)obj.Flags);
        }
    }

    public ShaderInstance Shader
    {
        get;
        set
        {
            if (field == value) return;
            field?.Return(Variant);
            field = value;
            UseBlendMode = false;
            RenderingServer.MaterialSetShader(MaterialRid, field?.GetShader(Variant) ?? new Rid());
            //_paramCache.Clear();
            foreach (var entry in _textureCache.Where(i => i.Value is not null)) entry.Value.FlagsChanged -= EntryOnFlagsChanged;
            _textureCache.Clear();
        }
    }
    public bool Instantiated;
    
    public ShaderVariant Variant;

    //public Dictionary<int, Variant> _paramCache = new();
    public Dictionary<int, TextureEntry> _textureCache = new();

    /*
    private Rid _currentShader;
    private void ChangeShader(Rid shaderRid)
    {
        if (MaterialRid == new Rid()) return;
        if (shaderRid == new Rid() || _currentShader == new Rid())
        {
            RenderingServer.MaterialSetShader(MaterialRid, shaderRid);
            _currentShader = shaderRid;
            return;
        }
        var list = RenderingServer.GetShaderParameterList(_currentShader).Select(i => i["name"].AsStringName()).Select(i => (i, RenderingServer.MaterialGetParam(MaterialRid, i))).ToArray();
        RenderingServer.MaterialSetShader(MaterialRid, shaderRid);
        foreach (var param in list) RenderingServer.MaterialSetParam(MaterialRid, param.Item1, param.Item2);
        _currentShader = shaderRid;
    }
    */

    public void ChangeBaseShader(ShaderVariant value, ShaderVariant mask)
    {
        var oldVariant = Variant;
        var newVariant = (Variant & (~mask)) | (value & mask);
        if (oldVariant == newVariant) return;
        //GD.Print($"Changing shader variant. Old: {oldVariant:X} New: {newVariant:X}");
        Variant = newVariant;
        RenderingServer.MaterialSetShader(MaterialRid, Shader.GetShader(Variant));
        Shader.Return(oldVariant);
    }

    //TODO: apparently frooxengine just doesn't remove materials? uhhhhhhh?
    private void Cleanup()
    {
        RenderingServer.FreeRid(MaterialRid);
        //_paramCache.Clear();
        foreach (var entry in _textureCache) entry.Value.FlagsChanged -= EntryOnFlagsChanged;
        _textureCache.Clear();
    }
}
