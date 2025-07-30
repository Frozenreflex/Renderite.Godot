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
    
    PropertyBlock = 0b00000000_00000000_00000000_00001000,
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
                
                //property block variant
                if ((variant & ShaderVariant.PropertyBlock) > 0) code.Append("#define PROPERTY_BLOCKS");

                code.Append(BaseShader);
                RenderingServer.ShaderSetCode(value.Rid, code.ToString());
            }
        }
        value.Users++;
        return value.Rid;
        
    }
}
