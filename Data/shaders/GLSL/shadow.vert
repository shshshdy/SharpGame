#version 450
#include "UniformsVS.glsl"

layout(location = 0) in vec3 inPos;
#ifdef TEX_LOCATION
layout(location = TEX_LOCATION) in vec2 inUV;
#endif
// todo: pass via specialization constant
#define SHADOW_MAP_CASCADE_COUNT 4

layout(binding = 0) uniform UBO{
    mat4[SHADOW_MAP_CASCADE_COUNT] cascadeViewProjMat;
} ubo;

layout(push_constant) uniform PushConsts{
    uint cascadeIndex;
} pushConsts;

layout(location = 0) out vec2 outUV;

out gl_PerVertex{
    vec4 gl_Position;
};

void main()
{
#ifdef TEX_LOCATION
    outUV = inUV;
#endif
    vec4 pos = Model * vec4(inPos, 1);
    gl_Position = ubo.cascadeViewProjMat[pushConsts.cascadeIndex] * pos;
}
