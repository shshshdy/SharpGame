#version 450

layout (location = 0) in vec3 in_Position;
layout (location = 1) in vec3 in_Normal;
layout (location = 2) in vec2 in_TexCoord;

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

layout (location = 0) out vec2 out_TexCoord;

out gl_PerVertex
{
    vec4 gl_Position;
};

void main() {
    out_TexCoord = in_TexCoord;
    gl_Position = ViewProj /** World*/ * vec4(in_Position.xyz, 1.0);
}
