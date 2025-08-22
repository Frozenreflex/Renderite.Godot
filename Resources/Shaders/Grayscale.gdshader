shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform float _RatioR = 0.3;
uniform float _RatioG = 0.59;
uniform float _RatioB = 0.11;
uniform float _Lerp : hint_range(0, 1) = 1.0;

ColorTexture2DBlack(Gradient2D)
STData(Gradient2D)

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

//#define GRADIENT

void fragment() {
	mediump vec4 c = texture(SCREEN_TEXTURE, SCREEN_UV);
	float grayscale = c.r * _RatioR + c.g * _RatioG + c.b * _RatioB;

#ifdef GRADIENT
	mediump vec3 newColor = SampleSTTexture(Gradient2D, vec2(grayscale, 0.0)).xyz;
#else
	mediump vec3 newColor = vec3(grayscale);
#endif

	ALBEDO = mix(c.rgb, newColor, _Lerp);
}