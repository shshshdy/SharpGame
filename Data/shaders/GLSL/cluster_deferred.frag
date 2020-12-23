#version 450

#include "Global.glsl"
#include "GridCoord.glsl"
#include "ClusterLighting.glsl"


#ifdef SUBPASS_INPUT

layout (input_attachment_index = 0, binding = 0) uniform subpassInput samplerAlbedo;
layout (input_attachment_index = 1, binding = 1) uniform subpassInput samplerNormal;
layout (input_attachment_index = 2, binding = 2) uniform subpassInput samplerDepth;

#else

layout(set = 3, binding = 0) uniform sampler2D samplerAlbedo;
layout(set = 3, binding = 1) uniform sampler2D samplerNormal;
layout(set = 3, binding = 2) uniform sampler2D samplerDepth;

#endif

layout(location = 0) in vec2 inUV;

layout(location = 0) out vec4 outFragcolor;

precision highp float;

void main() 
{
	// Get G-Buffer values
	
#ifdef SUBPASS_INPUT
	vec4 albedo = subpassLoad(samplerAlbedo);
	vec4 normal = subpassLoad(samplerNormal);
	float depth = subpassLoad(samplerDepth).r;
#else
	vec4 albedo = texture(samplerAlbedo, inUV);
	vec4 normal = texture(samplerNormal, inUV);
	float depth = texture(samplerDepth, inUV).r;
#endif

	vec4 clip = vec4(inUV * 2.0 - 1.0, depth, 1.0);
	highp vec4 world_w = ViewProjInv * clip;
	highp vec3 worldPos = world_w.xyz / world_w.w;

    vec3 specColor = vec3(albedo.a);
    vec3 N = normal.rgb * 2.0f - 1.0f;
    vec3 color = ClusterLighting(worldPos, N, albedo.rgb, specColor.rgb, 5);

    outFragcolor = vec4(color + 0.2f * albedo.rgb, 1);

}
