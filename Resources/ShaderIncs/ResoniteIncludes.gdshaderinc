#include "ResoniteTextureHelpers.gdshaderinc"

#define lerp(a, b, v) mix(a, b, v)
#define saturate(v) clamp(v, 0.0, 1.0)
#define clip(v) if (v < 0.0) { discard; }

#define HandleOffset \
    vec3 view_pos = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz; \
    view_pos.z -= _OffsetUnits * 0.1f; \
    VERTEX = (inverse(MODELVIEW_MATRIX) * vec4(view_pos, 1.0)).xyz;
    
float Get2DClipping(vec2 position, vec4 clipRect)
{
    vec2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
    return inside.x * inside.y;
}

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
