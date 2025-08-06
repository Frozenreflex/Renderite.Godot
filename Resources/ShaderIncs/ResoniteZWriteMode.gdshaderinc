#ifndef ZWRITE_MODE

#ifdef DEFAULT_ZWRITE_MODE

#if DEFAULT_ZWRITE_MODE == 0
#define ZWRITE_MODE 0
#elif DEFAULT_ZWRITE_MODE == 1
#define ZWRITE_MODE 1
#endif

#else
#define ZWRITE_MODE 0
#endif

#endif

#if ZWRITE_MODE == 0
render_mode depth_draw_always;
#elif ZWRITE_MODE == 1
render_mode depth_draw_never;
#endif