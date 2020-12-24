#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

precision highp float;

#include "global.glsl"

layout (set = 1, binding = 0) uniform sampler2D samplerDepth;
layout (set = 1, binding = 1) uniform sampler2D samplerNormal;
layout (set = 1, binding = 2) uniform sampler2D ssaoNoise;

layout (constant_id = 0) const int SSAO_KERNEL_SIZE = 32;
layout (constant_id = 1) const float SSAO_RADIUS = 0.5;
layout (constant_id = 2) const float SSAO_POWER = 1.5;

layout (set = 1, binding = 3) uniform UBOSSAOKernel
{
	vec4 samples[SSAO_KERNEL_SIZE];
} uboSSAOKernel;


layout (location = 0) in vec2 inUV;

layout (location = 0) out float outFragColor;

float linearizeDepth(float depth, float near, float far)
{
    return near * far / (far + depth * (near - far));
}

float getDepth(sampler2D depth_sampler, vec2 offset)
{
	float depth = texture(depth_sampler, offset).r;
	return linearizeDepth(depth, NearClip, FarClip);
}


float nrand(vec2 uv, float dx, float dy)
{
    uv += vec2(dx, dy + /*_Time.x*/0.01f);
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453);
}

vec3 spherical_kernel(vec2 uv, float index)
{
    // Uniformaly distributed points
    // http://mathworld.wolfram.com/SpherePointPicking.html
    float u = nrand(uv, 0, index) * 2 - 1;
    float theta = nrand(uv, 1, index) * 3.1415926 * 2;
    float u2 = sqrt(1 - u * u);
    vec3 v = vec3(u2 * cos(theta), u2 * sin(theta), u);
    // Adjustment for distance distribution.
    float l = index / SSAO_KERNEL_SIZE;
    return v * mix(0.1, 1.0, l * l);
}

void main() 
{
	float depth = texture(samplerDepth, inUV).x; //outFragColor = depth; return;
	vec4  clip  = vec4(inUV * 2.0 - 1.0, depth, 1.0);
    highp vec4 viewPos = InvProj * clip;
    highp vec3 fragPos = viewPos.xyz / viewPos.w;

	vec3 normal = normalize(texture(samplerNormal, inUV).rgb * 2.0 - 1.0);
	// Get a random vector using a noise lookup
	ivec2 texDim = textureSize(samplerDepth, 0); 
	ivec2 noiseDim = textureSize(ssaoNoise, 0);
	const vec2 noiseUV = vec2(float(texDim.x)/float(noiseDim.x), float(texDim.y)/(noiseDim.y)) * inUV;  
	vec3 randomVec = texture(ssaoNoise, noiseUV).xyz * 2.0 - 1.0;
	
	// Create TBN matrix
	vec3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
	vec3 bitangent = cross(tangent, normal);
	mat3 TBN = mat3(tangent, bitangent, normal);

	// Calculate occlusion value
	float occlusion = 0.0f;
	for(int i = 0; i < SSAO_KERNEL_SIZE; i++)
	{			
        // Wants a sample in normal oriented hemisphere.
		#if 0
		vec3 samplePos = spherical_kernel(inUV, i);
        samplePos *= (dot(normal, samplePos) >= 0 ? 1: 0) * 2 - 1;
		#else
		vec3 samplePos =  TBN * uboSSAOKernel.samples[i].xyz; 
		#endif
		samplePos = fragPos + samplePos * SSAO_RADIUS; 
		
		// project
		vec4 offset = vec4(samplePos, 1.0f);
		offset = Proj * offset; 
		offset.xyz /= offset.w; 
		offset.xyz = offset.xyz * 0.5f + 0.5f; 
		
		float sampleDepth = getDepth(samplerDepth, offset.xy); 
		//float sampleDepth = texture(samplerNormal, offset.xy).w; 

#define RANGE_CHECK 1
#ifdef RANGE_CHECK
		// Range check
		float rangeCheck = smoothstep(0.0f, 1.0f, SSAO_RADIUS / abs(fragPos.z - sampleDepth));
		occlusion += (sampleDepth < samplePos.z ? 1.0f : 0.0f) * rangeCheck;           
#else
		occlusion += (sampleDepth < samplePos.z ? 1.0f : 0.0f);  
#endif
	}
	occlusion = 1.0 - (occlusion / float(SSAO_KERNEL_SIZE));
	occlusion = pow(occlusion, SSAO_POWER);
	outFragColor = occlusion;
}

