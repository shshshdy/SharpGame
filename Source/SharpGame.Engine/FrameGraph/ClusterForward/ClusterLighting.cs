﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public partial class ClusterLighting : ScenePass
    {
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


        const uint QUERY_DEPTH_PASS = 0;
        const uint QUERY_CLUSTERING = 1;
        const uint QUERY_CALC_LIGHT_GRIDS = 2;
        const uint QUERY_CALC_GRID_OFFSETS = 3;
        const uint QUERY_CALC_LIGHT_LIST = 4;
        const uint QUERY_ONSCREEN = 5;
        const uint QUERY_TRANSFER = 6;
        const uint QUERY_HSIZE = 7;


        unsafe struct Query_data
        {
            fixed uint depth_pass[2];
            fixed uint clustering[2];
            fixed uint calc_light_grids[2];
            fixed uint calc_grid_offsets[2];
            fixed uint calc_light_list[2];
            fixed uint onscreen[2];
            fixed uint transfer[2];
        }

        uint query_count_;

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

        QueryPool[] query_pool = new QueryPool[3];

        public QueryPool QueryPool => query_pool[Graphics.WorkImage];

        public override void Init()
        {
            base.Init();

            CreateResources();

            InitEarlyZ();

        }

        protected override void OnSetFrameGraph(FrameGraph frameGraph)
        {
            PreappendGraphicsPass(Pass.EarlyZ, 8, DrawEarlyZ);
            PreappendComputePass(ComputeLight);
        }


        private void CreateResources()
        {
            query_count_ = (uint)QUERY_HSIZE * 2;
            for (int i = 0; i < 3; i++)
            {
                var queryPoolCreateInfo = new QueryPoolCreateInfo(QueryType.Timestamp, query_count_);
                query_pool[i] = new QueryPool(ref queryPoolCreateInfo);
            }

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
                (uint)Utilities.SizeOf<ClusterUBO>(), sharingMode, queue_families);


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

        public override void Update(RenderView view)
        {
            UpdateLight(view);
        }
    }
}
