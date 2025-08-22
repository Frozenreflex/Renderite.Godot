shader_type spatial;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

ColorTexture2DWhite(MainTex)
STData(MainTex)

MapTexture2DZero(NormalMap)
STData(NormalMap)

#define _NORMALMAP

void vertex() {
#ifdef _NORMALMAP
	UV = TransformTex(UV, NormalMap_ST);
	BINORMAL = normalize(cross(NORMAL, TANGENT));
#endif
}

void fragment() {
#ifdef _NORMALMAP
	mat3 tangentTransform = mat3(TANGENT, BINORMAL, NORMAL);
	vec3 bumpNormal = texture(NormalMap, TransformTex(UV, NormalMap_ST)).xyz;

	// Compute pertrubed normal, replacing the old one
	NORMAL = normalize(bumpNormal * tangentTransform);
#endif
	// transform the normal into the screen coordinates
	NORMAL = (VIEW_MATRIX * vec4(NORMAL, 1)).xyz;

	// sample the materialcap texture by converting normal to UV
	vec2 uv = (NORMAL.xy * 0.5) + 0.5;
	vec4 col = texture(MainTexture, uv);
	
	ALBEDO = col.rgb;
}