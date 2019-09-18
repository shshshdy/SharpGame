
namespace SharpGame
{
    public class LightComputePass : ComputePass
    {
        struct UBO
        {
            mat4 view;
            //mat4 normal;
            //mat4 model;
            mat4 projection_clip;
            vec2 tile_size; // xy
            Int2 grid_dim; // xy
            vec3 cam_pos;
            float cam_far;
            vec2 resolution;
            uint num_lights;
        };

        uint MAX_WIDTH = 1920;
        uint MAX_HEIGHT = 1080;

        uint MIN_NUM_LIGHTS = 1024;
        uint MAX_NUM_LIGHTS = 600000;
        uint num_lights = 0;

        uint TILE_WIDTH = 64;
        uint TILE_HEIGHT = 64;

        uint tile_count_x = 0;
        uint tile_count_y = 0;
        uint TILE_COUNT_Z = 256;

        private DoubleBuffer ubo;
        private DoubleBuffer light_pos_ranges;
        private DoubleBuffer light_colors;

        private Buffer grid_flags;
        private Buffer light_bounds;
        private Buffer grid_light_counts;
        private Buffer grid_light_count_total;
        private Buffer grid_light_count_offsets;
        private Buffer light_list;
        private Buffer grid_light_counts_compare;

        private Shader clusterLight;
        private ResourceLayout resourceLayout0;
        private ResourceLayout resourceLayout1;
        private ResourceSet[] resourceSet0 = new ResourceSet[2];
        private ResourceSet resourceSet1;
        public LightComputePass()
        {
            OnDraw = DoCompute;

            clusterLight = Resources.Instance.Load<Shader>("Shaders/ClusterLight.shader");

            resourceLayout0 = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Compute),
                new ResourceLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new ResourceLayoutBinding(2, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
            };

            resourceLayout1 = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new ResourceLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new ResourceLayoutBinding(2, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new ResourceLayoutBinding(3, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new ResourceLayoutBinding(4, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new ResourceLayoutBinding(5, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new ResourceLayoutBinding(6, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),

            };

            uint[] queue_families = null;
            uint size = 0;
            SharingMode sharingMode = SharingMode.Exclusive;

            if (Device.QFGraphics != Device.QFCompute)
            {
                sharingMode = SharingMode.Concurrent;
                queue_families = new[]
                {
                    Device.QFGraphics, Device.QFCompute
                };

                size = (uint)queue_families.Length;
            }

            ubo = new DoubleBuffer(BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.HostVisible,
                (uint)Utilities.SizeOf<UBO>(), sharingMode, queue_families);


            light_pos_ranges = new DoubleBuffer(BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.HostVisible,
                MAX_NUM_LIGHTS * 4 * sizeof(float), sharingMode, queue_families);
            light_pos_ranges.CreateView(Format.R32g32b32a32Sfloat);

            light_colors = new DoubleBuffer(BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.HostVisible,
                MAX_NUM_LIGHTS * sizeof(uint), sharingMode, queue_families);
            light_colors.CreateView(Format.R8g8b8a8Unorm);

            resourceSet0[0] = new ResourceSet(resourceLayout0, ubo[0], light_pos_ranges[0], light_colors[0]);
            resourceSet0[1] = new ResourceSet(resourceLayout0, ubo[1], light_pos_ranges[1], light_colors[1]);

            uint max_grid_count = ((MAX_WIDTH - 1) / TILE_WIDTH + 1) * ((MAX_HEIGHT - 1) / TILE_HEIGHT + 1) * TILE_COUNT_Z;
            grid_flags = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageTexelBuffer | BufferUsageFlags.TransferDst,
                max_grid_count, Format.R8Uint, sharingMode, queue_families);

            light_bounds = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageTexelBuffer | BufferUsageFlags.TransferDst,
                     MAX_NUM_LIGHTS * 6 * sizeof(uint), Format.R32Uint,
                     sharingMode, queue_families); // max tile count 1d (z 256)

            grid_light_counts = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageTexelBuffer | BufferUsageFlags.TransferDst,
                                  max_grid_count * sizeof(uint), Format.R32Uint,
                                  sharingMode, queue_families); // light count / grid

            grid_light_count_total = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageTexelBuffer | BufferUsageFlags.TransferDst,
                                   1 * sizeof(uint), Format.R32Uint,
                                   sharingMode, queue_families); // light count total * max grid count

