#version 450 core

#include "GridCoord.glsl"
#include "cluster_lighting.h"

#define AMBIENT_GLOBAL 0.2f

layout(set = 3, binding = 0) uniform sampler2D DiffMap;
layout(set = 3, binding = 1) uniform sampler2D NormalMap;
layout(set = 3, binding = 2) uniform sampler2D SpecMap;
layout(set = 3, binding = 3) uniform sampler2D AlphaMap;

layout(location = 0) in vec4 inWorldPos;
layout(location = 1) in vec2 inUV;
layout(location = 2) in mat3 inNormal;

layout (location = 0) out vec4 frag_color;


void main()
{
    vec3 ambient = vec3(AMBIENT_GLOBAL);
    vec4 diffColor = texture(DiffMap, inUV);
    vec4 specColor = texture(SpecMap, inUV);
    vec3 normal_sample = texture(NormalMap, inUV).rgb * 2.f - 1.f;

    vec3 N = inNormal * normal_sample;

    vec3 color = ClusterLighting(inWorldPos.xyz, N, diffColor.rgb, specColor.rgb, specColor.a*255);

    frag_color = vec4(color + ambient * diffColor.rgb, diffColor.a);
}
