#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

#include "UniformsPS.glsl"		
#include "Lighting.glsl"
#include "Shadow.glsl"

layout (set = 2, binding = 0) uniform sampler2D DiffMap;
layout(set = 3, binding = 0) uniform sampler2D NormalMap;
layout(set = 4, binding = 0) uniform sampler2D SpecMap;
layout(set = 5, binding = 0) uniform sampler2D EmissiveMap;

layout (location = 0) in vec4 inWorldPos;
layout (location = 1) in vec2 inUV;
layout (location = 2) in vec3 inViewPos;
layout (location = 3) in mat3 inNormal;

layout (location = 0) out vec4 outFragColor;

void main() 
{
	vec4 diffColor = texture(DiffMap, inUV);
    if(diffColor.a < 0.5) {
        discard;
    }

	//vec3 N = normalize(inNormal * DecodeNormal(texture(NormalMap, inUV)));
	vec3 N = normalize(inNormal[2]);
	vec3 L = -SunlightDir;
    
    uint cascadeIndex = GetCascadeIndex(inViewPos.z);
	// Depth compare for shadowing
	vec4 shadowCoord = GetShadowPos(cascadeIndex, inWorldPos.xyz);	

	float shadow = 1;

    #ifdef SHADOW
    shadow = filterPCF(shadowCoord / shadowCoord.w, cascadeIndex);
    #endif

    vec3 viewVec = CameraPos.xyz - inWorldPos.xyz;
	vec3 diffuse = diffColor.rgb * max(dot(N, L), 0.0)*shadow;
	vec3 specular = vec3(0.75) * BlinnPhong(N, viewVec, L, 16.0);
	outFragColor = vec4(diffColor.rgb * AmbientColor.xyz + diffuse + specular, 1.0);
}
