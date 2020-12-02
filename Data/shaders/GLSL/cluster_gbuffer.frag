#version 450 core

#include "GridCoord.glsl"

layout(set = 1, binding = 1, r8ui) uniform uimageBuffer grid_flags;
layout(set = 2, binding = 0) uniform sampler2D DiffMap;
layout(set = 2, binding = 1) uniform sampler2D NormalMap;
layout(set = 2, binding = 2) uniform sampler2D SpecMap;


//layout(early_fragment_tests) in;
layout(location = 0) in vec4 inWorldPos;
layout(location = 1) in vec2 inUV;
layout (location = 2) in mat3 inNormal;

layout(location = 0) out vec4 outAlbedoSpec;
layout(location = 1) out vec4 outNormalRoughness;

vec3 DecodeNormal(vec4 normalInput)
{
	return normalize(normalInput.rgb * 2.0 - 1.0);
}

float LinearDepth(float depth)
{
	float z = depth * 2.0f - 1.0f; 
	return (2.0f * ubo_in.cam_near * ubo_in.cam_far) / (ubo_in.cam_far + ubo_in.cam_near - z * (ubo_in.cam_far - ubo_in.cam_near));	
}

void main ()
{
	vec4 albedo = texture(DiffMap, inUV);

#ifdef ALPHA_TEST
    if (albedo.a < 0.5) {
        discard;
    }
#endif

	//float specular = texture(SpecMap, inUV).r;

	outAlbedoSpec = vec4(albedo.rgb, 1);

	vec3 N = normalize(inNormal * DecodeNormal(texture(NormalMap, inUV)));
	outNormalRoughness = vec4((N + 1.0f) * 0.5f, 1);

	float z = LinearDepth(gl_FragCoord.z);

    vec4 viewPos = ubo_in.view * inWorldPos;

    int grid_idx = ViewPosToGridIdx(gl_FragCoord.xy, viewPos.z);
    imageStore(grid_flags, int(grid_idx), uvec4(1, 0, 0, 0));
}
