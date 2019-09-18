#version 450 core
#define CAM_NEAR 0.1f
#define GRID_DIM_Z 256

layout(early_fragment_tests) in;
layout(location = 0) in vec4 world_pos_in;

#ifdef ALPHA_TEST
layout(location = 1) in vec2 inUV;
#endif

layout(set = 0, binding = 0) uniform UBO
{
    mat4 view;
    mat4 projection_clip;

    vec2 tile_size; // xy
    uvec2 grid_dim; // xy

    vec3 cam_pos;
    float cam_far;

    vec2 resolution;
    uint num_lights;
} ubo_in;

layout(set = 0, binding = 1, r8ui) uniform uimageBuffer grid_flags;

#ifdef ALPHA_TEST
layout(set = 1, binding = 0) uniform sampler2D DiffMap;
#endif

uvec3 view_pos_to_grid_coord(vec2 frag_pos, float view_z)
{
    vec3 c;
    c.xy = (frag_pos-0.5f) / ubo_in.tile_size;
    c.z = min(float(GRID_DIM_Z - 1), max(0.f, float(GRID_DIM_Z) * log((-view_z - CAM_NEAR) / (ubo_in.cam_far - CAM_NEAR) + 1.f)));
    return uvec3(c);
}

uint grid_coord_to_grid_idx(uvec3 c)
{
    return ubo_in.grid_dim.x * ubo_in.grid_dim.y * c.z + ubo_in.grid_dim.x * c.y + c.x;
}

void main ()
{
#ifdef ALPHA_TEST
    float alpha = texture(DiffMap, inUV).a;
    if (alpha < 0.5) {
        discard;
    }
#endif

    vec4 view_pos = ubo_in.view * world_pos_in;
    uint grid_idx = grid_coord_to_grid_idx(view_pos_to_grid_coord(gl_FragCoord.xy, view_pos.z));
    imageStore(grid_flags, int(grid_idx), uvec4(1, 0, 0, 0));
}
