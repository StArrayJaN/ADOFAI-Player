#version 330 core

uniform sampler2D in_fontTexture;
uniform float time;
uniform int gradientMode; // 0=horizontal, 1=vertical, 2=diagonal, 3=rainbow wave

in vec4 color;
in vec2 texCoord;
in vec2 screenPos;

out vec4 outputColor;

// HSV to RGB conversion
vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main()
{
    vec4 texColor = texture(in_fontTexture, texCoord);
    
    // Check if this is text by looking at texture alpha
    // Text has alpha from the font texture, UI elements typically have alpha = 1.0 in texture
    if (texColor.a > 0.01 && texColor.a < 0.99) {
        // This is likely text, apply gradient
        vec3 gradientColor;
        
        if (gradientMode == 0) {
            // Horizontal gradient
            float t = fract(screenPos.x * 0.002 + time * 0.2);
            gradientColor = hsv2rgb(vec3(t, 0.8, 1.0));
        }
        else if (gradientMode == 1) {
            // Vertical gradient
            float t = fract(screenPos.y * 0.002 + time * 0.2);
            gradientColor = hsv2rgb(vec3(t, 0.8, 1.0));
        }
        else if (gradientMode == 2) {
            // Diagonal gradient
            float t = fract((screenPos.x + screenPos.y) * 0.001 + time * 0.2);
            gradientColor = hsv2rgb(vec3(t, 0.8, 1.0));
        }
        else {
            // Rainbow wave
            float t = fract(screenPos.x * 0.003 + sin(screenPos.y * 0.01 + time * 2.0) * 0.1 + time * 0.3);
            gradientColor = hsv2rgb(vec3(t, 0.9, 1.0));
        }
        
        // Apply gradient to text, preserve alpha from texture
        outputColor = vec4(gradientColor * color.rgb, color.a * texColor.a);
    }
    else {
        // For UI elements and solid areas, use original color
        outputColor = color * texColor;
    }
}
