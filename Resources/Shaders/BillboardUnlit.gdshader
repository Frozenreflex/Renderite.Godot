shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZWriteMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

ColorTexture2DWhite(Tex)
STData(Tex)

uniform vec4 _Color : source_color = vec4(1);
uniform float _Cutoff : hint_range(0.0, 1.0) = 0.5;
uniform vec4 _PointSize = vec4(0.1, 0.1, 0.0, 0.0);

uniform sampler2D _OffsetTex : hint_default_black;
uniform vec4 _OffsetTex_ST = vec4(1, 1, 0, 0);
uniform vec4 _OffsetMagnitude = vec4(0.1, 0.1, 0.0, 0.0);

uniform float _PolarPower = 1.0;

varying vec3 vColor;

#define _TEXTURE
#define _RIGHT_EYE_ST
#define _VERTEXCOLORS
//#define _POLARUV
#define _OFFSET_TEXTURE
#define _MUL_RGB_BY_ALPHA
#define _MUL_ALPHA_INTENSITY

#ifdef _VERTEXCOLORS
void vertex(){
	vColor = COLOR.xyz;
}
#endif

void fragment() {
#if defined(_TEXTURE)
	vec4 _ST;
#if defined(_RIGHT_EYE_ST)
	if (VIEW_INDEX == 0)
		_ST = _Tex_ST;
#else
	_ST = _Tex_ST;
#endif

//TODO... this
//#if defined(_POLARUV)
//	vec2 uvddx, uvddy;
//	vec2 uv = TransformPolarMapping(UV * 2.0 - 1.0, _ST) * _PolarPower; // Where is the _PolarPower?
//#else
	// vec2 uv = i.uv * _ST.xy + _ST.zw;
	vec2 uv = UV;
//#endif

	// TODO!!! Polar mapping for Offset Texture as well?
	// Polar offets?
#if defined(_OFFSET_TEXTURE)
	vec4 offset = texture(_OffsetTex, TransformTex(UV, _OffsetTex_ST));
	uv += offset.xy * _OffsetMagnitude.xy;
#endif

// TODO... this
#if defined(_POLARUV)
//	vec4 col = textureGrad(Tex, uv, uvddx, uvddy);
	vec4 col = SampleTexture(Tex, UV);
#else
	vec4 col = SampleTexture(Tex, UV);
#endif

#if defined(_COLOR)
	col *= _Color;
#endif

#elif defined(_COLOR)
	vec4 col = _Color;
#else
	vec4 col = vec4(1);
#endif

#if defined(_ALPHATEST)
	clip(col.a - _Cutoff);
#endif

#ifdef _VERTEXCOLORS
	col.xyz *= vColor;
#endif

#ifdef _MUL_RGB_BY_ALPHA
	col.rgb *= col.a;
#endif

	// This is for external camera blending, so additive shaders output alpha
#ifdef _MUL_ALPHA_INTENSITY
	float mulfactor = (col.r + col.g + col.b) * 0.3333333;
	col.a *= mulfactor;
#endif

	ALBEDO = col.xyz;
}