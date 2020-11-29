
struct CameraData
{
    mat4 View;
    mat4 ViewInv;
    mat4 ViewProj;
	vec3 CameraPos;
    float pading1;
    vec2 GBufferInvSize;
    float NearClip;
    float FarClip;
};

struct ObjectData
{
    mat4 Model;
    vec4 UOffset0;
    vec4 VOffset0;
    vec4 UOffset1;
    vec4 VOffset1;
};
