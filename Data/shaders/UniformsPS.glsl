layout (binding = 1) uniform CameraPS
{
	vec3 CameraPos;
	float pading1;
	vec4 DepthReconstruct;
	vec2 GBufferInvSize;
	float NearClip;
	float FarClip;
};


layout (binding = 2) uniform LightPS
{
	vec4 LightColor;
	vec4 LightPos;
	vec3 LightDir;
	float pading1;
	vec4 NormalOffsetScale;
	vec4 ShadowCubeAdjust;
	vec4 ShadowDepthFade;
	vec2 ShadowIntensity;
	vec2 ShadowMapInvSize;
	vec4 ShadowSplits;
	float LightRad;
	float LightLength;
};
