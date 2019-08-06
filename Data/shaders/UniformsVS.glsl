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
/*
layout (constant_id = 0) const int MATRICES_COUNT = 1;

layout (binding = 0) uniform ObjectVS
{
    mat4 Model[MATRICES_COUNT];
};*/