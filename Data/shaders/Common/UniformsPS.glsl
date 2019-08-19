
layout (set = 1, binding = 0) uniform CameraPS
{
	vec3 CameraPos;
	float pading1;
	vec4 DepthReconstruct;
	vec2 GBufferInvSize;
	float NearClip;
	float FarClip;
};

layout (set = 1, binding = 1) uniform LightPS
{
    vec4 AmbientColor;
    vec4 SunlightColor;
	vec3 SunlightDir;
	float LightPS_pading1;

	vec4 LightColor[8];
	vec4 LightVec[8];

};
