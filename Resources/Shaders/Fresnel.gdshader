shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

ColorTexture2DWhite(FarTex)
STData(FarTex_ST)

ColorTexture2DWhite(NearTex)
STData(NearTex_ST)

MapTexture2DZero(NormalMap)
STData(NormalMap_ST)
uniform float NormalScale : hint_range(0.0, 1.0) = 1.0;

ColorTexture2DWhite(MaskTex)
STData(MaskTex_ST)

uniform vec4 _FarColor : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 _NearColor : source_color = vec4(1.0);
uniform float _Exp : hint_range(0.0, 1.0) = 1.0;
uniform float _PolarPow = 1.0;
uniform float _Cutoff : hint_range(0,1) = 0.5;

// Passed in at runtime
//#define NormalMap
//#define _TEXTURE
//#define _POLARUV
//#define _MASK_TEXTURE_MUL
//#define _MASK_TEXTURE_CLIP
//#define _ALPHATEST
//#define _VERTEXCOLORS
//#define _MUL_ALPHA_INTENSITY

varying vec4 vColor;

void vertex() {
	vColor = COLOR;
}

void fragment() {
	NORMAL = normalize(NORMAL);

	#ifdef NormalMap

	mat3 tangentTransform = mat3(TANGENT, BINORMAL, NORMAL);
	vec3 bumpNormal = texture(NormalMap, TRANSFORM_TEX(UV, NormalMap_ST)).xyz * NormalScale;

	// Compute pertrubed normal, replacing the old one
	NORMAL = normalize(bumpNormal * tangentTransform);

	#endif

	float fresnel = pow(1.0 - abs(dot(NORMAL, VIEW)), _Exp);

	mediump vec4 farColor = _FarColor;
	mediump vec4 nearColor = _NearColor;

	#ifdef _TEXTURE

		#ifdef _POLARUV
		mediump vec2 polarUv = PolarUV(UV * 2.0 - 1.0, _PolarPow);
		farColor *= SampleTex2Dpolar(FarTex, polarUv, FarTex_ST);
		nearColor *= SampleTex2Dpolar(NearTex, polarUv, NearTex_ST);
		#else
		farColor *= texture(FarTex, TRANSFORM_TEX(UV, FarTex));
		nearColor *= texture(NearTex, TRANSFORM_TEX(UV, NearTex));
		#endif

	#endif

	// compute final color by blending between the two based on the fresnel

	mediump vec4 col = mix(nearColor, farColor, fresnel);

#if defined(_MASK_TEXTURE_MUL) || defined(_MASK_TEXTURE_CLIP)
	vec4 mask = texture(MaskTex, TRANSFORM_TEX(UV, MaskTex_ST));

	float mul = (mask.r + mask.g + mask.b) * 0.3333333 * mask.a;

#ifdef _MASK_TEXTURE_MUL
	col.a *= mul;
#endif

#ifdef _MASK_TEXTURE_CLIP
	if (mul - _Cutoff <= 0.0)
		discard;
#endif

#endif

#if defined(_ALPHATEST) && !defined(_MASK_TEXTURE_CLIP)
	if (col.a - _Cutoff <= 0.0)
		discard;
#endif

#if defined(_VERTEXCOLORS)
	col *= vColor;
#endif

#ifdef _MUL_ALPHA_INTENSITY
	float mulfactor = (col.r + col.g + col.b) * 0.3333333;
	col.a *= mulfactor * mulfactor;
#endif

	ALBEDO = col.xyz;
}