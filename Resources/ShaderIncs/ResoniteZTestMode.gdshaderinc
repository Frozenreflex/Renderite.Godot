#ifndef ZTEST_MODE
#define ZTEST_MODE 0
#endif

#if ZTEST_MODE == 0
render_mode depth_test_default;
#elif ZTEST_MODE == 1
render_mode depth_test_inverted;
#elif ZTEST_MODE == 2
render_mode depth_test_disabled;
#endif