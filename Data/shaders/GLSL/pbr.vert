#version 450

#include "Common.glsl"

layout (location = 0) in vec3 inPos;
layout (location = 1) in vec3 inNormal;

layout (location = 0) out vec3 outWorldPos;
layout (location = 1) out vec3 outNormal;

void main() 
{
	outWorldPos = vec3(Model * vec4(inPos, 1.0));
	outNormal = mat3(Model) * inNormal;
	gl_Position =  ViewProj * vec4(outWorldPos, 1.0);
}
