#version 450

layout (binding = 0) uniform sampler2D samplerColor;

layout (location = 0) in vec2 inUV;

layout (location = 0) out vec4 outColor;

layout (constant_id = 0) const int dir = 0;

void main(void)
{
	// From the OpenGL Super bible
	const float weights[] = float[](0.0024499299678342,
									0.0043538453346397,
									0.0073599963704157,
									0.0118349786570722,
									0.0181026699707781,
									0.0263392293891488,
									0.0364543006660986,
									0.0479932050577658,
									0.0601029809166942,
									0.0715974486241365,
									0.0811305381519717,
									0.0874493212267511,
									0.0896631113333857,
									0.0874493212267511,
									0.0811305381519717,
									0.0715974486241365,
									0.0601029809166942,
									0.0479932050577658,
									0.0364543006660986,
									0.0263392293891488,
									0.0181026699707781,
									0.0118349786570722,
									0.0073599963704157,
									0.0043538453346397,
									0.0024499299678342);


	const float blurScale = 0.003;
	const float blurStrength = 1.0;

	float ar = 1.0;
	// Aspect ratio for vertical blur pass
	if (dir == 1)
	{
		vec2 ts = textureSize(samplerColor, 0);
		ar = ts.y / ts.x;
	}

	vec2 P = inUV.xy - vec2(0, (weights.length() >> 1) * ar * blurScale);

	vec4 color = vec4(0.0);
	for (int i = 0; i < weights.length(); i++)
	{
		vec2 dv = vec2(0.0, i * blurScale) * ar;
		vec4 c = texture(samplerColor, P + dv);
		float l = dot(c.rgb, vec3(0.2126, 0.7152, 0.0722));
		if(l > 0.75)
		color += c * weights[i] * blurStrength;
	}

	outColor = color;
}