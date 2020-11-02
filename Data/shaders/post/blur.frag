#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (binding = 0) uniform sampler2D samplerSSAO;
layout (binding = 1) uniform sampler2D samplerColor;

layout (location = 0) in vec2 inUV;

layout (location = 0) out vec4 outFragColor;

void main() 
{
	float ssao = texture(samplerSSAO, inUV).r;

	const int blurRange = 2;
	int n = 0;
	vec2 texelSize = 1.0 / vec2(textureSize(samplerSSAO, 0));
	float result = 0.0;
	for (int x = -blurRange; x < blurRange; x++) 
	{
		for (int y = -blurRange; y < blurRange; y++) 
		{
			vec2 offset = vec2(float(x), float(y)) * texelSize;
			result += texture(samplerSSAO, inUV + offset).r;
			n++;
		}
	}

	float res = result / (float(n));
	vec4 color = texture(samplerColor, inUV);
	outFragColor = vec4(res * color.rgb, color.a);
	//outFragColor = vec4(res, res, res, 1); return;
	
}