#version 330

in float tValue;

uniform float tMin;
uniform float tMax;

out vec4 FragColor;

void main() {
    float res = floor((tValue - tMin) * 255.0 / (tMax - tMin));
    //float res = tValue;
    FragColor = vec4(res/255.0, res/255.0, res/255.0, 1.0);
}