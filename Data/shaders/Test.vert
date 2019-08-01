#version 450
	
#include "UniformsVS.glsl" 
		
layout (location = 0) in vec3 in_Position;
layout (location = 1) in vec3 in_Normal;
layout (location = 2) in vec2 in_TexCoord;


layout (location = 0) out vec2 out_TexCoord;

out gl_PerVertex
{
	vec4 gl_Position;
};

void main() {
	out_TexCoord = in_TexCoord;
	gl_Position = ViewProj * model* vec4(in_Position.xyz, 1.0);
}