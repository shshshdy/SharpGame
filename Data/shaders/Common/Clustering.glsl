
#define UBO_SET 0
#include "GridCoord.glsl"

layout(set = 0, binding = 1, rgba32f) uniform imageBuffer light_pos_ranges;

layout(set = 1, binding = 0, r8ui) uniform uimageBuffer grid_flags;
layout(set = 1, binding = 1, r32ui) uniform uimageBuffer light_bounds;
layout(set = 1, binding = 2, r32ui) uniform uimageBuffer grid_light_counts;
layout(set = 1, binding = 3, r32ui) uniform uimageBuffer grid_light_count_total;
layout(set = 1, binding = 4, r32ui) uniform uimageBuffer grid_light_count_offsets;
layout(set = 1, binding = 5, r32ui) uniform uimageBuffer light_list;
layout(set = 1, binding = 6, r32ui) uniform uimageBuffer grid_light_counts_compare;


