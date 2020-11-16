#version 450

layout (set = 1, binding = 0) uniform sampler2DArray DiffMap;

layout (location = 0) in vec3 inNormal;
layout (location = 1) in vec3 inColor;
layout (location = 2) in vec3 inUV;
layout (location = 3) in vec3 inViewVec;
layout (location = 4) in vec3 inLightVec;

layout (location = 0) out vec4 outFragColor;

void main()
{
	vec4 color = texture(DiffMap, inUV);

	if (color.a < 0.5)
	{
		discard;
	}

	vec3 N = normalize(inNormal);
	vec3 L = normalize(inLightVec);
	vec3 ambient = vec3(0.65);
	vec3 diffuse = max(dot(N, L), 0.0) * inColor;
	outFragColor = vec4((ambient + diffuse) * color.rgb, 1.0);
}
