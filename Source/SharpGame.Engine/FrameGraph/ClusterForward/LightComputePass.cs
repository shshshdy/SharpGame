
using System;
using Vulkan;

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


            light_pos_ranges = new DoubleBuffer(BufferUsageFlags.StorageTexelBuffer, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent,
                MAX_NUM_LIGHTS * 4 * sizeof(float), SharingMode.Exclusive, queue_families);
            light_pos_ranges.CreateView(Format.R32g32b32a32Sfloat);

            light_colors = new DoubleBuffer(BufferUsageFlags.StorageTexelBuffer, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent,
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

        void update_light_buffers_(RenderView view)
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
                Color4 color = light.EffectiveColor;
                light_colors.SetData(ref color, offset1);
                offset += 16;
                offset1 += 4;
                num_lights++;
            }

            light_pos_ranges.Flush();
            light_colors.Flush();
        }

        unsafe void DoCompute(ComputePass renderPass, RenderView view)
        {
            tile_count_x = ((uint)view.ViewRect.width - 1) / TILE_WIDTH + 1;
            tile_count_y = ((uint)view.ViewRect.height - 1) / TILE_HEIGHT + 1;

            update_light_buffers_(view);
            
            var cmd_buf = renderPass.CmdBuffer;

            cmd_buf.Begin();
#if true
            /*
          
                update_light_buffers_(elapsed_time, data);

                cmd_buf.resetQueryPool(data.query_pool, 4, 6);
                cmd_buf.writeTimestamp(PipelineStageFlags.TopOfPipe, data.query_pool, QUERY_CALC_LIGHT_GRIDS * 2);

                */

            BufferMemoryBarrier* barriers = stackalloc BufferMemoryBarrier[2];
            barriers[0] = new BufferMemoryBarrier(light_pos_ranges.Buffer, AccessFlags.HostWrite, AccessFlags.ShaderRead);

            cmd_buf.PipelineBarrier(PipelineStageFlags.Host, PipelineStageFlags.ComputeShader, DependencyFlags.ByRegion, 0, null, 1, barriers, 0, null);

            // --------------------- calc light grids ---------------------

            // reads grid_flags, light_pos_ranges
            // writes light_bounds, grid_light_counts

            {
                var pass = clusterLight.Pass[0];
                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 0, resourceSet0[Graphics.Instance.WorkContext]);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 1, resourceSet1);
                cmd_buf.Dispatch((num_lights - 1) / 32 + 1, 1, 1);

                // cmd_buf.writeTimestamp(PipelineStageFlags.ComputeShader, data.query_pool, QUERY_CALC_LIGHT_GRIDS * 2 + 1);

                barriers[0] = new BufferMemoryBarrier(light_bounds, AccessFlags.ShaderRead | AccessFlags.ShaderWrite,
                                AccessFlags.ShaderRead | AccessFlags.ShaderWrite);
                barriers[1] = barriers[0];
                barriers[1].Buffer = grid_light_counts;
                cmd_buf.PipelineBarrier(PipelineStageFlags.ComputeShader,
                            PipelineStageFlags.ComputeShader,
                            DependencyFlags.ByRegion,
                            0, null, 2, barriers, 0, null);
            }
            // --------------------- calc grid offsets ---------------------

            // reads grid_flags, grid_light_counts
            // writes grid_light_count_total, grid_light_offsets
            {
                //cmd_buf.writeTimestamp(PipelineStageFlags.TopOfPipe, data.query_pool, QUERY_CALC_GRID_OFFSETS * 2);

                var pass = clusterLight.Pass[1];
                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 0, resourceSet0[Graphics.Instance.WorkContext]);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 1, resourceSet1);

                cmd_buf.Dispatch((tile_count_x - 1) / 16 + 1, (tile_count_y - 1) / 16 + 1, TILE_COUNT_Z);

                //cmd_buf.writeTimestamp(PipelineStageFlags.ComputeShader, data.query_pool, QUERY_CALC_GRID_OFFSETS * 2 + 1);

                barriers[0].Buffer = grid_light_count_total;
                barriers[1].Buffer = grid_light_count_offsets;
                cmd_buf.PipelineBarrier(PipelineStageFlags.ComputeShader,
                            PipelineStageFlags.ComputeShader,
                            DependencyFlags.ByRegion,
                            0, null, 1, barriers, 0, null);
            }

            // --------------------- calc light list ---------------------

            // reads grid_flags, light_bounds, grid_light_counts, grid_light_offsets
            // writes grid_light_counts_compare, light_list
            {
                //cmd_buf.writeTimestamp(PipelineStageFlags.TopOfPipe, data.query_pool, QUERY_CALC_LIGHT_LIST * 2);

                var pass = clusterLight.Pass[2];

                cmd_buf.BindComputePipeline(pass);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 0, resourceSet0[Graphics.Instance.WorkContext]);
                cmd_buf.BindComputeResourceSet(pass.PipelineLayout, 1, resourceSet1);
                cmd_buf.Dispatch((num_lights - 1) / 32 + 1, 1, 1);

                // cmd_buf.writeTimestamp(PipelineStageFlags.FragmentShader, data.query_pool, QUERY_CALC_LIGHT_LIST * 2 + 1);

            }
#endif
            cmd_buf.End();


        }

    }
}
