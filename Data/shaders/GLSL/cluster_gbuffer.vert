#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable
			
#include "UniformsVS.glsl"

#define TBN

layout(location = 0) in vec3 inPos;
layout(location = 1) in vec2 inUV;
layout(location = 2) in vec3 inNormal;

#ifdef TBN
layout(location = 3) in vec3 inTangent;
layout(location = 4) in vec3 inBitangent;
#else
layout(location = 3) in vec4 inTangent;
#endif

layout(location = 0) out vec4 outWorldPos;
layout(location = 1) out vec2 outUV;
layout(location = 2) out mat3 outNormal;

out gl_PerVertex
{
	vec4 gl_Position;
};

void main() 
{
	vec4 worldPos = Model * vec4(inPos, 1.0);

	gl_Position = ViewProj * worldPos;

	outWorldPos = /*View **/ worldPos;
	outUV = inUV;

	#ifdef TBN
	outNormal = mat3(Model) * mat3(inTangent, inBitangent, inNormal);
	#else
    vec3 bitangent = cross(inNormal, inTangent.xyz)*inTangent.w;
	outNormal = mat3(Model) * mat3(inTangent, bitangent, inNormal);
	#endif
}
