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
    private class Shader
    {
        public Rid Rid;
        public int Users;
    }
    private readonly Dictionary<ShaderVariant, Shader> _shaderMap = new();
    public string BaseShader;

    public void Return(ShaderVariant variant)
    {
        if (_shaderMap.TryGetValue(variant, out var value))
        {
            value.Users--;
            if (value.Users == 0)
            {
                RenderingServer.FreeRid(value.Rid);
                _shaderMap.Remove(variant);
            }
        }
    }
    public Rid GetShader(ShaderVariant variant)
    {
        if (!_shaderMap.TryGetValue(variant, out var value))
        {
            value = new Shader
            {
                Rid = RenderingServer.ShaderCreate(),
            };
            if (variant == 0) RenderingServer.ShaderSetCode(value.Rid, BaseShader);
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
                RenderingServer.ShaderSetCode(value.Rid, code.ToString());
            }
        }
        value.Users++;
        return value.Rid;
        
    }
}
