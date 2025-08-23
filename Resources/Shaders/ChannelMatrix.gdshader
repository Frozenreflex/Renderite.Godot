shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZWriteMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform vec4 _LevelsR = vec4(0, 1, 0, 0);
uniform vec4 _LevelsG = vec4(0, 0, 1, 0);
uniform vec4 _LevelsB = vec4(1, 0, 0, 0);
uniform vec4 _ClampMin = vec4(0, 0, 0, 0);
uniform vec4 _ClampMax = vec4(2, 2, 2, 0);

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void fragment() {
	mediump vec4 c = texture(SCREEN_TEXTURE, SCREEN_UV);

	c.rgb = (mat3(_LevelsR.xyz, _LevelsG.xyz, _LevelsB.xyz) * c.rgb) + vec3(_LevelsR.w, _LevelsG.w, _LevelsB.w);
	
	c.rgb = max(_ClampMin.xyz, c.rgb);
	c.rgb = min(_ClampMax.xyz, c.rgb);

	ALBEDO = c.rgb;
}