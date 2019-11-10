
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

layout(constant_id = 0) const int TransformMode = 0;
layout(constant_id = 1) const int MATRICES_COUNT = 64;
layout(constant_id = 2) const int ShadingMode = 1;
layout(constant_id = 3) const int LightCount = 2;
layout(constant_id = 4) const int HasNormalMap = 0;

layout (binding = 1) uniform ObjectVS_dynamic
{
    mat4 Model;	
    vec4 UOffset;
    vec4 VOffset;
    vec4 UOffset1;
    vec4 VOffset1;
};

layout (binding = 1) uniform SkinVS_dynamic
{
    mat4 SkinMatrices[MATRICES_COUNT];
};
