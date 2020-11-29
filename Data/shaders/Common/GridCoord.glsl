
#define CAM_NEAR 0.1f
#define GRID_DIM_Z 256

layout(set = 1, binding = 0) uniform UBO
{
    mat4 view;
    mat4 projection_clip;
    mat4 inv_view_proj;

    vec2 tile_size; // xy
    uvec2 grid_dim; // xy

    vec3 cam_pos;
    float cam_near;
    
    vec3 cam_forward;
    float cam_far;

    vec2 resolution;
    uint num_lights;
} ubo_in;

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

uvec3 view_pos_to_grid_coord(vec2 frag_pos, float view_z)
{
    vec3 c;
    c.xy = frag_pos / ubo_in.tile_size;
    c.z = min(float(GRID_DIM_Z - 1), max(0.f, float(GRID_DIM_Z) * log((view_z - CAM_NEAR) / (ubo_in.cam_far - CAM_NEAR) + 1.f)));
    return uvec3(c);
}

int grid_coord_to_grid_idx(uint i, uint j, uint k)
{
    return int(ubo_in.grid_dim.x * ubo_in.grid_dim.y * k + ubo_in.grid_dim.x * j + i);
}


int ViewPosToGridIdx(vec2 frag_pos, float view_z)
{
    vec3 c;
    c.xy = frag_pos / ubo_in.tile_size;
    c.z = min(float(GRID_DIM_Z - 1), max(0.f, float(GRID_DIM_Z) * log((view_z - CAM_NEAR) / (ubo_in.cam_far - CAM_NEAR) + 1.f)));
    return int(ubo_in.grid_dim.x * ubo_in.grid_dim.y * uint(c.z) + ubo_in.grid_dim.x * uint(c.y) + uint(c.x));
}
