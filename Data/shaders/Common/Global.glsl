
layout (binding = 0) uniform GlobalUniform
{
    mat4 View;
    mat4 ViewInv;
    mat4 Proj;
    mat4 ProjInv;
    mat4 ViewProj;
    mat4 ViewProjInv;
	vec3 CameraPos;
	float NearClip;
	vec3 CameraDir;
	float FarClip;
	vec2 GBufferInvSize;
    float Time;

};
