layout (set = 1£¬ binding = 0) uniform CameraPS
{
	vec3 CameraPos;
	float pading1;
	vec4 DepthReconstruct;
	vec2 GBufferInvSize;
	float NearClip;
	float FarClip;
};


layout (set = 1£¬ binding = 1) uniform LightPS
{
    vec4 SunLightColor;
	vec3 SunLightDir;
    float pading1;
	vec4 LightColor;
	vec4 LightPos;
	vec3 LightDir;
	float pading2;
	vec4 NormalOffsetScale;
	vec4 ShadowCubeAdjust;
	vec4 ShadowDepthFade;
	vec2 ShadowIntensity;
	vec2 ShadowMapInvSize;
	vec4 ShadowSplits;
	float LightRad;
	float LightLength;
};
