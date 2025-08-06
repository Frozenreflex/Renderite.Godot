shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform float Lerp : hint_range(0.0, 1.0) = 1.0;
uniform sampler2D ScreenTexture : hint_screen_texture, filter_linear_mipmap;

void fragment() {   
	vec3 color = texture(ScreenTexture, SCREEN_UV).rgb;
	ALBEDO = mix(color, 1.0 - color, Lerp);
}
