const int TFUWrap = 3;
const int TFVWrap = 12;
const int TFFilter = 48;

const int TFUWrapRepeat = 0 << 0;
const int TFUWrapClamp = 1 << 0;
const int TFUWrapMirror = 2 << 0;
const int TFUWrapMirrorOnce = 3 << 0;

const int TFVWrapRepeat = 0 << 2;
const int TFVWrapClamp = 1 << 2;
const int TFVWrapMirror = 2 << 2;
const int TFVWrapMirrorOnce = 3 << 2;

const int TFFilterNearest = 0 << 4;
const int TFFilterLinear = 1 << 4;
const int TFFilterAnisotropic = 2 << 4;

float PingPong(float value)
{
    return abs(mod(value + 1.0, 2.0) - 1.0);
}
vec2 WrapCoordinates(vec2 uv, int flags)
{
    float x, y;

    int uWrap = flags & TFUWrap;
    int vWrap = flags & TFVWrap;

    if (uWrap == TFUWrapClamp)
    {
        x = clamp(uv.x, 0.0, 1.0);
    }
    else if (uWrap == TFUWrapMirror)
    {
        x = PingPong(uv.x);
    }
    else if (uWrap == TFUWrapMirrorOnce)
    {
        x = PingPong(clamp(uv.x, -1.0, 2.0));
    }
    else
    {
        x = uv.x;
    }

    if (vWrap == TFVWrapClamp)
    {
        y = clamp(uv.y, 0.0, 1.0);
    }
    else if (vWrap == TFVWrapMirror)
    {
        y = PingPong(uv.y);
    }
    else if (vWrap == TFVWrapMirrorOnce)
    {
        y = PingPong(clamp(uv.y, -1.0, 2.0));
    }
    else
    {
        y = uv.y;
    }

    return vec2(x, y);
}
vec2 TransformTex(vec2 tex, vec4 offset)
{
	return tex * offset.xy + offset.zw;
}
vec2 _TransformUV(vec2 uv, vec4 st, int flags)
{
    return WrapCoordinates(TransformTex(uv, st), flags);
}
vec4 _SampleTexture(sampler2D nearest, sampler2D anisotropic, sampler2D linear, vec2 uv, int flags)
{
    vec2 wrapped = WrapCoordinates(uv, flags);
    int filter = flags & TFFilter;
    if (filter == TFFilterLinear)
    {
        return texture(linear, wrapped);
    }
    if (filter == TFFilterNearest)
    {
        return texture(nearest, wrapped);
    }
    return texture(anisotropic, wrapped);
}

vec4 _SampleSTTexture(sampler2D nearest, sampler2D anisotropic, sampler2D linear, vec2 uv, int flags, vec4 st)
{
    return _SampleTexture(nearest, anisotropic, linear, TransformTex(uv, st), flags);
}

vec3 ReadNormal(vec4 value)
{
    //resonite packs normals in a way that unity wants them
    //TODO: is this correct? i saw something that said X = A and Y = R, but another that said X = R and Y = A
    float x = (value.r * 2.0) - 1.0;
    float y = (value.a * 2.0) - 1.0;
    float z = sqrt(1.0 - (x*x + y*y));
    return vec3(x,y,z);
}

vec3 BlendNormals(vec3 n1, vec3 n2)
{
    return normalize(vec3(n1.xy + n2.xy, n1.z*n2.z));
}

#define STData(T) \
uniform vec4 _##T##_ST = vec4(1.0,1.0,0.0,0.0);

#define ColorTexture2D(T) \
\
uniform sampler2D _##T##_Nearest : source_color, filter_nearest, repeat_enable; \
uniform sampler2D _##T##_Linear : source_color, filter_linear, repeat_enable; \
uniform sampler2D _##T##_Anisotropic : source_color, filter_linear_mipmap_anisotropic, repeat_enable; \
uniform int _##T##_Flags;

#define ColorTexture2DWhite(T) \
\
uniform sampler2D _##T##_Nearest : source_color, filter_nearest, repeat_enable, hint_default_white; \
uniform sampler2D _##T##_Linear : source_color, filter_linear, repeat_enable, hint_default_white; \
uniform sampler2D _##T##_Anisotropic : source_color, filter_linear_mipmap_anisotropic, repeat_enable, hint_default_white; \
uniform int _##T##_Flags;

#define ColorTexture2DBlack(T) \
\
uniform sampler2D _##T##_Nearest : source_color, filter_nearest, repeat_enable, hint_default_black; \
uniform sampler2D _##T##_Linear : source_color, filter_linear, repeat_enable, hint_default_black; \
uniform sampler2D _##T##_Anisotropic : source_color, filter_linear_mipmap_anisotropic, repeat_enable, hint_default_black; \
uniform int _##T##_Flags;

#define ColorTexture2DTransparent(T) \
\
uniform sampler2D _##T##_Nearest : source_color, filter_nearest, repeat_enable, hint_default_transparent; \
uniform sampler2D _##T##_Linear : source_color, filter_linear, repeat_enable, hint_default_transparent; \
uniform sampler2D _##T##_Anisotropic : source_color, filter_linear_mipmap_anisotropic, repeat_enable, hint_default_transparent; \
uniform int _##T##_Flags;

#define MapTexture2D(T) \
\
uniform sampler2D _##T##_Nearest : filter_nearest, repeat_enable; \
uniform sampler2D _##T##_Linear : filter_linear, repeat_enable; \
uniform sampler2D _##T##_Anisotropic : filter_linear_mipmap_anisotropic, repeat_enable; \
uniform int _##T##_Flags;

#define MapTexture2DOne(T) \
\
uniform sampler2D _##T##_Nearest : filter_nearest, repeat_enable, hint_default_white; \
uniform sampler2D _##T##_Linear : filter_linear, repeat_enable, hint_default_white; \
uniform sampler2D _##T##_Anisotropic : filter_linear_mipmap_anisotropic, repeat_enable, hint_default_white; \
uniform int _##T##_Flags;

#define MapTexture2DZero(T) \
\
uniform sampler2D _##T##_Nearest : filter_nearest, repeat_enable, hint_default_transparent; \
uniform sampler2D _##T##_Linear : filter_linear, repeat_enable, hint_default_transparent; \
uniform sampler2D _##T##_Anisotropic : filter_linear_mipmap_anisotropic, repeat_enable, hint_default_transparent; \
uniform int _##T##_Flags;

#define SampleSTTexture(T, uv) _SampleSTTexture(_##T##_Nearest, _##T##_Anisotropic, _##T##_Linear, uv##, _##T##_Flags, _##T##_ST)
#define SampleSTTextureData(T, data, uv) _SampleSTTexture(_##T##_Nearest, _##T##_Anisotropic, _##T##_Linear, uv##, _##T##_Flags, _##data##_ST)
#define SampleTexture(T, uv) _SampleTexture(_##T##_Nearest, _##T##_Anisotropic, _##T##_Linear, uv##, _##T##_Flags)