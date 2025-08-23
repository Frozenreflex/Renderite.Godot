shader_type spatial;
render_mode blend_mix, depth_draw_always; // Add blend_add

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform vec4 _BaseColor : source_color = vec4(0.0);

uniform vec4 _AccumulationColor : source_color = vec4(0.1, 0.1, 0.1, 0.1);

uniform vec4 _AccumulationColorBottom : source_color = vec4(0.1, 0.1, 0.0, 0.1);
uniform vec4 _AccumulationColorTop : source_color = vec4(0.1, 0.1, 0.1, 0.1);

uniform float _AccumulationRate : hint_range(0.0, 1.0) = 0.1;

uniform float _FogStart : hint_range(0.0, 10000000.0) = 0.0;
uniform float _FogEnd : hint_range(0.0, 10000000.0) = 10000000.0;

uniform float _FogDensity : hint_range(0.0, 1.0) = 0.1;

uniform float _SrcBlend : hint_range(0.0, 1.0) = 1.0;
uniform float _DstBlend : hint_range(0.0, 1.0) = 0.0;

uniform sampler2D DEPTH_TEXTURE : hint_depth_texture;

#define OBJECT_SPACE
#define FOG_LINEAR
#define FOG_EXP
#define FOG_EXP2
#define COLOR_CONSTANT
#define COLOR_VERT_GRADIENT
#define SATURATE_ALPHA
#define SATURATE_COLOR

vec3 LinePlaneIntersection(vec3 linePoint, vec3 lineDirection, vec3 planePoint, vec3 planeNormal)
{
	vec3 diff = linePoint - planePoint;
	float prod1 = dot(diff, planeNormal);
	float prod2 = dot(lineDirection, planeNormal);
	float prod3 = prod1 / prod2;

	return linePoint - lineDirection * prod3;
}

void FilterPoint(vec3 refPoint, vec3 newPoint, vec2 checkSquare, inout vec3 lastPoint, inout float lastDistance)
{
	if (abs(checkSquare).x > 0.5 && abs(checkSquare).y > 0.5)
		return;

	float dist = distance(refPoint, newPoint);

	if (dist < lastDistance)
	{
		lastDistance = dist;
		lastPoint = newPoint;
	}
}

vec3 IntersectUnitCube(vec3 linePoint, vec3 lineDirection)
{
	vec3 i0 = LinePlaneIntersection(linePoint, lineDirection, vec3(-0.5, 0, 0), vec3(-1, 0, 0));
	vec3 i1 = LinePlaneIntersection(linePoint, lineDirection, vec3(0.5, 0, 0), vec3(1, 0, 0));
	vec3 i2 = LinePlaneIntersection(linePoint, lineDirection, vec3(0, -0.5, 0), vec3(0, -1, 0));
	vec3 i3 = LinePlaneIntersection(linePoint, lineDirection, vec3(0, 0.5, 0), vec3(0, 1, 0));
	vec3 i4 = LinePlaneIntersection(linePoint, lineDirection, vec3(0, 0, -0.5), vec3(0, 0, -1));
	vec3 i5 = LinePlaneIntersection(linePoint, lineDirection, vec3(0, 0, 0.5), vec3(0, 0, 1));

	float dist = 65000.0;
	vec3 p = linePoint;

	FilterPoint(linePoint, i0, i0.yz, p, dist);
	FilterPoint(linePoint, i1, i1.yz, p, dist);
	FilterPoint(linePoint, i2, i2.xz, p, dist);
	FilterPoint(linePoint, i3, i3.xz, p, dist);
	FilterPoint(linePoint, i4, i4.xy, p, dist);
	FilterPoint(linePoint, i5, i5.xy, p, dist);

	return p;
}

void fragment() {
	mediump vec3 ndir;

	float sceneZ = GetDLinearDepth(DEPTH_TEXTURE, UNITY_PROJ_COORD(i.projPos)));
	float partZ = i.projPos.z;

	vec3 start;
	vec3 end;

#ifdef OBJECT_SPACE
	vec3 camPos = (unity_WorldToObject * vec4(_WorldSpaceCameraPos.xyz, 1)).xyz;
	vec3 endPos = i.origin;

	ndir = normalize(endPos - camPos);

	// check if the camera position is inside of the cube
	start = clamp_inside_unit_cube(camPos, ndir);

	float maxDist = distance(camPos, endPos);
	float endRatio = min(sceneZ / partZ, 1);

	end = camPos + ndir * maxDist * endRatio;
#else
	// TODO!!! Can this code be factored out into the vertex shader instead? Particularly all the unit cube clipping
	vec3 clampedStartPos = unity_WorldToObject * vec4(_WorldSpaceCameraPos.xyz, 1)).xyz;
	ndir = normalize(i.origin.xyz - clampedStartPos);
	clampedStartPos = clamp_inside_unit_cube(clampedStartPos, -ndir);

	float clampedDepth = -UnityObjectToViewPos(clampedStartPos).z;

	if (clampedDepth > sceneZ)
		discard;

	// compute the sample origin position
	vec3 camPos = _WorldSpaceCameraPos.xyz;
	vec3 endPos = (unity_ObjectToWorld * vec4(i.origin.xyz, 1)).xyz;

	ndir = normalize(endPos - camPos);

	start = camPos;
	// TODO!!! This is technically not fully correct and will result in spherical distance, but calculating actual world position
	// requires inverse matrix, for computing clip to world transformation
	end = camPos + sceneZ * ndir;
#endif

	if (distance_sqr(camPos, end) < distance_sqr(camPos, start))
		discard;

	// total distance that needs to be travelled
	float dist = distance(start, end);

#ifdef FOG_LINEAR
	dist = min(_FogEnd, dist);
	dist -= _FogStart;
	dist = max(0, dist);
#endif

#ifdef FOG_EXP
	dist = 1 - (1 / exp(dist * _FogDensity));
#endif

#ifdef FOG_EXP2
	float d = dist * _FogDensity;
	dist = 1 - (1 / exp(d * d));
#endif
	vec4 accColor;

#if defined(COLOR_CONSTANT)
	accColor = _AccumulationColor;
#elif defined(COLOR_VERT_GRADIENT)
	// need to compute the start and end points within the cube volume
	float startY;
	float endY;
#ifdef OBJECT_SPACE
	startY = start.y;
	endY = end.y;
#else
	vec3 localStart = (unity_WorldToObject * vec4(start, 1)).xyz;
	vec3 localEnd = (unity_WorldToObject * vec4(end, 1)).xyz;

	localStart = clamp_inside_unit_cube(localStart, normalize(localStart));
	localEnd = clamp_inside_unit_cube(localEnd, normalize(localEnd));

	startY = localStart.y;
	endY = localEnd.y;
#endif
	float avgY = (startY + endY) * 0.5 + 0.5;

	accColor = mix(_AccumulationColorBottom, _AccumulationColorTop, clamp(avgY, 0, 1));
#endif

	vec4 acc = dist * accColor * _AccumulationRate;

	vec4 resultColor = _BaseColor + acc;

#if defined(SATURATE_ALPHA)
	resultColor.a = clamp(resultColor.a, 0, 1);
#elif defined(SATURATE_COLOR)
	resultColor = clamp(resultColor, 0, 1);
#endif

	ALBEDO = resultColor.xyz;
	// ALPHA = resultColor.a;
}