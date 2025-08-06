shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform vec3 HSVOffset = vec3(0.2, 0.0, 0.0);
uniform vec3 HSVMul = vec3(1);
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void fragment() {
	vec3 c = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;
	vec3 hsv = RGB_To_HSV_NoClip(c);

	hsv *= HSVMul.xyz;
	hsv += HSVOffset.xyz;
	hsv.x = fract(hsv.x);
	hsv.y = mix(hsv.y, 0, 1);

	c = HSV_To_RGB(hsv);

	ALBEDO = c;
}