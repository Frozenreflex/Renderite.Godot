shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

//ColorTexture2DWhite(FontAtlas)
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

DefineRectClip

#ifdef OVERLAY
uniform sampler2D depthTexture : hint_depth_texture;
#endif

void vertex()
{
    StartRectClip
}

void fragment()
{
    DoRectClip(_Rect);

    vec4 c = vec4(1.0);

    vec4 atlasColor = SampleTexture(FontAtlas, UV);
    #if defined(MSDF) || defined(SDF)
    vec2 msdfUnit = _Range.xy;
    #if defined(MSDF)
    float sigDist = median(atlasColor.r, atlasColor.g, atlasColor.b) - 0.5;
    #elif defined(SDF)
    float sigDist = atlasColor.a - 0.5;
    #endif

    //sigDist += _FaceDilate + NORMAL.x;

    float antiAliasing = dot(msdfUnit, 0.5 / fwidth(UV));
    antiAliasing = max(antiAliasing, 1);

    float glyphLerp = lerp(sigDist * antiAliasing, sigDist, _FaceSoftness);
    glyphLerp = saturate(glyphLerp + 0.5);
    clip(max(glyphLerp, _BackgroundColor.a) - 0.001);

    vec4 fillColor = _TintColor * COLOR;

    #ifdef OUTLINE
    float outlineDist = sigDist - (_OutlineSize/* + NORMAL.y*/);
    float outlineLerp = lerp(outlineDist * antiAliasing, outlineDist, _FaceSoftness);
    outlineLerp = saturate(outlineLerp + 0.5);
    fillColor = lerp(_OutlineColor * vec4(1,1,1,COLOR.a), fillColor, outlineLerp);
    #endif

    c = lerp(_BackgroundColor * COLOR, fillColor, glyphLerp);
    #endif

    #if defined(RASTER)
    c = atlasColor * COLOR;

    clip(c.a - 0.001);
    #endif

    #ifdef OVERLAY
    float sceneZ = SceneLinearDepth(depthTexture);
    float partZ = FragmentDepth;
    if (partZ > sceneZ) { c *= _OverlayTint; }
    #endif

    ALBEDO = c.rgb;
    SetAlpha(c.a);
}