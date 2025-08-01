#include "ResoniteTextureHelpers.gdshaderinc"

#define lerp(a, b, v) mix(a, b, v)
#define saturate(v) clamp(v, 0.0, 1.0)
#define clip(v) if (v < 0.0) { discard; }