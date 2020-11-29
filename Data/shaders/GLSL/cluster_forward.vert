#version 450 core
#include "Common.glsl"


layout(location = 0) in vec3 inPos;
layout(location = 1) in vec2 inUV;
layout(location = 2) in vec3 inNormal;
layout(location = 3) in vec3 inTangent;
layout(location = 4) in vec3 inBitangent;

out gl_PerVertex
{
    vec4 gl_Position;
};

layout(location = 0) out vec4 outWorldPos;
layout(location = 1) out vec2 outUV;
layout(location = 2) out mat3 outNormal;



void main()
{
    outWorldPos = Model * vec4(inPos, 1.f);
    outUV = inUV;
    
	outNormal = mat3(Model) * mat3(inTangent, inBitangent, inNormal);

    gl_Position = ViewProj * outWorldPos;
}
