shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZWriteMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

ColorTexture2DWhite(MainTex)
STData(MainTex)

uniform float _TextureChannel = 0;

void fragment() {
	
	vec4 col = SampleTexture(MainTex, UV);
	
	if (_TextureChannel == 0.0)
    {
        col.rgb = vec3(col.r, 0, 0); // red
    } 
	else if (_TextureChannel == 1.0)
    {
        col.rgb = vec3(0, col.g, 0); // green
    } 
	else if (_TextureChannel == 2.0)
    {
        col.rgb = vec3(0, 0, col.b); // blue
    } 
	else if (_TextureChannel == 3.0)
    {
        col.rgb = vec3(col.a, col.a, col.a); // alpha
    }
    
    ALBEDO = col.rgb;
}


