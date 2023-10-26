#version 330 core

in vec3 aPosition;

in vec2 aTexCoord;

out vec2 texCoord;

uniform mat4 aProjection;

uniform mat4 aView;

uniform mat4 aModel;

void main()
{
    gl_Position = vec4(aPosition, 1.0) * aModel * aView * aProjection;

    //gl_Position = vec4(aPosition, 1.0);

    texCoord = aTexCoord;
}