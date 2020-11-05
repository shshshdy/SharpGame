
struct CameraData
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

struct ObjectData
{
    mat4 Model;	
    vec4 UOffset;
    vec4 VOffset;
    vec4 UOffset1;
    vec4 VOffset1;
};
