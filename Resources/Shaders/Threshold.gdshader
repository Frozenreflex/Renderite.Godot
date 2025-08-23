shader_type spatial;
render_mode unshaded;

// TODO Prevent this from sampling itself

// Define shader parameters
uniform float Threshold : hint_range(0.0, 1.0) = 0.5;
uniform float Transition : hint_range(0.0, 1.0) = 0.01;
uniform sampler2D ScreenTexture : hint_screen_texture, filter_linear_mipmap;

void fragment() {   
    // Sample the screen texture
    vec4 color = texture(ScreenTexture, SCREEN_UV);
    
    // Apply the threshold effect
    color.rgb -= Threshold;
    color.rgb /= Transition;
    color.rgb += Transition * 0.5;
    
    // Saturate the color to clamp it between 0 and 1
    color.rgb = clamp(color.rgb, 0.0, 1.0);
    
    // Output the final color
    ALBEDO = color.rgb;
    ALPHA = 0.2f;
}