#version 450

#include "Common.glsl"

// Vertex attributes
layout (location = 0) in vec4 inPos;
layout (location = 1) in vec2 inUV;

layout (location = 0) out vec2 outUV;

out gl_PerVertex
{
	vec4 gl_Position;
};

void main() 
{
	outUV = vec2(inUV.s, 1.0-inUV.t);
	// Skysphere always at center, only use rotation part of modelview matrix
	gl_Position = ViewProj * vec4(inPos.xyz + CameraPos, 1.0);
}
