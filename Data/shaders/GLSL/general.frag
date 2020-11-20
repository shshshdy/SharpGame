#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

#include "UniformsPS.glsl"		
#include "Lighting.glsl"
#include "Shadow.glsl"

layout(set = 2, binding = 0) uniform sampler2D DiffMap;
layout(set = 2, binding = 1) uniform sampler2D NormalMap;
layout(set = 2, binding = 2) uniform sampler2D SpecMap;
#ifdef ALPHA_MAP
layout(set = 2, binding = 3) uniform sampler2D AlphaMap;
#endif
layout (location = 0) in vec4 inWorldPos;
layout (location = 1) in vec2 inUV;
layout (location = 2) in vec3 inViewPos;
layout (location = 3) in mat3 inNormal;

layout (location = 0) out vec4 outFragColor;

void main() 
{
	vec4 diffColor = texture(DiffMap, inUV);
#ifdef ALPHA_TEST
    #ifdef ALPHA_MAP
    vec4 alphaMap = texture(AlphaMap, inUV);
    if(alphaMap.r < 0.5) {
        discard;
    }
    #else
    if(diffColor.a < 0.5) {
        discard;
    }
    #endif
#endif

	vec3 N = normalize(inNormal * DecodeNormal(texture(NormalMap, inUV)));
	//vec3 N = normalize(inNormal[2]);
	vec3 L = -SunlightDir;
    uint cascadeIndex = GetCascadeIndex(inViewPos.z);
	// Depth compare for shadowing
	vec4 shadowCoord = GetShadowPos(cascadeIndex, inWorldPos.xyz);	

	float shadow = 1;

    #ifdef SHADOW
    shadow = filterPCF(shadowCoord / shadowCoord.w, cascadeIndex);
    #endif

	float NDotL = max(dot(N, L), 0.0);
    vec3 viewVec = CameraPos.xyz - inWorldPos.xyz;
	vec3 diffuse = diffColor.rgb * NDotL * shadow;
    //outFragColor = vec4(NDotL);return;
	vec3 specular = texture(SpecMap, inUV).xyz * BlinnPhong(N, viewVec, L, 16.0);
	outFragColor = vec4(diffColor.rgb * AmbientColor.xyz + diffuse + specular, diffColor.a);
}
