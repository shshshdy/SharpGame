#version 450 core

#include "GridCoord.glsl"

layout(set = 1, binding = 1, r8ui) uniform uimageBuffer grid_flags;

#ifdef ALPHA_TEST
layout(set = 2, binding = 0) uniform sampler2D DiffMap;
#endif


//layout(early_fragment_tests) in;
layout(location = 0) in vec4 world_pos_in;

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

    vec4 view_pos = ubo_in.view * world_pos_in;
    
    uvec3 grid_coord = view_pos_to_grid_coord(gl_FragCoord.xy, view_pos.z);
    uint grid_idx = grid_coord_to_grid_idx(grid_coord.x, grid_coord.y, grid_coord.z);
    imageStore(grid_flags, int(grid_idx), uvec4(1, 0, 0, 0));
}
