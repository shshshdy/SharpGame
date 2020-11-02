#version 450

precision highp float;

#include <common.h>

#ifdef MS_DEPTH
layout(set = 0, binding = 0) uniform sampler2DMS ms_depth_sampler;
#else
layout(set = 0, binding = 0) uniform sampler2D depth_sampler;
#endif
layout(set = 0, binding = 1) uniform sampler2D color_sampler;

layout(location = 0) in vec2 in_uv;

layout(location = 0) out vec4 o_color;

layout(set = 0, binding = 2) uniform PostprocessingUniform
{
	Camera postprocessing_uniform;
} ;

float linearizeDepth(float depth, float near, float far)
{
    return near * far / (far + depth * (near - far));
}

float getDepth(ivec2 offset)
{
	float depth;
#ifdef MS_DEPTH
	depth = texelFetch(ms_depth_sampler, ivec2(gl_FragCoord.xy) + offset, 0).r;
#else
	depth = texelFetch(depth_sampler, ivec2(gl_FragCoord.xy) + offset, 0).r;
#endif
	return linearizeDepth(depth, postprocessing_uniform.nearPlane, postprocessing_uniform.farPlane);
}

void main(void)
{
	vec4 color = texture(color_sampler, in_uv);
		
	float depth = getDepth(ivec2(0, 0));	//o_color = vec4(d, d, d, 1); return;
	
	vec3 outline_color = vec3(0.0, 0.0, 0.0);
	int thickness = 2;
	float outline = depth - getDepth(ivec2(-thickness, 0));
	outline += depth - getDepth(ivec2(0, thickness));
	outline += depth - getDepth(ivec2(thickness, 0));
	outline += depth - getDepth(ivec2(0, -thickness));

#ifdef OUTLINE_ONLY
	o_color.rgb = vec3(outline);
#else
	o_color.rgb = mix(color.rgb, outline_color, clamp(outline, 0.0, 1.0));
#endif
}
