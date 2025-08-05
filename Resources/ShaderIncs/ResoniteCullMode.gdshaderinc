#ifndef CULL_MODE

#ifdef DEFAULT_CULL_MODE

#if DEFAULT_CULL_MODE == 0
#define CULL_MODE 0
#elif DEFAULT_CULL_MODE == 1
#define CULL_MODE 1
#elif DEFAULT_CULL_MODE == 2
#define CULL_MODE 2
#endif

#else
#define CULL_MODE 2
#endif

#endif

#if CULL_MODE == 0
render_mode cull_disabled;
#elif CULL_MODE == 1
render_mode cull_front;
#elif CULL_MODE == 2
render_mode cull_back;
#endif