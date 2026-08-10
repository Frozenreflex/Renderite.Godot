shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZWriteMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform float _RefractionStrength = 0.01;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
MapTexture2DZero(NormalMap)
// uniform float ColorMask;

// #define NORMALMAP

void fragment() {
	vec2 grabUv = SCREEN_UV.xy;
	NORMAL = (INV_VIEW_MATRIX * vec4(NORMAL, 1)).xyz;
#ifdef NORMALMAP
	mat3 tangentTransform = mat3(TANGENT, BINORMAL, NORMAL);
	vec3 bumpNormal = SampleTexture(NormalMap, UV).xyz;
	// Compute pertrubed normal, replacing the old one
	NORMAL = normalize(bumpNormal * tangentTransform);
#endif
	NORMAL = (VIEW_MATRIX * vec4(NORMAL, 1)).xyz;
	grabUv -= NORMAL.xy * _RefractionStrength;
	vec3 color = texture(SCREEN_TEXTURE, grabUv).xyz;
	
	// Hold off until we get better color mask support
	//int mask = int(ColorMask);
	//color.r *= float(mask & 8) / 8.0;
	//color.g *= float(mask & 4) / 4.0;
	//color.b *= float(mask & 2) / 2.0;
	
	ALBEDO = color;
}