#version 330 core
in vec2 v_texCoord;
in vec4 v_color;

uniform sampler2D uMainTex;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(uMainTex, v_texCoord);
    FragColor = v_color * texColor;
}