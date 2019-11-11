#version 450

layout (set = 0, binding = 0) uniform sampler2D samplerAlbedo;
layout (set = 0, binding = 1) uniform sampler2D samplerNormal;
layout (set = 0, binding = 2) uniform sampler2D samplerDepth;

#include "GridCoord.glsl"

layout (location = 0) in vec2 inUV;

layout (location = 0) out vec4 outFragcolor;

void main() 
{
	// Get G-Buffer values
	vec4 albedo = texture(samplerAlbedo, inUV);
	vec4 normal = texture(samplerNormal, inUV);
	float depth = texture(samplerDepth, inUV).r;
		
	// Ambient part
	vec3 fragcolor  = albedo.rgb;
	
	outFragcolor = vec4(depth, depth, depth, 1.0);	
}
