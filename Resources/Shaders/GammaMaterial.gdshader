shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform float _Gamma = 2.2;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform float _OffsetFactor : hint_range(0.0, 1.0) = 1.0;
uniform float _OffsetUnits : hint_range(0.0, 1.0) = 1.0;
uniform vec4 _Rect = vec4(0, 0, 0, 0);
DefineRectClip

void vertex() {
	StartRectClip
}

void fragment() {
	DoRectClip(_Rect);
	vec3 c = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;
	ALBEDO = pow(c, vec3(_Gamma));
	ALPHA = 0.8f;
}
