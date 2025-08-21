shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform vec3 _HSVOffset = vec3(0.2, 0.0, 0.0);
uniform vec3 _HSVMul = vec3(1);
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
	vec3 hsv = RGB_To_HSV_NoClip(c);

	hsv *= _HSVMul.xyz;
	hsv += _HSVOffset.xyz;
	hsv.x = fract(hsv.x);
	hsv.y = saturate(hsv.y);

	c = HSV_To_RGB(hsv);

	ALBEDO = c;
}