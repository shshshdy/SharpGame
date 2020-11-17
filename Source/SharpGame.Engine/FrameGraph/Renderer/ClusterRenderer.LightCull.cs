
using System;
using Vulkan;

namespace SharpGame
{
    public partial class ClusterRenderer : RenderPipeline
    {
        private DescriptorSetLayout computeLayout0;
        private DescriptorSetLayout computeLayout1;

        private DescriptorSet computeSet0;
        private DescriptorSet computeSet1;
        PipelineLayout pipelineLayout;

        protected Shader clusterLight;
        private void InitLightCompute()
        {
            clusterLight = Resources.Instance.Load<Shader>("Shaders/ClusterLight.shader");

            computeLayout0 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Compute),
                new DescriptorSetLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
            };

            computeLayout1 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new DescriptorSetLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new DescriptorSetLayoutBinding(2, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new DescriptorSetLayoutBinding(3, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new DescriptorSetLayoutBinding(4, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new DescriptorSetLayoutBinding(5, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
                new DescriptorSetLayoutBinding(6, DescriptorType.StorageTexelBuffer, ShaderStage.Compute),
            };

            computeSet0 = new DescriptorSet(computeLayout0, uboCluster, light_pos_ranges);

            pipelineLayout = new PipelineLayout(computeLayout0, computeLayout1);

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
            tile_count_x = ((uint)View.ViewRect.width - 1) / TILE_WIDTH + 1;
            tile_count_y = ((uint)View.ViewRect.height - 1) / TILE_HEIGHT + 1;

            cmd_buf.ResetQueryPool(QueryPool, 4, 6);
            //cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, QueryPool, QUERY_CALC_LIGHT_GRIDS * 2);

            BufferMemoryBarrier* barriers = stackalloc BufferMemoryBarrier[2];
            barriers[0] = new BufferMemoryBarrier(light_pos_ranges.Buffer, AccessFlags.HostWrite, AccessFlags.ShaderRead);

            cmd_buf.PipelineBarrier(PipelineStageFlags.Host, PipelineStageFlags.ComputeShader, DependencyFlags.ByRegion, 0, null, 1, barriers, 0, null);

            // --------------------- calc light grids ---------------------

            // reads grid_flags, light_pos_ranges
            // writes light_bounds, grid_light_counts

            {
                var pass = clusterLight.Pass[0];
                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pipelineLayout, 0, computeSet0);
                cmd_buf.BindComputeResourceSet(pipelineLayout, 1, computeSet1);
                cmd_buf.Dispatch((num_lights - 1) / 32 + 1, 1, 1);

             //   cmd_buf.WriteTimestamp(PipelineStageFlags.ComputeShader, QueryPool, QUERY_CALC_LIGHT_GRIDS * 2 + 1);

                barriers[0] = new BufferMemoryBarrier(lightBounds, AccessFlags.ShaderRead | AccessFlags.ShaderWrite,
                                AccessFlags.ShaderRead | AccessFlags.ShaderWrite);
                barriers[1] = barriers[0];
                barriers[1].Buffer = gridLightCounts;
                cmd_buf.PipelineBarrier(PipelineStageFlags.ComputeShader,
                            PipelineStageFlags.ComputeShader,
                            DependencyFlags.ByRegion,
                            0, null, 2, barriers, 0, null);
            }

            // --------------------- calc grid offsets ---------------------

            // reads grid_flags, grid_light_counts
            // writes grid_light_count_total, grid_light_offsets
            {
            //    cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, QueryPool, QUERY_CALC_GRID_OFFSETS * 2);

                var pass = clusterLight.Pass[1];
                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pipelineLayout, 0, computeSet0);
                cmd_buf.BindComputeResourceSet(pipelineLayout, 1, computeSet1);

                cmd_buf.Dispatch((tile_count_x - 1) / 16 + 1, (tile_count_y - 1) / 16 + 1, TILE_COUNT_Z);

            //    cmd_buf.WriteTimestamp(PipelineStageFlags.ComputeShader, QueryPool, QUERY_CALC_GRID_OFFSETS * 2 + 1);

                barriers[0].Buffer = gridLightCountTotal;
                barriers[1].Buffer = gridLightCountOffsets;
                cmd_buf.PipelineBarrier(PipelineStageFlags.ComputeShader,
                            PipelineStageFlags.ComputeShader,
                            DependencyFlags.ByRegion,
                            0, null, 1, barriers, 0, null);
            }

            // --------------------- calc light list ---------------------

            // reads grid_flags, light_bounds, grid_light_counts, grid_light_offsets
            // writes grid_light_counts_compare, light_list
            {
             //   cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, QueryPool, QUERY_CALC_LIGHT_LIST * 2);

                var pass = clusterLight.Pass[2];

                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pipelineLayout, 0, computeSet0);
                cmd_buf.BindComputeResourceSet(pipelineLayout, 1, computeSet1);
                cmd_buf.Dispatch((num_lights - 1) / 32 + 1, 1, 1);

            //    cmd_buf.WriteTimestamp(PipelineStageFlags.FragmentShader, QueryPool, QUERY_CALC_LIGHT_LIST * 2 + 1);

            }


        }

    }
}
