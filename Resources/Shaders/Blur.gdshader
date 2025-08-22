shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZWriteMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform int Iterations : hint_range(1, 32, 1) = 4;
uniform vec4 Spread = vec4(0.1, 0.1, 0.0, 0.0);

ColorTexture2DWhite(SpreadTex)
STData(SpreadTex)

uniform float RefractionStrength = 0.01;

MapTexture2DZero(NormalMap)

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// #define REFRACT
// #define REFRACT_NORMALMAP
// #define SPREAD_TEX

void fragment() {
    float angleStep = TAU / float(Iterations);
    vec2 grabUv = SCREEN_UV;

#ifdef REFRACT_NORMALMAP
    NORMAL = normalize(NORMAL);

    mat3 tangentTransform = mat3(TANGENT, BINORMAL, NORMAL);
    vec3 bumpNormal = SampleSTTextureData(NormalMap, SpreadTex, UV).xyz;

    // Compute pertrubed normal, replacing the old one
    NORMAL = normalize(bumpNormal * tangentTransform);
#endif

#if defined(REFRACT) || defined(REFRACT_NORMALMAP)
    NORMAL = (VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
    grabUv -= (NORMAL.xy / SCREEN_UV) * RefractionStrength;
#endif

#ifdef SPREAD_TEX
    vec2 spread = SampleSTTexture(SpreadTex, SCREEN_UV).rg;
#else
    vec2 spread = Spread.xy;
#endif

    vec3 c = vec3(0);
    for (int n = 0; n < Iterations; n++)
    {
        float angle = angleStep * float(n);
        c += texture(SCREEN_TEXTURE, grabUv + vec2(-cos(angle), sin(angle)) * spread).xyz;
    }
    
    ALBEDO = (c / float(Iterations));
}