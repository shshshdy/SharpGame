
layout (binding = 0) uniform GlobalUniform
{
    mat4 View;
    mat4 InvView;
    mat4 Proj;
    mat4 InvProj;
    mat4 ViewProj;
    mat4 InvViewProj;
	vec3 CameraPos;
	float NearClip;
	vec3 CameraDir;
	float FarClip;
	vec2 ViewportSize;
    float Time;

};
