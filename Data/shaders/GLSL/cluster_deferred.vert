#version 450

layout (binding = 0) uniform CameraVS
{
    mat4 View;
    mat4 ViewInv;
    mat4 ViewProj;
	vec3 CameraPos;
	float NearClip;
	vec3 FrustumSize;
	float FarClip;	
    vec4 DepthMode;
    vec4 GBufferOffsets;
};

layout(location = 0) in vec3 inPosition;

layout(location = 0) out vec2 outUV;
layout(location = 1) out vec3 outViewRay;

out gl_PerVertex
{
	vec4 gl_Position;
};

void main() 
{
	outUV = (inPosition.xy + 1.0f) / 2;
	gl_Position = vec4(inPosition, 1.0f);

    vec4 position = inverse(ViewProj) * vec4(inPosition, 1.0);
    position /= position.w;
    outViewRay = position.xyz - CameraPos;
}
