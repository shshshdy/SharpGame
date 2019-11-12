#version 450 core

#include "GridCoord.glsl"

#define AMBIENT_GLOBAL 0.2f

#include "cluster_lighting.h"

layout(set = 3, binding = 0) uniform sampler2D DiffMap;
layout(set = 4, binding = 0) uniform sampler2D NormalMap;
layout(set = 5, binding = 0) uniform sampler2D SpecMap;
layout(set = 6, binding = 0) uniform sampler2D AlphaMap;

layout(location = 0) in vec4 world_pos_in;
layout(location = 1) in vec2 uv_in;
layout(location = 2) in mat3 inNormal;

layout (location = 0) out vec4 frag_color;


void main()
{
    vec3 ambient = vec3(AMBIENT_GLOBAL);
    vec4 diffColor = texture(DiffMap, uv_in);
    vec4 specColor = texture(SpecMap, uv_in);
    vec3 normal_sample = texture(NormalMap, uv_in).rgb * 2.f - 1.f;

    vec3 N = inNormal * normal_sample;

    vec3 color = ClusterLighting(world_pos_in.xyz, N, diffColor.rgb, specColor.rgb, specColor.a*255);

    frag_color = vec4(color + ambient * diffColor.rgb, diffColor.a);
}
