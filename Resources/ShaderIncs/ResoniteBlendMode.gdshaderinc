#ifndef BLEND_MODE
#define BLEND_MODE 0
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