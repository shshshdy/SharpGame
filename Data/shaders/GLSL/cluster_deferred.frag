#version 450

#include "GridCoord.glsl"

layout(set = 1, binding = 1, rgba32f) uniform readonly imageBuffer light_pos_ranges;
layout(set = 1, binding = 2, rgba8) uniform readonly imageBuffer light_colors;

layout(set = 2, binding = 0, r8ui) uniform uimageBuffer grid_flags;
layout(set = 2, binding = 1, r32ui) uniform uimageBuffer light_bounds;
layout(set = 2, binding = 2, r32ui) uniform uimageBuffer grid_light_counts;
layout(set = 2, binding = 3, r32ui) uniform uimageBuffer grid_light_count_total;
layout(set = 2, binding = 4, r32ui) uniform uimageBuffer grid_light_count_offsets;
layout(set = 2, binding = 5, r32ui) uniform uimageBuffer light_list;
layout(set = 2, binding = 6, r32ui) uniform uimageBuffer grid_light_counts_compare;

layout(set = 3, binding = 0) uniform sampler2D samplerAlbedo;
layout(set = 3, binding = 1) uniform sampler2D samplerNormal;
layout(set = 3, binding = 2) uniform sampler2D samplerDepth;

layout(location = 0) in vec2 inUV;
layout(location = 1) in vec3 inViewRay;
layout(location = 0) out vec4 outFragcolor;

vec3 ReconstructPositionFromDepth(float depth)
{
  depth = (ubo_in.cam_near * ubo_in.cam_far) / (ubo_in.cam_far + depth * (ubo_in.cam_near - ubo_in.cam_far));
  const vec3 viewRay = normalize(inViewRay);
  const float viewZDist = dot(ubo_in.cam_forward, viewRay);
  return inEyePosition + viewRay * (depth / viewZDist);
}

void main() 
{
	// Get G-Buffer values
	vec4 albedo = texture(samplerAlbedo, inUV);
	vec4 normal = texture(samplerNormal, inUV);
	float depth = texture(samplerDepth, inUV).r;
    vec3 worldPos = ReconstructPositionFromDepth(depth);
		
	// Ambient part
	vec3 fragcolor  = albedo.rgb;
	
	outFragcolor = vec4(albedo.xyz, 1.0);	
	//outFragcolor = vec4(depth, depth, depth, 1.0);	
}
