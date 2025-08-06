shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

MapTexture2DOne(FontAtlas)

uniform vec4 _TintColor : source_color = vec4(1);
uniform vec4 _OverlayTint : source_color = vec4(1,1,1,0.5);
uniform vec4 _OutlineColor : source_color = vec4(1,1,1,0);
uniform vec4 _BackgroundColor : source_color = vec4(0);
uniform vec4 _Range = vec4(0.001, 0.001, 0, 0);
uniform float _FaceDilate;
uniform float _FaceSoftness;
uniform float _OutlineSize;
uniform vec4 _Rect = vec4(0,0,1,1);
uniform float _OffsetFactor = 0.0;
uniform float _OffsetUnits = 0.0;

void fragment()
{
    vec4 atlasColor = SampleTexture(FontAtlas, UV);
    
    #if defined(MSDF) || defined(SDF)
    vec2 msdfUnit = _Range.xy;

    #ifdef MSDF
    float sigDist = median(atlasColor.r, atlasColor.g, atlasColor.b) - 0.5;
    #elif defined(SDF)
    float sigDist = atlasColor.a - 0.5;
    #endif

    //sigDist += _FaceDilate + i.extraData.x;

    float antiAliasing = dot(msdfUnit, 0.5 / fwidth(UV));
    antiAliasing = max(antiAliasing, 1);

    float glyphLerp = lerp(sigDist * antiAliasing, sigDist, _FaceSoftness);
    glyphLerp = saturate(glyphLerp + 0.5);

    clip(max(glyphLerp, _BackgroundColor.a) - 0.001);

    vec4 fillColor = _TintColor * COLOR;

    #ifdef OUTLINE
    float outlineDist = sigDist - (_OutlineSize/* + i.extraData.y*/);
    float outlineLerp = lerp(outlineDist * antiAliasing, outlineDist, _FaceSoftness);

    outlineLerp = saturate(outlineLerp + 0.5);

    fillColor = lerp(_OutlineColor * vec4(1,1,1,COLOR.a), fillColor, outlineLerp);
    #endif

    vec4 result = lerp(_BackgroundColor * COLOR, fillColor, glyphLerp);
    ALBEDO = result.rgb;
    #endif
    
    #ifdef RASTER
    vec4 c = atlasColor * COLOR;

    clip(c.a - 0.001);

    ALBEDO = c.rgb;
    #endif
}