#ifndef CULL_MODE
#define CULL_MODE 2
#endif

#if CULL_MODE == 0
render_mode cull_disabled;
#elif CULL_MODE == 1
render_mode cull_front;
#elif CULL_MODE == 2
render_mode cull_back;
#endif