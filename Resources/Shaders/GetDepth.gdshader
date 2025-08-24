shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZWriteMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform float _Multiply = 1;
uniform float _Offset = 0;
uniform float _ClipMin = 0;
uniform float _ClipMax = 1;
uniform sampler2D DEPTH_TEXTURE : hint_depth_texture;
uniform vec4 _Rect = vec4(0, 0, 0, 0);
DefineRectClip

//#define CLIP

float GetLinearDepth(vec2 uv, mat4 projMatrix) {
	float depth = texture(DEPTH_TEXTURE, uv).x;
	vec3 ndc = vec3(uv * 2.0 - 1.0, depth);
 	vec4 view = projMatrix * vec4(ndc, 1.0);
  	view.xyz /= view.w;
  	return -view.z;
}

void vertex() {
	StartRectClip
}

void fragment() {
	DoRectClip(_ClipRect);
  	float linear_depth = GetLinearDepth(SCREEN_UV, INV_PROJECTION_MATRIX);
	
#ifdef CLIP
	linear_depth -= _ClipMin;
	linear_depth /= _ClipMax - _ClipMin;
#endif

	linear_depth *= _Multiply;
	linear_depth += _Offset;
	linear_depth = clamp(linear_depth, 0.0, 1.0);

	ALBEDO = vec3(linear_depth);
}