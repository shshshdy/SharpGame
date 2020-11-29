

#define SHADOW_MAP_CASCADE_COUNT 4

layout (set = 1, binding = 0) uniform LightPS
{
    vec4 AmbientColor;
    vec4 SunlightColor;
	vec3 SunlightDir;
	float LightPS_pading1;
	vec4 cascadeSplits;
	mat4 LightMatrices[SHADOW_MAP_CASCADE_COUNT];
	vec4 LightColor[8];
	vec4 LightVec[8];
};

layout (set = 1, binding = 1) uniform sampler2DArray ShadowMap;
