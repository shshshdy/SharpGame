
#define CAM_NEAR 0.1f
#define GRID_DIM_Z 256

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

layout (set = 0, binding = 1, rgba32f) uniform imageBuffer light_pos_ranges;

layout(set = 1, binding = 0, r8ui) uniform uimageBuffer grid_flags;
layout (set = 1, binding = 1, r32ui) uniform uimageBuffer light_bounds;
layout (set = 1, binding = 2, r32ui) uniform uimageBuffer grid_light_counts;
layout (set = 1, binding = 3, r32ui) uniform uimageBuffer grid_light_count_total;
layout (set = 1, binding = 4, r32ui) uniform uimageBuffer grid_light_count_offsets;
layout(set = 1, binding = 5, r32ui) uniform uimageBuffer light_list;
layout(set = 1, binding = 6, r32ui) uniform uimageBuffer grid_light_counts_compare;


vec3 get_view_space_pos(vec3 pos_in)
{
    return (ubo_in.view * vec4(pos_in, 1.f)).xyz;
}

vec2 view_pos_to_frag_pos(vec3 view_pos)
{
    vec4 clip_pos = ubo_in.projection_clip * vec4(view_pos, 1.f);
    vec3 ndc = clip_pos.xyz / clip_pos.w;
    return 0.5 * (1.f + ndc.xy) * ubo_in.resolution;
}

vec3 view_pos_to_grid_coord(vec2 frag_pos, float view_z)
{
    vec3 c;
    c.xy = frag_pos / ubo_in.tile_size;
    c.z = min(float(GRID_DIM_Z - 1), max(0.f, float(GRID_DIM_Z) * log((view_z - CAM_NEAR) / (ubo_in.cam_far - CAM_NEAR) + 1.f)));
    return c;
}

int grid_coord_to_grid_idx(uint i, uint j, uint k)
{
    return int(ubo_in.grid_dim.x * ubo_in.grid_dim.y * k + ubo_in.grid_dim.x * j + i);
}
