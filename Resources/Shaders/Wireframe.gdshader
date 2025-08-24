shader_type spatial;
render_mode unshaded; // Somewhat ironically, we cannot use the "wireframe" render_mode here bc we need fill

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteCullMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform float _Thickness = 1.0; // 0.001 in the inspector?

uniform vec4 _LineColor : source_color = vec4(0, 0, 0, 1);
uniform vec4 _FillColor : source_color = vec4(0, 0, 0, 0);
// uniform vec4 _InnerLineColor : source_color = vec4(0, 0, 0, 0.73);
// uniform vec4 _InnerFillColor : source_color = vec4(0, 0, 0, 0);

uniform vec4 _LineFarColor : source_color = vec4(1, 1, 1, 1);
uniform vec4 _FillFarColor : source_color = vec4(1, 1, 1, 0);
// uniform vec4 _InnerLineFarColor : source_color = vec4(1, 1, 1, 0.73);
// uniform vec4 _InnerFillFarColor : source_color = vec4(1, 1, 1, 0);

uniform float _Exp = 1;

varying vec3 barycentric;
varying vec3 world_position;
varying vec3 world_normal;

//#define _SCREENSPACE // I don't know if we use this here...
//#define _FRESNEL

ColorTexture2DWhite(MainTex)
STData(MainTex)

float line_interpolation(vec3 bary) {
    // Calculate distance to edges
    vec3 d = fwidth(bary);
    vec3 smoothed = smoothstep(vec3(0.0), d * _Thickness, bary);
    return min(smoothed.x, min(smoothed.y, smoothed.z));
}

void vertex() {
    world_position = VERTEX;
    world_normal = NORMAL;
    
    // Simple barycentric coordinate assignment based on vertex index
    // This is a workaround since Godot doesn't have direct geometry shader support
    if (VERTEX_ID % 3 == 0) {
        barycentric = vec3(1.0, 0.0, 0.0);
    } else if (VERTEX_ID % 3 == 1) {
        barycentric = vec3(0.0, 1.0, 0.0);
    } else {
        barycentric = vec3(0.0, 0.0, 1.0);
    }
}

void fragment() {
    // Calculate line interpolation factor
    float line_factor = 1.0 - line_interpolation(barycentric);
    
    vec4 final_fill_color = _FillColor;
    vec4 final_line_color = _LineColor;
    
    #if defined(_FRESNEL)
        vec3 view_dir = normalize(CAMERA_POSITION_WORLD - world_position);
        float fresnel = pow(1.0 - abs(dot(world_normal, view_dir)), _Exp);
        
        final_fill_color = mix(_FillColor, _FillFarColor, fresnel);
        final_line_color = mix(_LineColor, _LineFarColor, fresnel);
    #endif
    
    // Mix between fill and line colors based on line factor
    vec4 base_color = mix(final_fill_color, final_line_color, line_factor);
    
    vec4 texture_color = SampleSTTexture(MainTex, UV);
    vec4 final_color = base_color * texture_color;
    
    ALBEDO = final_color.rgb;
	ALPHA = final_color.a; 
	// SetAlpha(final_color.a);
}