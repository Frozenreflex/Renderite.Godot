shader_type spatial;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

ColorTexture2DWhite(MainTex)
STData(MainTex)
MapTexture2D(EmissionMap)
MapTexture2DZero(BumpMap)
MapTexture2D(OcclusionMap)
MapTexture2DZero(ParallaxMap)
MapTexture2DOne(MetallicGlossMap)

ColorTexture2DWhite(DetailAlbedoMap)
STData(DetailAlbedoMap)
MapTexture2DZero(DetailNormalMap)

uniform vec4 _Color = vec4(1,1,1,1);
uniform float _Metallic = 1.0;
uniform float _Glossiness = 0.5;
uniform float _Cutoff = 0.5f;
uniform vec4 _EmissionColor = vec4(0,0,0,1);
uniform float _BumpScale = 1.0;
uniform float _DetailNormalMapScale = 1.0;
uniform float _Parallax = 0.02;
uniform float _OffsetFactor = 0.0;
uniform float _OffsetUnits = 0.0;

void fragment()
{
    vec4 albedo = _Color * SampleSTTexture(MainTex, UV) 
    #ifdef _DETAIL_MULX2
    albedo *= SampleSTTexture(DetailAlbedoMap, UV);
    #endif
    SetAlpha(albedo.a);
    SetAlphaCutout(_Cutoff);
    ALBEDO = albedo.rgb;
    
    #if defined(_NORMALMAP) || defined(_DETAIL_MULX2)
    vec3 normals = ReadNormal(SampleSTTextureData(BumpMap, MainTex, UV)) * _BumpScale;
    #ifdef _DETAIL_MULX2
    normals = BlendNormals(normals, ReadNormal(SampleSTTextureData(DetailNormalMap, DetailAlbedoMap, UV)) * _DetailNormalMapScale);
    #endif
    NORMAL_MAP = normals;
    #endif
    
    #ifdef _METALLICGLOSSMAP
    vec4 metallicGloss = SampleSTTextureData(MetallicGlossMap, MainTex, UV);
    METALLIC = metallicGloss.r * _Metallic;
    ROUGHNESS = 1.0 - (metallicGloss.a * _Glossiness);
    #else
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    #endif

    #ifdef _EMISSION
    vec3 emissionColor = (_EmissionColor * SampleSTTextureData(EmissionMap, MainTex, UV)).rgb;
    EMISSION = emissionColor;
    #endif
}
