shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform vec4 _Point = vec4(0);
uniform float _Distance = 1;
uniform float _Transition = 0.1;
uniform float _Cutoff = 0.5;

ColorTexture2DWhite(NearTexture)
STData(NearTexture)

ColorTexture2DWhite(FarTexture)
STData(FarTexture)

uniform vec4 _NearColor : source_color = vec4(1);
uniform vec4 _FarColor : source_color = vec4(0,0,0,1);

// Pragma includes for this shader
//#define WORLD_SPACE
//#define VERTEXCOLORED
//#define ALPHA_TEST

varying vec4 position;

#ifdef VERTEXCOLORED
varying vec3 vertexColors;
#endif

void vertex()
{
	
#ifdef WORLD_SPACE
	position = MODELVIEW_MATRIX * vec4(VERTEX, 1);
#else
	position = vec4(VERTEX, 1);
#endif

#ifdef VERTEXCOLORED
	vertexColors = COLOR.rgb;
#endif
}

void fragment() 
{
	float _dist = distance(_Point.xyz, position.xyz);
	_dist -= _Distance;
	
	float _lerp = clamp((_dist / _Transition) + (_Transition * 0.5), 0.0, 1.0);
	
	vec4 cNear = SampleSTTexture(NearTexture, UV) * _NearColor;
	vec4 cFar = SampleSTTexture(FarTexture, UV) * _FarColor;
	vec4 c = mix(cNear, cFar, _lerp);
	
#ifdef VERTEXCOLORED
	c.rgb *= vertexColors;
#endif
	
#ifdef ALPHA_TEST
	if (c.a - Cutoff <= 0.0)
		discard;
#endif

	ALBEDO = c.rgb;
    
    SetAlpha(c.a);
    SetAlphaCutout(_Cutoff);
}

