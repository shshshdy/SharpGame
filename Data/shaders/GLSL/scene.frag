#version 450

#include "UniformsPS.glsl"


#define ambient 0.3

#include "Shadow.glsl"

layout (set = 2, binding = 0) uniform sampler2D DiffMap;

layout (location = 0) in vec3 inNormal;
layout (location = 1) in vec4 inColor;
layout (location = 2) in vec3 inViewPos;
layout (location = 3) in vec3 inPos;
layout (location = 4) in vec2 inUV;

layout (constant_id = 0) const int enablePCF = 1;

layout (location = 0) out vec4 outFragColor;

#define SHADOW_MAP_CASCADE_COUNT 4

void main() 
{	
	vec4 color = texture(DiffMap, inUV);
	if (color.a < 0.5) {
		discard;
	}

	// Get cascade index for the current fragment's view position
	uint cascadeIndex = 0;
	for(uint i = 0; i < SHADOW_MAP_CASCADE_COUNT - 1; ++i) {
		if(inViewPos.z > cascadeSplits[i]) {	
			cascadeIndex = i + 1;
            //break;
		}
	}

	// Depth compare for shadowing
	vec4 shadowCoord = (biasMat * LightMatrices[cascadeIndex]) * vec4(inPos, 1.0);	

	float shadow = 0;
    
	if (enablePCF == 1) {
		shadow = filterPCF(shadowCoord / shadowCoord.w, cascadeIndex);
	} else {
		shadow = textureProj(shadowCoord / shadowCoord.w, vec2(0.0), cascadeIndex);
	}

	// Directional light
	vec3 N = normalize(inNormal);
	vec3 L = normalize(-SunlightDir);
	vec3 H = normalize(L + inViewPos);
	float diffuse = max(dot(N, L), ambient);
	vec3 lightColor = vec3(1.0);
	outFragColor.rgb = max(lightColor * (diffuse * color.rgb), vec3(0.0));
	outFragColor.rgb *= shadow;
	outFragColor.a = color.a;
    /*
	// Color cascades (if enabled)
	//if (ubo.colorCascades == 1) {
		switch(cascadeIndex) {
			case 0 : 
				outFragColor.rgb *= vec3(1.0f, 0.25f, 0.25f);
				break;
			case 1 : 
				outFragColor.rgb *= vec3(0.25f, 1.0f, 0.25f);
				break;
			case 2 : 
				outFragColor.rgb *= vec3(0.25f, 0.25f, 1.0f);
				break;
			case 3 : 
				outFragColor.rgb *= vec3(1.0f, 1.0f, 0.25f);
				break;
		}
	//}*/
}
