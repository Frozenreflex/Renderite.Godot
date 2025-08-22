shader_type spatial;
render_mode unshaded;

#include "res://Resources/ShaderIncs/ResoniteBlendMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteZTestMode.gdshaderinc"
#include "res://Resources/ShaderIncs/ResoniteIncludes.gdshaderinc"

uniform vec4 _OuterColor : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 _InnerColor : source_color = vec4(1.0, 1.0, 1.0, 1.0);

uniform float _OffsetFactor : hint_range(0.0, 1.0) = 1.0;
uniform float _OffsetUnits : hint_range(0.0, 1.0) = 1.0;
uniform vec4 _Rect = vec4(0.25, 0.25, 0.75, 0.75);
uniform vec4 _ClipRect = vec4(0, 0, 1, 1);
varying vec2 Vert;

void vertex() {
    Vert = VERTEX.xy * vec2(-1,1);
}

void fragment() {
    DoRectClip(_ClipRect);
    ALBEDO.rgb = mix(_OuterColor, _InnerColor, Get2DClipping(UV, _Rect)).rgb;
}