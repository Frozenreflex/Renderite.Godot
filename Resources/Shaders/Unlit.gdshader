shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

ColorTexture2DWhite(Tex)
STData(Tex)

ColorTexture2DBlack(OffsetTex)
STData(OffsetTex)

ColorTexture2DWhite(MaskTex)
STData(MaskTex)

STData(RightEye)

uniform float _Cutoff = 0.5;
uniform vec4 _Color : source_color = vec4(1.0);
uniform vec4 _OffsetMagnitude = vec4(0.1,0.1,0.0,0.0);
uniform float _PolarPow = 1.0;
uniform float _OffsetFactor = 0.0;
uniform float _OffsetUnits = 0.0;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() 
{
    #if defined(_TEXTURE) || defined(_TEXTURE_NORMALMAP)

    vec4 _ST;
    #if defined(_RIGHT_EYE_ST)
	if (VIEW_INDEX == 0) { _ST = _Tex_ST; }
    else { _ST = _RightEye_ST; }
    #else
    _ST = _Tex_ST;
    #endif

    #if defined(_POLARUV)
    vec2 uv = PolarMapping(i.texcoord * 2.0 - 1.0, _ST, _PolarPow);
    #else
    vec2 uv = UV * _ST.xy + _ST.zw;
    #endif

    #if defined(_OFFSET_TEXTURE)
    //vec4 offset = tex2D(_OffsetTex, TRANSFORM_TEX(i.texcoord, _OffsetTex));
    vec4 offset = SampleTexture(OffsetTex, UV);
    uv += offset.xy * _OffsetMagnitude.xy;
    #endif

    //removed grad sample
    vec4 col = SampleTexture(Tex, uv);

    #if defined(_TEXTURE_NORMALMAP)
    col = vec4(UnpackScaleNormal(col, 1.0) * 0.5 + 0.5, 1.0);
    #endif

    #if defined(_COLOR)
    col *= _Color;
    #endif

    #elif defined(_COLOR)
    vec4 col = _Color;
    #else
    vec4 col = vec4(1.0);
    #endif
    
    #if defined(_MASK_TEXTURE_MUL) || defined(_MASK_TEXTURE_CLIP)
    vec4 mask = SampleTexture(MaskTex, UV);

    float mul = (mask.r + mask.g + mask.b) * 0.3333333 * mask.a;

    #ifdef _MASK_TEXTURE_MUL
    col.a *= mul;
    #endif

    #ifdef _MASK_TEXTURE_CLIP
    if (mul - _Cutoff <= 0.0) { discard; }
    #endif

    #endif

    #if defined(_ALPHATEST) && !defined(_MASK_TEXTURE_CLIP)
    if (col.a - _Cutoff <= 0.0) { discard; }
    //clip(col.a - _Cutoff);
    #endif

    EVR_APPLY_VERTEX_COLORS_FRAG(col);

    #ifdef _MUL_RGB_BY_ALPHA
    col.rgb *= col.a;
    #endif
    
    #ifdef _MUL_ALPHA_INTENSITY
    float mulfactor = (col.r + col.g + col.b) * 0.3333333;
    col.a *= mulfactor;
    #endif
    
    ALBEDO = col.rgb;
    SetAlpha(col.a);
    SetAlphaCutout(_Cutoff);
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
