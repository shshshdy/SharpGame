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
layout(location = 1) out vec3 oFarRay;
layout(location = 2) out vec3 oNearRay;

//layout(location = 1) out vec3 outViewRay;

out gl_PerVertex
{
	vec4 gl_Position;
};

mat3 GetCameraRot()
{
    return mat3(ViewInv[0][0], ViewInv[0][1], ViewInv[0][2],
        ViewInv[1][0], ViewInv[1][1], ViewInv[1][2],
        ViewInv[2][0], ViewInv[2][1], ViewInv[2][2]);
}

vec3 GetFarRay(vec4 clipPos)
{
    vec3 viewRay = vec3(
        clipPos.x / clipPos.w * FrustumSize.x,
        clipPos.y / clipPos.w * FrustumSize.y,
        FrustumSize.z);

    return GetCameraRot() * viewRay;
}

vec3 GetNearRay(vec4 clipPos)
{
    vec3 viewRay = vec3(
        clipPos.x / clipPos.w * FrustumSize.x,
        clipPos.y / clipPos.w * FrustumSize.y,
        0.0);

    return (GetCameraRot() * viewRay) * DepthMode.x;
}

void main() 
{
	outUV = (inPosition.xy + 1.0f) / 2;
	gl_Position = vec4(inPosition, 1.0f);

    oFarRay = GetFarRay(gl_Position);
       
    oNearRay = GetNearRay(gl_Position);

    //vec4 position = inverse(ViewProj) * vec4(inPosition, 1.0);
    //position /= position.w;
    //outViewRay = position.xyz - CameraPos;
}
