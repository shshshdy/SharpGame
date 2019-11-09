#version 450

layout (set = 0, binding = 0) uniform sampler2D samplerAlbedo;
layout (set = 0, binding = 1) uniform sampler2D samplerNormal;
//layout (set = 0, binding = 2) uniform sampler2D samplerposition;

//#include "GridCoord.glsl"


layout (location = 0) in vec2 inUV;

layout (location = 0) out vec4 outFragcolor;

void main() 
{
	//outFragcolor = vec4(1, 0, 0, 1.0);	

	// Get G-Buffer values
	vec4 albedo = texture(samplerAlbedo, inUV);
	vec3 normal = texture(samplerNormal, inUV).rgb;
	//vec3 fragPos = texture(samplerposition, inUV).rgb;
		
	// Ambient part
	vec3 fragcolor  = albedo.rgb;
	
	outFragcolor = vec4(fragcolor, 1.0);	
}