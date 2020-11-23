#version 450

#include "UniformsVS.glsl"

layout (location = 0) in vec3 inPos;
layout (location = 1) in vec2 inUV;
layout (location = 2) in vec3 inNormal;

layout(push_constant) uniform PushConsts{
	float lodBias;
};

layout (location = 0) out vec2 outUV;
layout (location = 1) out float outLodBias;
layout (location = 2) out vec3 outNormal;
layout (location = 3) out vec3 outViewVec;
layout (location = 4) out vec3 outLightVec;

out gl_PerVertex 
{
	vec4 gl_Position;   
};

void main() 
{
	outUV = inUV;
	outLodBias = lodBias;
	outNormal = inNormal;

	vec4 worldPos = Model * vec4(inPos, 1.0);

	gl_Position = ViewProj * worldPos;

	vec3 lightPos = vec3(0.0, 50.0f, 0.0f);
	outLightVec = lightPos - inPos.xyz;
	outViewVec = CameraPos.xyz - worldPos.xyz;		
}
