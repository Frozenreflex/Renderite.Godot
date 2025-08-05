shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"


ColorTexture2DWhite(MainTex)
STData(MainTex)

MapTexture2DOne(MaskTex)
STData(MaskTex)

uniform vec4 _Tint : source_color = vec4(1.0);
uniform vec4 _OverlayTint : source_color = vec4(1.0,1.0,1.0,0.5);
uniform float _Cutoff = 0.98;
uniform float _OffsetFactor = 0.0;
uniform float _OffsetUnits = 0.0;
uniform vec4 _Rect = vec4(0,0,1,1);

DefineRectClip

#ifdef TEXTURE_LERPCOLOR
varying vec3 LerpColor;
#endif

void vertex() 
{
    StartRectClip
    
    #ifdef TEXTURE_LERPCOLOR
    LerpColor = TANGENT;
    #endif
}

void fragment() 
{
    DoRectClip(_Rect);
    
    vec4 color = SampleSTTexture(MainTex, UV);
    #ifdef TEXTURE_NORMALMAP
    color = vec4(UnpackScaleNormal(color, 1) * 0.5 + 0.5, 1);
    #endif
    
    #ifdef TEXTURE_LERPCOLOR
    float l = (color.r + color.g + color.b) * 0.3333333333;
    vec4 lerpColor = lerp(COLOR, vec4(LerpColor,1.0), l);

    color = vec4(lerpColor.rgb, lerpColor.a * color.a);
    #else
    color *= COLOR;
    #endif
    
    #if defined(_MASK_TEXTURE_MUL) || defined(_MASK_TEXTURE_CLIP)
    vec4 mask = SampleTexture(MaskTex, UV);

    float mul = (mask.r + mask.g + mask.b) * 0.3333333 * mask.a;

    #ifdef _MASK_TEXTURE_MUL
    color.a *= mul;
    #endif
    
    #ifdef _MASK_TEXTURE_CLIP
    if (mul - _Cutoff <= 0.0) 
    {
        discard;
    }
    #endif
    
    #endif
    
    #if defined(ALPHACLIP) && !defined(_MASK_TEXTURE_CLIP)
    clip(color.a - _Cutoff);
    #endif
    
    ALBEDO = color.rgb;
    SetAlpha(color.a);
}