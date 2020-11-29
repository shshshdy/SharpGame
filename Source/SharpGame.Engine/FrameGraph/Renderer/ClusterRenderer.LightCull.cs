
using System;

namespace SharpGame
{
    public partial class ClusterRenderer : RenderPipeline
    {
        protected SharedBuffer light_pos_ranges;
        protected SharedBuffer light_colors;

        private Buffer lightBounds;
        private Buffer gridLightCounts;
        private Buffer gridLightCountTotal;
        private Buffer gridLightCountOffsets;
        private Buffer lightList;
        private Buffer gridLightCountsCompare;

        private DescriptorSetLayout computeLayout0;
        private DescriptorSetLayout computeLayout1;

        private DescriptorSet computeSet0;
        private DescriptorSet computeSet1;

        protected Shader clusterLight;
        private void InitLightCompute()
        {
            uint[] queue_families = null;

            VkSharingMode sharingMode = VkSharingMode.Exclusive;

            if (Device.QFGraphics != Device.QFCompute)
            {
                sharingMode = VkSharingMode.Concurrent;
                queue_families = new[] { Device.QFGraphics, Device.QFCompute };
            }

            uint max_grid_count = ((MAX_WIDTH - 1) / TILE_WIDTH + 1) * ((MAX_HEIGHT - 1) / TILE_HEIGHT + 1) * TILE_COUNT_Z;

            light_pos_ranges = new SharedBuffer(VkBufferUsageFlags.StorageTexelBuffer, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                MAX_NUM_LIGHTS * 4 * sizeof(float), VkSharingMode.Exclusive, queue_families);
            light_pos_ranges.CreateView(VkFormat.R32G32B32A32SFloat);

            light_colors = new SharedBuffer(VkBufferUsageFlags.StorageTexelBuffer, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                MAX_NUM_LIGHTS * sizeof(uint), sharingMode, queue_families);
            light_colors.CreateView(VkFormat.R8G8B8A8UNorm);

            lightBounds = Buffer.CreateTexelBuffer(VkBufferUsageFlags.TransferDst, MAX_NUM_LIGHTS * 6 * sizeof(uint), VkFormat.R32UInt, sharingMode, queue_families); // max tile count 1d (z 256)
            gridLightCounts = Buffer.CreateTexelBuffer(VkBufferUsageFlags.TransferDst, max_grid_count * sizeof(uint), VkFormat.R32UInt, sharingMode, queue_families); // light count / grid
            gridLightCountTotal = Buffer.CreateTexelBuffer(VkBufferUsageFlags.TransferDst, sizeof(uint), VkFormat.R32UInt, sharingMode, queue_families); // light count total * max grid count
            gridLightCountOffsets = Buffer.CreateTexelBuffer(VkBufferUsageFlags.TransferDst, max_grid_count * sizeof(uint), VkFormat.R32UInt, sharingMode, queue_families); // same as above
            lightList = Buffer.CreateTexelBuffer(VkBufferUsageFlags.TransferDst, 1024 * 1024 * sizeof(uint), VkFormat.R32UInt, sharingMode, queue_families); // light idx
            gridLightCountsCompare = Buffer.CreateTexelBuffer(VkBufferUsageFlags.TransferDst, max_grid_count * sizeof(uint), VkFormat.R32UInt, sharingMode, queue_families); // light count / grid

            resourceLayout0 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(2, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
            };

            resourceLayout1 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(2, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(3, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(4, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(5, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(6, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
            };

            resourceSet0 = new DescriptorSet(resourceLayout0, uboCluster, light_pos_ranges, light_colors);
            resourceSet1 = new DescriptorSet(resourceLayout1, gridFlags, lightBounds, gridLightCounts, gridLightCountTotal,
                gridLightCountOffsets, lightList, gridLightCountsCompare);


            clusterLight = Resources.Instance.Load<Shader>("Shaders/ClusterLight.shader");

            computeLayout0 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.Compute),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Compute),
            };

            computeLayout1 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Compute),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Compute),
                new DescriptorSetLayoutBinding(2, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Compute),
                new DescriptorSetLayoutBinding(3, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Compute),
                new DescriptorSetLayoutBinding(4, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Compute),
                new DescriptorSetLayoutBinding(5, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Compute),
                new DescriptorSetLayoutBinding(6, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Compute),
            };

            computeSet0 = new DescriptorSet(computeLayout0, uboCluster, light_pos_ranges);

