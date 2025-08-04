using System.Collections.Generic;
using System.Linq;
using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public class MaterialInstance
{
    public Rid MaterialRid;
    
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
        ChangeBaseShader(variant, ShaderVariant.BlendModeMask);
    }
    public void SetValue(StringName name, Variant value) => RenderingServer.MaterialSetParam(MaterialRid, name, value);

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
        }
    }
    public bool Instantiated;
    
    public ShaderVariant Variant;
    public MaterialInstance() => MaterialRid = RenderingServer.MaterialCreate();

    //private Rid _currentShader;

    /*
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
        var newVariant = (Variant & (~mask)) & (value & mask);
        if (oldVariant == newVariant) return;
        GD.Print($"Changing shader variant. Old: {oldVariant:X} New: {newVariant:X}");
        Variant = newVariant;
        Shader.Return(Variant);
        RenderingServer.MaterialSetShader(MaterialRid, Shader.GetShader(Variant));
    }

    //TODO: apparently frooxengine just doesn't remove materials? uhhhhhhh?
    private void Cleanup() => RenderingServer.FreeRid(MaterialRid);
}
