#version 450 core

#include "GridCoord.glsl"

layout(set = 1, binding = 1, r8ui) uniform uimageBuffer grid_flags;

#ifdef ALPHA_TEST
layout(set = 2, binding = 0) uniform sampler2D DiffMap;
#endif


//layout(early_fragment_tests) in;
layout(location = 0) in vec4 inViewPos;

#ifdef ALPHA_TEST
layout(location = 1) in vec2 inUV;
#endif

void main ()
{
#ifdef ALPHA_TEST
    float alpha = texture(DiffMap, inUV).a;
    if (alpha < 0.5) {
        discard;
    }
#endif

    int grid_idx = ViewPosToGridIdx(gl_FragCoord.xy, inViewPos.z);
    imageStore(grid_flags, grid_idx, uvec4(1, 0, 0, 0));
}
