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

#define fmod(x, y) (x - y * trunc(x/y))

vec2 PolarUV(vec2 rawUv, float radiusPow)
{
	const float ANGLE_LENGTH = (3.141592653589 * 2.0);
	const float DIV_ANGLE = 1.0 / ANGLE_LENGTH;

	float radius = pow(length(rawUv), radiusPow);
	float angle = atan(rawUv.x, rawUv.y) + ANGLE_LENGTH * 0.5;

	return vec2(angle * DIV_ANGLE, radius);
}
vec2 TransformPolarMapping(vec2 polarUv, vec4 tex_ST/*, out vec2 uvddx, out vec2 uvddy*/)
{
	polarUv = polarUv * tex_ST.xy + tex_ST.zw;
    /*
	float x0 = polarUv.x;
	float x1 = fmod(abs(polarUv.x) + 0.5, 1.0);

	vec2 coord0 = vec2(x0, polarUv.y);
	vec2 coord1 = vec2(x1, polarUv.y);

	vec2 uvddx0 = dFdx(coord0);
	vec2 uvddy0 = dFdy(coord0);
	vec2 uvddx1 = dFdx(coord1);
	vec2 uvddy1 = dFdy(coord1);

	uvddx = uvddx0;
	uvddy = uvddy0;

	float l0 = length(uvddx0) + length(uvddy0);
	float l1 = length(uvddx1) + length(uvddy1);

	if (l1 < l0)
	{
		uvddx = uvddx1;
		uvddy = uvddy1;
	}
    */

	return polarUv;
}
vec2 PolarMapping(vec2 rawUv, vec4 tex_ST, float radiusPow/*, out vec2 uvddx, out vec2 uvddy*/)
{
	rawUv = PolarUV(rawUv, radiusPow);
	return TransformPolarMapping(rawUv, tex_ST/*, uvddx, uvddy*/);
}
vec2 TransformTex(vec2 tex, vec4 offset)
{
	return tex * offset.xy + offset.zw;
}

vec3 RGB_To_HSV_NoClip(inout vec3 RGB) 
{
	vec3 HSV;

	float minChannel, maxChannel;
	if (RGB.x > RGB.y) {
		maxChannel = RGB.x;
		minChannel = RGB.y;
	}
	else {
		maxChannel = RGB.y;
		minChannel = RGB.x;
	}

	if (RGB.z > maxChannel) maxChannel = RGB.z;
	if (RGB.z < minChannel) minChannel = RGB.z;

	HSV.xy = vec2(0);
	HSV.z = maxChannel;
	float delta = maxChannel - minChannel;             //Delta RGB value
	if (delta != 0.0) {                    // If gray, leave H  S at zero
		HSV.y = delta / HSV.z;
		vec3 delRGB = (HSV.zzz - RGB + 3.0 * delta) / (6.0 * delta);
		if (RGB.x == HSV.z) HSV.x = delRGB.z - delRGB.y;
		else if (RGB.y == HSV.z) HSV.x = (1.0 / 3.0) + delRGB.x - delRGB.z;
		else if (RGB.z == HSV.z) HSV.x = (2.0 / 3.0) + delRGB.y - delRGB.x;
	}
	return (HSV);
}

vec3 HSV_To_RGB(inout vec3 HSV) 
{
	vec3 RGB = vec3(HSV.z);

	float var_h = HSV.x * 6.0;
	float var_i = floor(var_h);
	float var_1 = HSV.z * (1.0 - HSV.y);
	float var_2 = HSV.z * (1.0 - HSV.y * (var_h - var_i));
	float var_3 = HSV.z * (1.0 - HSV.y * (1.0 - (var_h - var_i)));
	if (var_i == 0.0) { RGB = vec3(HSV.z, var_3, var_1); }
	else if (var_i == 1.0) { RGB = vec3(var_2, HSV.z, var_1); }
	else if (var_i == 2.0) { RGB = vec3(var_1, HSV.z, var_3); }
	else if (var_i == 3.0) { RGB = vec3(var_1, var_2, HSV.z); }
	else if (var_i == 4.0) { RGB = vec3(var_3, var_1, HSV.z); }
	else { RGB = vec3(HSV.z, var_1, var_2); }

	return (RGB);
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

vec3 UnpackScaleNormal(vec4 packednormal, float bumpScale)
{
    //TODO: this is ripped straight from unity, what the fuck does UNITY_NO_DXT5nm do
    #if defined(UNITY_NO_DXT5nm)
        half3 normal = packednormal.xyz * 2 - 1;
        #if (SHADER_TARGET >= 30)
            // SM2.0: instruction count limitation
            // SM2.0: normal scaler is not supported
            normal.xy *= bumpScale;
        #endif
        return normal;
    #else
        packednormal.x *= packednormal.w;

        vec3 normal;
        normal.xy = (packednormal.xy * 2.0 - 1.0);
        normal.xy *= bumpScale;
        normal.z = sqrt(1.0 - clamp(dot(normal.xy, normal.xy), 0.0, 1.0));
        return normal;
    #endif
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