shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

//ColorTexture2DWhite(FontAtlas)
MapTexture2DOne(FontAtlas)

uniform vec4 _TintColor = vec4(1);
uniform vec4 _OverlayTint = vec4(1,1,1,0.5);
uniform vec4 _OutlineColor = vec4(1,1,1,0);
uniform vec4 _BackgroundColor = vec4(0);
uniform vec4 _Range = vec4(0.001, 0.001, 0, 0);
uniform float _FaceDilate;
uniform float _FaceSoftness;
uniform float _OutlineSize;
uniform vec4 _Rect = vec4(0,0,1,1);

float median(float r, float g, float b)
{
	return max(min(r, g), min(max(r, g), b));
}
void fragment()
{
    vec4 atlasColor = SampleTexture(FontAtlas, UV);
    vec2 msdfUnit = _Range.xy;
    float sigDist = median(atlasColor.r, atlasColor.g, atlasColor.b) - 0.5;
    //sigDist += _FaceDilate + NORMAL.x;

    float antiAliasing = dot(msdfUnit, 0.5 / fwidth(UV));
    antiAliasing = max(antiAliasing, 1);

    float glyphLerp = lerp(sigDist * antiAliasing, sigDist, _FaceSoftness);
    glyphLerp = saturate(glyphLerp + 0.5);
    clip(max(glyphLerp, _BackgroundColor.a) - 0.001);

    vec4 fillColor = _TintColor * COLOR;

    float outlineDist = sigDist - (_OutlineSize/* + NORMAL.y*/);
    float outlineLerp = lerp(outlineDist * antiAliasing, outlineDist, _FaceSoftness);

    outlineLerp = saturate(outlineLerp + 0.5);

    fillColor = lerp(_OutlineColor * vec4(1,1,1,COLOR.a), fillColor, outlineLerp);

    vec4 c = lerp(_BackgroundColor * COLOR, fillColor, glyphLerp);
    ALBEDO = c.rgb;
    SetAlpha(c.a);
    /*
    float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
    float partZ = i.projPos.z;

    if (partZ > sceneZ)
    {
        c *= _OverlayTint;
    }
    */
}