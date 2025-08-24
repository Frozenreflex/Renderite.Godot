#include "ResoniteTextureHelpers.gdshaderinc"

#define lerp(a, b, v) mix(a, b, v)
#define saturate(v) clamp(v, 0.0, 1.0)
#define clip(v) if (v < 0.0) { discard; }

vec3 calculate_barycentric(vec3 p1, vec3 p2, vec3 p3, vec3 point) {
    vec3 v0 = p3 - p1;
    vec3 v1 = p2 - p1;
    vec3 v2 = point - p1;

    float dot00 = dot(v0, v0);
    float dot01 = dot(v0, v1);
    float dot02 = dot(v0, v2);
    float dot11 = dot(v1, v1);
    float dot12 = dot(v1, v2);

    float inv_denom = 1.0 / (dot00 * dot11 - dot01 * dot01);
    float u = (dot11 * dot02 - dot01 * dot12) * inv_denom;
    float v = (dot00 * dot12 - dot01 * dot02) * inv_denom;

    return vec3(1.0 - u - v, v, u);
}

float median(float r, float g, float b)
{
	return max(min(r, g), min(max(r, g), b));
}

float Get2DClipping(vec2 position, vec4 clipRect)
{
    vec2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
    return inside.x * inside.y;
}

float _SceneLinearDepth(float depthSample, vec2 screenUv, mat4 invProjectionMatrix, vec3 vertex)
{
    vec3 ndc = vec3(screenUv * 2.0 - 1.0, depthSample);
    vec4 view = invProjectionMatrix * vec4(ndc, 1.0);
    view.xyz /= view.w;
    
    return -view.z;
}

#define SceneLinearDepth(depth) _SceneLinearDepth(texture(depth, SCREEN_UV).x, SCREEN_UV, INV_PROJECTION_MATRIX, VERTEX)
#define FragmentDepth (-VERTEX.z)

#ifdef _VERTEXCOLORS
#define EVR_APPLY_VERTEX_COLORS_FRAG(c) c *= COLOR;
#else
#define EVR_APPLY_VERTEX_COLORS_FRAG(c) // no-op
#endif

#ifdef RECTCLIP
#define DefineRectClip varying vec2 Vert;
#else
#define DefineRectClip
#endif

#ifdef RECTCLIP
#define StartRectClip Vert = VERTEX.xy * vec2(-1,1);
#else
#define StartRectClip
#endif

#ifdef RECTCLIP
#define DoRectClip(T) clip(Get2DClipping(Vert, T ) - 0.1)
#else
#define DoRectClip(T)
#endif
