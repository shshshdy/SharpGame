#version 450 core
			
#include "UniformsVS.glsl"

layout (location= 0) in vec3 pos_in;
#ifdef ALPHA_TEST
layout(location = 1) in vec2 inUV;
#endif

out gl_PerVertex
{
    vec4 gl_Position;
};

layout (location= 0) out vec4 world_pos_out;

#ifdef ALPHA_TEST
layout(location = 1) out vec2 outUV;
#endif

void main(void)
{
    world_pos_out = Model * vec4(pos_in, 1.f);
	
#ifdef ALPHA_TEST
    outUV = inUV;
#endif

    gl_Position = ViewProj * world_pos_out;
}
