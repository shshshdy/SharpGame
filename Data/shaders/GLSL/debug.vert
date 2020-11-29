
#version 450
			
#include "Common.glsl"
  
layout(location = 0) in vec3 in_Position;
layout(location = 1) in vec4 in_Color;

layout (location = 0) out vec4 out_Color;

out gl_PerVertex
{
	vec4 gl_Position;
};

void main() 
{
    out_Color = in_Color;
	gl_Position = ViewProj * Model* vec4(in_Position.xyz, 1.0);
}