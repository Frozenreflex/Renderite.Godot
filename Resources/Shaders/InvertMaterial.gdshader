shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"

uniform float _Lerp : hint_range(0.0, 1.0) = 1.0;
uniform float _OffsetFactor : hint_range(0.0, 1.0) = 1.0;
uniform float _OffsetUnits : hint_range(0.0, 1.0) = 1.0;
uniform vec4 _Rect = vec4(0, 0, 0, 0);
DefineRectClip

uniform sampler2D ScreenTexture : hint_screen_texture, filter_linear_mipmap;

void vertex() {
	StartRectClip
}

void fragment() {
	vec3 color = texture(ScreenTexture, SCREEN_UV).rgb;
	ALBEDO = mix(color, 1.0 - color, _Lerp);
	DoRectClip(rect);
}
