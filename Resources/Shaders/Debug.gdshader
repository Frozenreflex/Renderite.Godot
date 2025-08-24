shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform vec3 _Scale = vec3(1.0, 1.0, 1.0);
uniform vec3 _Offset = vec3(0.0, 0.0, 0.0);
varying vec3 data;

#define _POSITION

void vertex () {
#if		defined(_POSITION)
	data = VERTEX;
#elif	defined(_COLOR)
	data = COLOR.rgb;
#elif	defined(_COLOR_ALPHA)
	data = COLOR.aaa;
#elif	defined(_NORMAL)
	data = NORMAL;
#elif	defined(_TANGENT)
	data = TANGENT.xyz;
#elif	defined(_TANGENT4)
	data = TANGENT.xyz;
#elif	defined(_UV0)
	data = vec3(UV, 1);
#elif	defined(_UV1)
	data = vec3(UV2, 1);
#elif	defined(_UV2)
	data = vec3(UV2, 1);
#elif	defined(_UV3)
	data = vec3(UV2, 1);
#endif
}

void fragment() {
#ifdef _BITANGENT
	ALBEDO = cross(NORMAL, TANGENT.xyz) * TANGENT.w;
#endif
#ifdef _NORMALIZE
	ALBEDO = normalize(data);
#endif

	ALBEDO = clamp((data * _Scale) + _Offset, 0.0, 1.0);
}