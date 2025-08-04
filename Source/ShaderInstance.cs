using System;
using System.Collections.Generic;
using System.Text;
using Godot;

namespace Renderite.Godot.Source;

public enum ShaderVariant
{
    BlendModeMask = 0b00000000_00000000_00000000_00000111,
    
    BlendModeNone = 0,
    BlendModeOpaque,
    BlendModeCutout,
    BlendModeAlpha,
    BlendModeTransparent,
    BlendModeAdditive,
    BlendModeMultiply,
    
    CullModeMask  = 0b00000000_00000000_00000000_00011000,
    
    CullModeFront = 0b00000000_00000000_00000000_00001000,
    CullModeBack  = 0b00000000_00000000_00000000_00010000,
    CullModeOff   = 0b00000000_00000000_00000000_00011000,
    
    
    
    ZTestMask = 0b00000000_00000000_00000000_01100000,
    ZTestDefault = 0,
    ZTestInvert = 0b00000000_00000000_00000000_00100000,
    ZTestDisable = 0b00000000_00000000_00000000_01000000,
}
public class ShaderInstance
{
    private class InternalShader
    {
        public Rid Rid => Shader.GetRid();
        public readonly Shader Shader = new();
        public int Users;
    }
    private readonly Dictionary<ShaderVariant, InternalShader> _shaderMap = new();
    public string BaseShader;

    public void Return(ShaderVariant variant)
    {
        if (!_shaderMap.TryGetValue(variant, out var value)) return;
        value.Users--;
        if (value.Users <= 0) _shaderMap.Remove(variant);
    }
    public void Cleanup() => _shaderMap.Clear();
    public Rid GetShader(ShaderVariant variant)
    {
        if (!_shaderMap.TryGetValue(variant, out var value))
        {
            value = new InternalShader();
            if (variant == 0) value.Shader.Code = BaseShader;
            else
            {
                var code = new StringBuilder();
                
                //blend mode variant
                var blend = variant & ShaderVariant.BlendModeMask;
                if (blend > 0) code.Append($"#define BLEND_MODE {((int)blend) - 1}\n");

                //cull mode variant
                var cull = variant & ShaderVariant.CullModeMask;
                if (cull > 0)
                {
                    switch (cull)
                    {
                        case ShaderVariant.CullModeOff:
                            code.Append("#define CULL_MODE 0\n");
                            break;
                        case ShaderVariant.CullModeFront:
                            code.Append("#define CULL_MODE 1\n");
                            break;
                        case ShaderVariant.CullModeBack:
                            code.Append("#define CULL_MODE 2\n");
                            break;
                    }
                }

                //depth test variant
                var depth = variant & ShaderVariant.ZTestMask;
                if (depth > 0) code.Append(depth is ShaderVariant.ZTestInvert ? "#define ZTEST_MODE 1\n" : "#define ZTEST_MODE 2\n");

                code.Append(BaseShader);
                value.Shader.Code = code.ToString();
            }
            _shaderMap[variant] = value;
        }
        value.Users++;
        return value.Rid;
    }
}
