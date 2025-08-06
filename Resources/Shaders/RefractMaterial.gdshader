shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"

uniform float RefractionStrength;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D NormalMap : hint_normal;
uniform float ColorMask;

void fragment() {
	vec2 grabUv = SCREEN_UV.xy;
	NORMAL = (INV_VIEW_MATRIX * vec4(NORMAL, 1)).xyz;
#ifdef NORMALMAP
	mat3 tangentTransform = mat3(TANGENT, BINORMAL, NORMAL);
	vec3 bumpNormal = texture(NormalMap, UV).xyz;
	// Compute pertrubed normal, replacing the old one
	NORMAL = normalize(bumpNormal * tangentTransform);
#endif
	NORMAL = (VIEW_MATRIX * vec4(NORMAL, 1)).xyz;
	grabUv -= NORMAL.xy * RefractionStrength;
	vec3 color = texture(SCREEN_TEXTURE, grabUv).xyz;
	int mask = int(ColorMask);
	color.r *= float(mask & 8) / 8.0;
	color.g *= float(mask & 4) / 4.0;
	color.b *= float(mask & 2) / 2.0;
	ALBEDO = color;
}