            computeSet1 = new DescriptorSet(computeLayout1,
                gridFlags, lightBounds, gridLightCounts, gridLightCountTotal,
                gridLightCountOffsets, lightList, gridLightCountsCompare);
        }

        private void UpdateLight(RenderView view)
        {
            uint offset = 0;
            uint offset1 = 0;
            num_lights = 0;
            foreach (var light in view.lights)
            {
                if(light.LightType != LightType.Point)
                {
                    continue;
                }

                vec4 pos = new vec4(light.Node.WorldPosition, light.Range);
                light_pos_ranges.SetData(ref pos, offset);
                int color = light.EffectiveColor.ToRgba();
                light_colors.SetData(ref color, offset1);
                offset += 16;
                offset1 += 4;
                num_lights++;
            }

            light_pos_ranges.Flush();
            light_colors.Flush();
        }

        protected unsafe void ComputeLight(ComputePass renderPass, RenderContext rc, CommandBuffer cmd_buf)
        {
            tile_count_x = ((uint)View.ViewRect.extent.width - 1) / TILE_WIDTH + 1;
            tile_count_y = ((uint)View.ViewRect.extent.height - 1) / TILE_HEIGHT + 1;

            cmd_buf.ResetQueryPool(QueryPool, 4, 6);
            //cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, QueryPool, QUERY_CALC_LIGHT_GRIDS * 2);

            VkBufferMemoryBarrier* barriers = stackalloc VkBufferMemoryBarrier[2];
            barriers[0] = new VkBufferMemoryBarrier(light_pos_ranges.Buffer.handle, VkAccessFlags.HostWrite, VkAccessFlags.ShaderRead);

            cmd_buf.PipelineBarrier(VkPipelineStageFlags.Host, VkPipelineStageFlags.ComputeShader, VkDependencyFlags.ByRegion, 0, null, 1, barriers, 0, null);

            // --------------------- calc light grids ---------------------

            // reads grid_flags, light_pos_ranges
            // writes light_bounds, grid_light_counts

            {
                var pass = clusterLight.Pass[0];
                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 0, computeSet0);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 1, computeSet1);
                cmd_buf.Dispatch((num_lights - 1) / 32 + 1, 1, 1);

             //   cmd_buf.WriteTimestamp(PipelineStageFlags.ComputeShader, QueryPool, QUERY_CALC_LIGHT_GRIDS * 2 + 1);

                barriers[0] = new VkBufferMemoryBarrier(lightBounds.handle, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite,
                                VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite);
                barriers[1] = barriers[0];
                barriers[1].buffer = gridLightCounts.handle;
                cmd_buf.PipelineBarrier(VkPipelineStageFlags.ComputeShader,
                            VkPipelineStageFlags.ComputeShader,
                            VkDependencyFlags.ByRegion,
                            0, null, 2, barriers, 0, null);
            }

            // --------------------- calc grid offsets ---------------------

            // reads grid_flags, grid_light_counts
            // writes grid_light_count_total, grid_light_offsets
            {
            //    cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, QueryPool, QUERY_CALC_GRID_OFFSETS * 2);

                var pass = clusterLight.Pass[1];
                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 0, computeSet0);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 1, computeSet1);

                cmd_buf.Dispatch((tile_count_x - 1) / 16 + 1, (tile_count_y - 1) / 16 + 1, TILE_COUNT_Z);

            //    cmd_buf.WriteTimestamp(PipelineStageFlags.ComputeShader, QueryPool, QUERY_CALC_GRID_OFFSETS * 2 + 1);

                barriers[0].buffer = gridLightCountTotal;
                barriers[1].buffer = gridLightCountOffsets;
                cmd_buf.PipelineBarrier(VkPipelineStageFlags.ComputeShader,
                            VkPipelineStageFlags.ComputeShader,
                            VkDependencyFlags.ByRegion,
                            0, null, 1, barriers, 0, null);
            }

            // --------------------- calc light list ---------------------

            // reads grid_flags, light_bounds, grid_light_counts, grid_light_offsets
            // writes grid_light_counts_compare, light_list
            {
             //   cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, QueryPool, QUERY_CALC_LIGHT_LIST * 2);

                var pass = clusterLight.Pass[2];

                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 0, computeSet0);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 1, computeSet1);
                cmd_buf.Dispatch((num_lights - 1) / 32 + 1, 1, 1);

            //    cmd_buf.WriteTimestamp(PipelineStageFlags.FragmentShader, QueryPool, QUERY_CALC_LIGHT_LIST * 2 + 1);

            }


        }

    }
}
