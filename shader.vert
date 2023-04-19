#version 330

layout (location = 0) in vec2 aPosition;
layout (location = 1) in float aValue;

out float tValue;

void main() {
    gl_Position = vec4(aPosition, 0.0, 1.0);
    tValue = aValue;
}