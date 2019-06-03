#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (location = 0) in vec3 inPos;
layout (location = 1) in vec3 inNormal;
layout (location = 2) in vec2 inUV;
layout (location = 3) in vec4 inColor;

layout(push_constant) uniform PushConsts {
	mat4 model;
};

layout (binding = 0) uniform CameraVS
{
    mat4 View;
    mat4 ViewInv;
    mat4 ViewProj;
	vec3 CameraPos;
	float NearClip;
	vec3 FrustumSize;
	float FarClip;
};


layout (set = 1, binding = 0) uniform UBO 
{
	vec4 lightPos;
};

layout (location = 0) out vec3 outNormal;
layout (location = 1) out vec4 outColor;
layout (location = 2) out vec2 outUV;
layout (location = 3) out vec3 outViewVec;
layout (location = 4) out vec3 outLightVec;

out gl_PerVertex
{
	vec4 gl_Position;
};

void main() 
{
	outNormal = inNormal;
	outColor = inColor;
	outUV = inUV;
	
	vec4 pos = model * vec4(inPos, 1.0);

	gl_Position = ViewProj * pos;
	
	outNormal = mat3(model) * inNormal;
	vec3 lPos = lightPos.xyz;
	outLightVec = lPos - pos.xyz;
	outViewVec = CameraPos.xyz-pos.xyz;		
}
