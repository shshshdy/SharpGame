#version 450

layout(location = 0) in vec3 inPosition;

layout(location = 0) out vec2 outUV;

out gl_PerVertex
{
	vec4 gl_Position;
};

void main() 
{
	outUV = (inPosition.xy + 1.0f) / 2;
	gl_Position = vec4(inPosition, 1.0f);


}
