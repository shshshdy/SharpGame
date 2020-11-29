
layout(set = 1, binding = 1, rgba32f) uniform readonly imageBuffer light_pos_ranges;
layout(set = 1, binding = 2, rgba8) uniform readonly imageBuffer light_colors;

layout(set = 2, binding = 0, r8ui) uniform uimageBuffer grid_flags;
layout(set = 2, binding = 1, r32ui) uniform uimageBuffer light_bounds;
layout(set = 2, binding = 2, r32ui) uniform uimageBuffer grid_light_counts;
layout(set = 2, binding = 3, r32ui) uniform uimageBuffer grid_light_count_total;
layout(set = 2, binding = 4, r32ui) uniform uimageBuffer grid_light_count_offsets;
layout(set = 2, binding = 5, r32ui) uniform uimageBuffer light_list;
layout(set = 2, binding = 6, r32ui) uniform uimageBuffer grid_light_counts_compare;

vec3 ClusterLighting(vec3 world_pos, vec3 world_norm, vec3 diffColor, vec3 specularColor, float specularPower)
{
    vec3 outDiff = vec3(0);
    vec3 outSpec = vec3(0);
    vec3 V = normalize(ubo_in.cam_pos - world_pos);

    float r0 = 0.02f;

    float fresnel = max(0.f, r0 + (1.f - r0) * pow(1.f - max(dot(V, world_norm), 0.f), 5.f));
    vec3 fresnel_specular = specularColor * (1.f - specularColor) * fresnel;

    vec3 view_pos = (ubo_in.view * vec4(world_pos.xyz, 1.f)).xyz;
    uvec3 grid_coord = view_pos_to_grid_coord(gl_FragCoord.xy, view_pos.z);
    int grid_idx = grid_coord_to_grid_idx(grid_coord.x, grid_coord.y, grid_coord.z);

    if (imageLoad(grid_flags, grid_idx).r == 1)
    {
        uint offset = imageLoad(grid_light_count_offsets, grid_idx).r;
        uint light_count = imageLoad(grid_light_counts, grid_idx).r;
        float c = light_count / 10.0f;

        for (uint i = 0; i < light_count; i++)
        {
            int light_idx = int(imageLoad(light_list, int(offset + i)).r);
            vec4 light_pos_range = imageLoad(light_pos_ranges, light_idx);
            float dist = distance(light_pos_range.xyz, world_pos);
        
            if (dist < light_pos_range.w)
            {
                vec3 l = normalize(light_pos_range.xyz - world_pos);
                vec3 h = normalize(0.5f * (V + l));

#ifdef TRANSLUCENT
				float lambertien = abs(dot(world_norm, l));
#else
				float lambertien = max(dot(world_norm, l), 0.f);
#endif
                float atten = max(1.f - max(0.f, dist / light_pos_range.w), 0.f);

				vec3 specular; 
#ifdef TRANSLUCENT

				specular = 0.5f * fresnel_specular;
				//specular = specularColor* pow(abs(dot(h, world_norm)), specularPower);
#else
				if (specularPower > 0.f) {

                    vec3 blinn_phong_specular = specularColor * pow(max(0.f, dot(h, world_norm)), specularPower);

                    specular = fresnel_specular + blinn_phong_specular;
                }
                else {
                    specular = 0.5f * fresnel_specular;
                }
#endif
                vec3 light_color = imageLoad(light_colors, light_idx).rgb;
                vec3 diffuse = light_color * lambertien * atten;

                outDiff += diffuse;
                outSpec += diffuse * specular;
            }
        }
    }

    return diffColor.rgb * outDiff + outSpec;
}
