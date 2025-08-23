shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZWriteMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform sampler3D _LUT : hint_default_black; // Need to define this to avoid unallocated memory
uniform sampler3D _SecondaryLUT : hint_default_black; // This too
uniform float _Lerp : hint_range(0, 1) = 0; // Specified in shader

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// #define LERP

void fragment() {
	vec3 c = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;

#ifdef LERP
	vec3 c0 = texture(_LUT, c.rgb).rgb;
	vec3 c1 = texture(_SecondaryLUT, c.rgb).rgb;

	c = mix(c0, c1, _Lerp);
#else
	c = texture(_LUT, c.rgb).rgb;
#endif

	ALBEDO = c;
}
