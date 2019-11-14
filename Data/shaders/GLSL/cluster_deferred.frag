#version 450

#include "GridCoord.glsl"
#include "cluster_lighting.h"

layout(set = 3, binding = 0) uniform sampler2D samplerAlbedo;
layout(set = 3, binding = 1) uniform sampler2D samplerNormal;
layout(set = 3, binding = 2) uniform sampler2D samplerDepth;

layout(location = 0) in vec2 inUV;
layout(location = 1) in vec3 iFarRay;
layout(location = 2) in vec3 iNearRay;

layout(location = 0) out vec4 outFragcolor;

//#define DEPTH_RECONSTRUCT

float ReconstructDepth(float hwDepth)
{
    return dot(vec2(hwDepth, ubo_in.depth_reconstruct.y / (hwDepth - ubo_in.depth_reconstruct.x)),
            ubo_in.depth_reconstruct.zw);
}

void main() 
{
	// Get G-Buffer values
	vec4 albedo = texture(samplerAlbedo, inUV);
	vec4 normal = texture(samplerNormal, inUV);

	vec4 pos = texture(samplerDepth, inUV);

#ifdef DEPTH_RECONSTRUCT

    float depth = /*pos.w;//*/ReconstructDepth(pos.w);
      
#ifdef ORTHO
    vec3 worldPos = lerp(iNearRay, iFarRay, depth);
#else
    vec3 worldPos = iFarRay * depth;
#endif
    worldPos += ubo_in.cam_pos;

#else
    vec3 worldPos = pos.xyz;// ReconstructPositionFromDepth(depth);
#endif


    vec3 specColor = vec3(albedo.a);

    vec3 N = normal.rgb * 2.0f - 1.0f;

    vec3 color = ClusterLighting(worldPos, N, albedo.rgb, specColor.rgb, 5);

    outFragcolor = vec4(color + 0.2f * albedo.rgb, 1);

}
