
const mat4 biasMat = mat4( 
	0.5, 0.0, 0.0, 0.0,
	0.0, 0.5, 0.0, 0.0,
	0.0, 0.0, 1.0, 0.0,
	0.5, 0.5, 0.0, 1.0 
);

#define ambient 0.3

uint GetCascadeIndex(float viewZ)
{
    // Get cascade index for the current fragment's view position
	uint cascadeIndex = 0;
	for(uint i = 0; i < SHADOW_MAP_CASCADE_COUNT - 1; ++i) {
		if(viewZ > cascadeSplits[i]) {	
			cascadeIndex = i + 1;
		}
	}

    return cascadeIndex;
}

vec4 GetShadowPos(uint cascadeIndex, vec3 worldPos)
{
	// Depth compare for shadowing
	vec4 shadowCoord = (biasMat * LightMatrices[cascadeIndex]) * vec4(worldPos, 1.0);
    return shadowCoord;
}

float textureProj(vec4 shadowCoord, vec2 offset, uint cascadeIndex)
{
	float shadow = 1.0;
	float bias = 0.005;

	if ( shadowCoord.z > -1.0 && shadowCoord.z < 1.0 ) {
		float dist = texture(ShadowMap, vec3(shadowCoord.st + offset, cascadeIndex)).r;
		if (shadowCoord.w > 0 && dist < shadowCoord.z - bias) {
			shadow = ambient;
		}
	}
	return shadow;

}

float filterPCF(vec4 sc, uint cascadeIndex)
{
	ivec2 texDim = textureSize(ShadowMap, 0).xy;
	float scale = 0.75;
	float dx = scale * 1.0 / float(texDim.x);
	float dy = scale * 1.0 / float(texDim.y);

	float shadowFactor = 0.0;
	int count = 0;
    /*
	int range = 1;
	for (int x = -range; x <= range; x++) {
		for (int y = -range; y <= range; y++) {
			shadowFactor += textureProj(sc, vec2(dx*x, dy*y), cascadeIndex);
			count++;
		}
	}*/

    count = 4;
    shadowFactor = textureProj(sc, vec2(0, 0), cascadeIndex)
                   + textureProj(sc, vec2(dx, 0), cascadeIndex)
                   + textureProj(sc, vec2(0, dy), cascadeIndex)
                   + textureProj(sc, vec2(dx, dy), cascadeIndex);

	return shadowFactor / count;
}
