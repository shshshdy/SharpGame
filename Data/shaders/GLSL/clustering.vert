#version 450 core
			
#include "UniformsVS.glsl"

layout (location= 0) in vec3 inPos;
//#ifdef ALPHA_TEST
layout(location = 1) in vec2 inUV;
//#endif

out gl_PerVertex
{
    vec4 gl_Position;
};

layout (location= 0) out vec4 outViewPos;

#ifdef ALPHA_TEST
layout(location = 1) out vec2 outUV;
#endif

void main(void)
{
    vec4 worldPos = Model * vec4(inPos, 1.f);

	outViewPos = View * worldPos;

#ifdef ALPHA_TEST
    outUV = inUV;
#endif

    gl_Position = ViewProj * worldPos;
}
