#version 450

layout(location = 0) in vec3 inPos;
#ifdef ALPHA_TEST
layout(location = 1) in vec2 inUV;
#endif

layout(location = 0) out vec2 outUV;

out gl_PerVertex{
    vec4 gl_Position;
};

void main()
{
#ifdef ALPHA_TEST
    outUV = inUV;
#endif
    vec4 pos = Model * vec4(inPos, 1);
    gl_Position = ViewProj * pos;
}
