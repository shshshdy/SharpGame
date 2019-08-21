
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

layout(set=2, binding=0) uniform samplerCube prefilteredMap;
layout(set=2, binding=1) uniform samplerCube samplerIrradiance;
layout(set=2, binding=2) uniform sampler2D samplerBRDFLUT;

layout(set=3, binding=0) uniform sampler2D albedoMap;
layout(set=4, binding=0) uniform sampler2D normalMap;
layout(set=5, binding=0) uniform sampler2D metallicMap;
layout(set=6, binding=0) uniform sampler2D roughnessMap;
