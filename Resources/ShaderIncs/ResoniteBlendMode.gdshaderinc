#ifndef BLEND_MODE

#ifdef DEFAULT_BLEND_MODE

#if DEFAULT_BLEND_MODE == 0
#define BLEND_MODE 0
#elif DEFAULT_BLEND_MODE == 1
#define BLEND_MODE 1
#elif DEFAULT_BLEND_MODE == 2
#define BLEND_MODE 2
#elif DEFAULT_BLEND_MODE == 3
#define BLEND_MODE 3
#elif DEFAULT_BLEND_MODE == 4
#define BLEND_MODE 4
#elif DEFAULT_BLEND_MODE == 5
#define BLEND_MODE 5
#endif

#else
#define BLEND_MODE 0
#endif

#endif

#if BLEND_MODE == 4
render_mode blend_add;
#elif BLEND_MODE == 5
render_mode blend_mul;
#endif

#if BLEND_MODE == 0

#define SetAlpha(a)

#else

#define SetAlpha(a) ALPHA = a

#endif

#if BLEND_MODE == 1

#define SetAlphaCutout(c) ALPHA_SCISSOR_THRESHOLD = c

#else

#define SetAlphaCutout(c)

#endif