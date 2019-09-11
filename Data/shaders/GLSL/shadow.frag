
#version 450

layout(set = 1, binding = 0) uniform sampler2D DiffMap;
layout(location = 0) in vec2 inUV;

void main()
{
    float alpha = texture(DiffMap, inUV).a;
    if (alpha < 0.5) {
        discard;
    }
}
