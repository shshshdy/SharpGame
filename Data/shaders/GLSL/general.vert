#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable
			
#include "UniformsVS.glsl"

layout(location = 0) in vec3 inPos;
layout(location = 1) in vec2 inUV;
layout(location = 2) in vec3 inNormal;
layout(location = 3) in vec4 inTangent;

layout(location = 0) out vec4 outWorldPos;
layout(location = 1) out vec2 outUV;
layout(location = 2) out vec3 outViewPos;
layout(location = 3) out mat3 outNormal;

out gl_PerVertex
{
	vec4 gl_Position;
};

void main() 
{
	vec4 worldPos = Model * vec4(inPos, 1.0);

	gl_Position = ViewProj * worldPos;
	outWorldPos = worldPos;
	outUV = inUV;
    outViewPos = (View * worldPos).xyz;
    vec3 bitangent = cross(inTangent.xyz, inNormal) * inTangent.w;
	outNormal = mat3(Model) * mat3(inTangent, bitangent, inNormal);
}
