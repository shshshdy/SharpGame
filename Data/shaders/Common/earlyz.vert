#version 450

layout(location = 0) in vec3 inPos;
layout(location = 0) out vec2 outUV;

out gl_PerVertex{
    vec4 gl_Position;
};

void main()
{
    vec4 pos = Model * vec4(inPos, 1);
    gl_Position = ViewProj * pos;
}
