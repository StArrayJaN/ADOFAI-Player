#version 330 core

in vec4 vertexColor;
in vec2 vertexTexCoord;

uniform sampler2D uMainTex;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(uMainTex, vertexTexCoord);
    vec4 finalColor = vertexColor * texColor;

    FragColor = finalColor;
}
