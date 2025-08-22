shader_type spatial;
render_mode unshaded;

varying vec3 Position;
varying vec3 Normal;

#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

// https://github.com/godotengine/godot-proposals/issues/6138
void vertex()
{
	Position = VERTEX;
	Normal = NORMAL;
}

float SquareAntialiased(float coord, float dd) // antialiased square wave
{
	coord += dd * 0.5f;
	dd = max(dd, 0.0001f);
	float invdd = 1.0f / dd;
	coord = mod(coord, 1.0f);
	float c = min(coord * invdd, 0.5f - (coord - 0.5f) / dd + 0.5f);
	c = clamp(c, 0.0f, 1.0f);
	return c;
}

float SXOR(float a, float b) // "smooth" xor, used to combine checker patterns
{
	return abs(a - b);
}

float Checkers(vec3 coord, vec3 ddxyz, vec3 tripCoeff)
{
	vec3 fade = clamp(1.0f / (ddxyz * 2.0f) - 1.0f, 0.0f, 1.0f);
	float cx = SquareAntialiased(coord.x, ddxyz.x);
	float cy = SquareAntialiased(coord.y, ddxyz.y);
	float cz = SquareAntialiased(coord.z, ddxyz.z);
	vec3 filter = clamp(1.0f / ddxyz * 0.5f - 0.5f, 0.0f, 1.0f); // smooth, manual filtering
	float c = SXOR(cy * filter.y, cz * filter.z) * tripCoeff.x;
	c += SXOR(cz * filter.z, cx * filter.x) * tripCoeff.y;
	c += SXOR(cx * filter.x, cy * filter.y) * tripCoeff.z;
	c += 1.0f - dot(filter, vec3(0.3333f));
	return c;
}

void fragment()
{
	vec3 coord = Position.xyz * 4.0f;
	vec3 ddxyz = fwidth(coord) * 1.0f;
	vec3 tripCoeff = max((Normal * Normal * 2.0f - 0.5f), 0.001f);
	tripCoeff /= (tripCoeff.x + tripCoeff.y + tripCoeff.z);
	float checkers = Checkers(coord, ddxyz, tripCoeff);
	coord = Position.xyz;
	ddxyz = fwidth(coord);
	float checkersL = 1.0f - Checkers(coord, ddxyz, tripCoeff);
	checkers = mix(checkers, checkersL, 0.1f);
	ALBEDO = mix(vec3(0.001f), vec3(0.01f), checkers);
}