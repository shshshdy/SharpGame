#version 450 core

#include "GridCoord.glsl"

layout(set = 1, binding = 1, r8ui) uniform uimageBuffer grid_flags;

#ifdef ALPHA_TEST
layout(set = 2, binding = 0) uniform sampler2D DiffMap;
#endif
layout(set = 3, binding = 0) uniform sampler2D NormalMap;
layout(set = 4, binding = 0) uniform sampler2D SpecMap;


//layout(early_fragment_tests) in;
layout(location = 0) in vec4 inWorldPos;

#ifdef ALPHA_TEST
layout(location = 1) in vec2 inUV;
#endif
layout (location = 2) in mat3 inNormal;

layout(location = 0) out vec4 outAlbedoSpec;
layout(location = 1) out vec4 outNormalRoughness;
//layout(location = 2) out vec4 outDepth;

vec3 DecodeNormal(vec4 normalInput)
{
	return normalize(normalInput.rgb * 2.0 - 1.0);
}

float linearDepth(float depth)
{
	float z = depth * 2.0f - 1.0f; 
	return (2.0f * ubo_in.cam_near * ubo_in.cam_far) / (ubo_in.cam_far + ubo_in.cam_near - z * (ubo_in.cam_far - ubo_in.cam_near));	
}

void main ()
{
	vec4 albedo = vec4(1);
#ifdef ALPHA_TEST
    albedo = texture(DiffMap, inUV);
    if (albedo.a < 0.5) {
        discard;
    }
#endif

	//float specular = texture(SpecMap, inUV).r;

	outAlbedoSpec = vec4(albedo.rgb, 1);

	vec3 N = normalize(inNormal * DecodeNormal(texture(NormalMap, inUV)));
	outNormalRoughness = vec4((N + 1.0f) * 0.5f, 1);

	float z = linearDepth(gl_FragCoord.z);

    //outDepth = vec4(inWorldPos.xyz, z);

    vec4 viewPos = ubo_in.view*inWorldPos;

    int grid_idx = ViewPosToGridIdx(gl_FragCoord.xy, viewPos.z);
    imageStore(grid_flags, int(grid_idx), uvec4(1, 0, 0, 0));
}