            grid_light_count_offsets = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageTexelBuffer | BufferUsageFlags.TransferDst,
                                     max_grid_count * sizeof(uint), Format.R32Uint,
                                     sharingMode, queue_families); // same as above

            light_list = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageTexelBuffer | BufferUsageFlags.TransferDst,
                               1024 * 1024 * sizeof(uint), Format.R32Uint,
                               sharingMode, queue_families); // light idx

            grid_light_counts_compare = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageTexelBuffer | BufferUsageFlags.TransferDst,
                                      max_grid_count * sizeof(uint), Format.R32Uint,
                                      sharingMode, queue_families); // light count / grid

            resourceSet1 = new ResourceSet(resourceLayout1,
                grid_flags, light_bounds, grid_light_counts, grid_light_count_total,
                grid_light_count_offsets, light_list, grid_light_counts_compare);
        }

        void DoCompute(ComputePass renderPass, RenderView view)
        {
            var cmd_buf = renderPass.CmdBuffer;

            cmd_buf.Begin();

            /*
            // compute
            {               
                update_light_buffers_(elapsed_time, data);

                cmd_buf.resetQueryPool(data.query_pool, 4, 6);

                barriers[0] ={
                    vk::AccessFlagBits::eHostWrite,
		vk::AccessFlagBits::eShaderRead,
		VK_QUEUE_FAMILY_IGNORED,
		VK_QUEUE_FAMILY_IGNORED,
		data.p_light_pos_ranges->p_buf->buf,
		0, VK_WHOLE_SIZE


        };

                cmd_buf.writeTimestamp(vk::PipelineStageFlagBits::eTopOfPipe, data.query_pool, QUERY_CALC_LIGHT_GRIDS * 2);

                cmd_buf.pipelineBarrier(vk::PipelineStageFlagBits::eHost,
                            vk::PipelineStageFlagBits::eComputeShader,
                            vk::DependencyFlagBits::eByRegion,
                            0, nullptr, 1, barriers, 0, nullptr);

                // --------------------- calc light grids ---------------------

                // reads grid_flags, light_pos_ranges
                // writes light_bounds, grid_light_counts

                cmd_buf.bindPipeline(vk::PipelineBindPoint::eCompute, pipelines_.calc_light_grids);
                pipeline_desc_sets_.calc_light_grids[0] = data.desc_set;
                cmd_buf.bindDescriptorSets(vk::PipelineBindPoint::eCompute,
                               pipeline_layouts_.calc_light_grids,
                               0, static_cast<uint32_t>(pipeline_desc_sets_.calc_light_grids.size()),
                               pipeline_desc_sets_.calc_light_grids.data(),
                               0, nullptr);
                cmd_buf.dispatch((p_info_->num_lights - 1) / 32 + 1, 1, 1);

                cmd_buf.writeTimestamp(vk::PipelineStageFlagBits::eComputeShader, data.query_pool, QUERY_CALC_LIGHT_GRIDS * 2 + 1);

                barriers[0] = vk::BufferMemoryBarrier(vk::AccessFlagBits::eShaderRead | vk::AccessFlagBits::eShaderWrite,
                                vk::AccessFlagBits::eShaderRead | vk::AccessFlagBits::eShaderWrite,
                                VK_QUEUE_FAMILY_IGNORED,
                                VK_QUEUE_FAMILY_IGNORED,
                                p_light_bounds_->p_buf->buf,
                                0, VK_WHOLE_SIZE);
                barriers[1] = barriers[0];
                barriers[1].buffer = p_grid_light_counts_->p_buf->buf;
                cmd_buf.pipelineBarrier(vk::PipelineStageFlagBits::eComputeShader,
                            vk::PipelineStageFlagBits::eComputeShader,
                            vk::DependencyFlagBits::eByRegion,
                            0, nullptr, 2, barriers, 0, nullptr);

                // --------------------- calc grid offsets ---------------------

                // reads grid_flags, grid_light_counts
                // writes grid_light_count_total, grid_light_offsets

                cmd_buf.writeTimestamp(vk::PipelineStageFlagBits::eTopOfPipe, data.query_pool, QUERY_CALC_GRID_OFFSETS * 2);

                cmd_buf.bindPipeline(vk::PipelineBindPoint::eCompute, pipelines_.calc_grid_offsets);
                pipeline_desc_sets_.calc_grid_offsets[0] = data.desc_set;
                cmd_buf.bindDescriptorSets(vk::PipelineBindPoint::eCompute,
                               pipeline_layouts_.calc_grid_offsets,
                               0, static_cast<uint32_t>(pipeline_desc_sets_.calc_grid_offsets.size()),
                               pipeline_desc_sets_.calc_grid_offsets.data(),
                               0, nullptr);
                cmd_buf.dispatch((p_info_->tile_count_x - 1) / 16 + 1, (p_info_->tile_count_y - 1) / 16 + 1, p_info_->TILE_COUNT_Z);

                cmd_buf.writeTimestamp(vk::PipelineStageFlagBits::eComputeShader, data.query_pool, QUERY_CALC_GRID_OFFSETS * 2 + 1);

                barriers[0].buffer = p_grid_light_count_total_->p_buf->buf;
                barriers[1].buffer = p_grid_light_count_offsets_->p_buf->buf;
                cmd_buf.pipelineBarrier(vk::PipelineStageFlagBits::eComputeShader,
                            vk::PipelineStageFlagBits::eComputeShader,
                            vk::DependencyFlagBits::eByRegion,
                            0, nullptr, 1, barriers, 0, nullptr);

                // --------------------- calc light list ---------------------

                // reads grid_flags, light_bounds, grid_light_counts, grid_light_offsets
                // writes grid_light_counts_compare, light_list

                cmd_buf.writeTimestamp(vk::PipelineStageFlagBits::eTopOfPipe, data.query_pool, QUERY_CALC_LIGHT_LIST * 2);

                cmd_buf.bindPipeline(vk::PipelineBindPoint::eCompute, pipelines_.calc_light_list);
                pipeline_desc_sets_.calc_light_list[0] = data.desc_set;
                cmd_buf.bindDescriptorSets(vk::PipelineBindPoint::eCompute,
                               pipeline_layouts_.calc_light_list,
                               0, static_cast<uint32_t>(pipeline_desc_sets_.calc_light_list.size()),
                               pipeline_desc_sets_.calc_light_list.data(),
                               0, nullptr);
                cmd_buf.dispatch((p_info_->num_lights - 1) / 32 + 1, 1, 1);

                cmd_buf.writeTimestamp(vk::PipelineStageFlagBits::eFragmentShader, data.query_pool, QUERY_CALC_LIGHT_LIST * 2 + 1);

            }
            */



            cmd_buf.End();


        }

    }
}
