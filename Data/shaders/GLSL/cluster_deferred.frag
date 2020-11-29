#version 450


#include "GridCoord.glsl"
#include "ClusterLighting.glsl"

layout(set = 3, binding = 0) uniform sampler2D samplerAlbedo;
layout(set = 3, binding = 1) uniform sampler2D samplerNormal;
layout(set = 3, binding = 2) uniform sampler2D samplerDepth;

layout(location = 0) in vec2 inUV;

layout(location = 0) out vec4 outFragcolor;

//precision highp float;

/*
vec3 ReconstructWSPosFromDepth(vec2 uv, float depth)
{
	vec4 pos = vec4(uv * 2.0 - 1.0, depth, 1.0f);
	vec4 posVS = uboConstant.invProj * pos;
	vec3 posNDC = posVS.xyz / posVS.w;
	return (uboConstant.invView * vec4(posNDC, 1)).xyz;
}*/

void main() 
{
	// Get G-Buffer values
	vec4 albedo = texture(samplerAlbedo, inUV);
	vec4 normal = texture(samplerNormal, inUV);

#ifdef DEPTH_RECONSTRUCT

	float depth = texture(samplerDepth, inUV).r;
	vec4 clip = vec4(inUV * 2.0 - 1.0, depth, 1.0);
	highp vec4 world_w = ubo_in.inv_view_proj * clip;
	highp vec3 worldPos = world_w.xyz / world_w.w;

#else
	vec4 pos = texture(samplerDepth, inUV);
    vec3 worldPos = pos.xyz;// ReconstructPositionFromDepth(depth);
#endif


    vec3 specColor = vec3(albedo.a);

    vec3 N = normal.rgb * 2.0f - 1.0f;

    vec3 color = ClusterLighting(worldPos, N, albedo.rgb, specColor.rgb, 5);

    outFragcolor = vec4(color + 0.2f * albedo.rgb, 1);

}
