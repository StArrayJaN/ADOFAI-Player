#version 330 core

in vec4 vColor;
in vec2 vTexCoord;

uniform sampler2D uTexture;
uniform int uUseTexture;

out vec4 FragColor;

void main()
{
    if (uUseTexture != 0)
        FragColor = texture(uTexture, vTexCoord) * vColor;
    else
        FragColor = vColor;
}
