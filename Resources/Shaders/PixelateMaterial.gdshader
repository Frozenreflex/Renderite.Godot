shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

MapTexture2DZero(ResolutionMagnitudeTexure)

uniform vec2 Resolution = vec2(100, 100);
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform float _OffsetFactor : hint_range(0.0, 1.0) = 1.0;
uniform float _OffsetUnits : hint_range(0.0, 1.0) = 1.0;
uniform vec4 _Rect = vec4(0, 0, 1, 1);
DefineRectClip

void vertex() {
	StartRectClip
}

void fragment() {
	DoRectClip(_Rect);
#ifdef _RESOLUTION_MAGNITUDE_TEXTURE
	vec2 size = Resolution.xy * texture(SampleSTTextureData(ResolutionMagnitudeTexure), TRANSFORM_TEX(SCREEN_UV, texture(SampleSTTextureData(ResolutionMagnitudeTexure), SCREEN_UV))).rg;
#else
	vec2 size = Resolution.xy;
#endif
	vec2 grabbedUv = round(SCREEN_UV * size) / size;

	ALBEDO = texture(SCREEN_TEXTURE, grabbedUv).rgb;
}
