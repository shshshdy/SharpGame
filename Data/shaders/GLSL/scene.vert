#version 450
#include "UniformsVS.glsl"

layout (location = 0) in vec3 inPos;
layout (location = 1) in vec3 inNormal;
layout (location = 2) in vec2 inUV;

layout (location = 0) out vec3 outNormal;
layout (location = 2) out vec3 outViewPos;
layout (location = 3) out vec3 outPos;
layout (location = 4) out vec2 outUV;

layout(push_constant) uniform PushConsts {
	vec4 position;
	uint cascadeIndex;
} pushConsts;

out gl_PerVertex {
	vec4 gl_Position;   
};

void main() 
{
	outNormal = inNormal;
	outUV = inUV;

	vec4 pos = Model * vec4(inPos, 1.0);
	outPos = pos.xyz;
	outViewPos = (View * pos).xyz;
	gl_Position = ViewProj * pos;
}

