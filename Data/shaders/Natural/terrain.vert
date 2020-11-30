#version 450
#include "Common.glsl"
layout (location = 0) in vec3 inPos;
layout (location = 1) in vec2 inUV;
layout (location = 2) in vec3 inNormal;

layout (location = 0) out vec3 outNormal;
layout (location = 1) out vec2 outUV;

void main(void)
{
	gl_Position =  ViewProj* vec4(inPos.xyz, 1.0);
	outUV = inUV;
	outNormal = inNormal;
}