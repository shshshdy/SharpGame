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

layout (constant_id = 0) const int MATRICES_COUNT = 64;

layout (binding = 1) uniform ObjectVS
{
    mat4 Model;	
    vec4 UOffset;
    vec4 VOffset;
};

layout (binding = 1) uniform SkinVS
{
    mat4 SkinMatrices[MATRICES_COUNT];
};