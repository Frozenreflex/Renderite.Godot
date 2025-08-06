shader_type spatial;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform float Gamma = 2.2;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void fragment() {
	vec3 c = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;
	ALBEDO = pow(c, vec3(Gamma));
	ALPHA = 0.8f;
}
