#version 330 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texCoord;

uniform mat4 uProjectionMatrix;
uniform mat4 uViewMatrix;
uniform mat4 uModelMatrix;
uniform vec4 uMainTex_ST;

out vec4 vertexColor;
out vec2 vertexTexCoord;

void main()
{
    gl_Position = uProjectionMatrix * uViewMatrix * uModelMatrix * vec4(position, 1.0);
    vertexColor = color;
    vertexTexCoord = texCoord * uMainTex_ST.xy + uMainTex_ST.zw;
}
