#include "ResoniteTextureHelpers.gdshaderinc"

#define lerp(a, b, v) mix(a, b, v)
#define saturate(v) clamp(v, 0.0, 1.0)
#define clip(v) if (v < 0.0) { discard; }

#define HandleOffset \
    vec3 view_pos = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz; \
    view_pos.z -= _OffsetUnits * 0.1f; \
    VERTEX = (inverse(MODELVIEW_MATRIX) * vec4(view_pos, 1.0)).xyz;