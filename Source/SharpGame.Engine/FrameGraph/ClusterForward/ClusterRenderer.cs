#define NO_DEPTHWRITE

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ClusterUniforms
    {
        public mat4 view;
        public mat4 projection_clip;
        public vec2 tile_size;
        public FixedArray2<uint> grid_dim;
        public vec3 cam_pos;
        public float cam_far;
        public vec2 resolution;
        public uint num_lights;
    };

    public struct QueryData
    {
        public FixedArray2<uint> depth_pass;
        public FixedArray2<uint> clustering;
        public FixedArray2<uint> calc_light_grids;
        public FixedArray2<uint> calc_grid_offsets;
        public FixedArray2<uint> calc_light_list;
        public FixedArray2<uint> onscreen;
        public FixedArray2<uint> transfer;

        public uint Clustering => clustering[1] - clustering[0];
        public uint CalcLightGrids => calc_light_grids[1] - calc_light_grids[0];
        public uint CalcGridOffsets => calc_grid_offsets[1] - calc_grid_offsets[0];
        public uint CalcLightList => calc_light_list[1] - calc_light_list[0];
        public uint SceneRender => onscreen[1] - onscreen[0];
        public uint ClearBuffer => transfer[1] - transfer[0];
    }

    public partial class ClusterRenderer : RenderPipeline
    {
        protected uint MAX_WIDTH = 1920;
        protected uint MAX_HEIGHT = 1080;

        protected uint MIN_NUM_LIGHTS = 1024;
        protected uint MAX_NUM_LIGHTS = 600000;
        protected uint num_lights = 0;

        protected uint TILE_WIDTH = 64;
        protected uint TILE_HEIGHT = 64;

        protected uint tile_count_x = 0;
        protected uint tile_count_y = 0;
        protected uint TILE_COUNT_Z = 256;


        const uint QUERY_DEPTH_PASS = 0;
        const uint QUERY_CLUSTERING = 1;
        const uint QUERY_CALC_LIGHT_GRIDS = 2;
        const uint QUERY_CALC_GRID_OFFSETS = 3;
        const uint QUERY_CALC_LIGHT_LIST = 4;
        const uint QUERY_ONSCREEN = 5;
        const uint QUERY_TRANSFER = 6;
        const uint QUERY_HSIZE = 7;


        uint query_count_;

        protected ClusterUniforms clusterUniforms = new ClusterUniforms();

        protected DoubleBuffer uboCluster;
        protected DoubleBuffer light_pos_ranges;
        protected DoubleBuffer light_colors;

        private Buffer grid_flags;
        private Buffer light_bounds;
        private Buffer grid_light_counts;
        private Buffer grid_light_count_total;
        private Buffer grid_light_count_offsets;
        private Buffer light_list;
        private Buffer grid_light_counts_compare;

        protected Shader clusterLight;
        protected ResourceLayout resourceLayout0;
        protected ResourceLayout resourceLayout1;
        protected ResourceSet[] resourceSet0 = new ResourceSet[2];
        protected ResourceSet resourceSet1;

        QueryPool[] query_pool = new QueryPool[3];

        QueryData[] queryData = new QueryData[3];

        public ref QueryData QueryData => ref queryData[Graphics.WorkImage];

        public QueryPool QueryPool => query_pool[Graphics.WorkImage];

        protected GraphicsPass clusterPass;
        protected ComputePass lightPass;
        protected ScenePass mainPass;

        public ClusterRenderer(string name = "cluster_forward")// : base(name)
        {
            Renderer.OnSubmit += Renderer_OnSubmit;

        }
        
        protected override void Destroy()
        {
            base.Destroy();

            Renderer.OnSubmit -= Renderer_OnSubmit;
            mainPass.OnSubmitBegin -= ClusterRenderer_OnSubmitBegin;
            mainPass.OnSubmitEnd -= ClusterRenderer_OnSubmitEnd;
        }
        /*
        protected override void OnSetFrameGraph(RenderPipeline frameGraph)
        {
            clusterPass = PreappendGraphicsPass(Pass.EarlyZ, 8, DrawClustering);
            clusterPass.PassQueue = PassQueue.EarlyGraphics;

            lightPass = PreappendComputePass(ComputeLight);

            this.OnDraw = DrawScene;
        }*/

        protected override void OnInit()
        {
            AddPass<ShadowPass>(null);

            clusterPass = AddPass<GraphicsPass>(/*Pass.EarlyZ, 8,*/ DrawClustering);
            clusterPass.PassQueue = PassQueue.EarlyGraphics;

            lightPass = AddComputePass(ComputeLight);

            mainPass = AddPass<ScenePass>(DrawScene);
            mainPass.Name = "cluster_forward";
            mainPass.OnSubmitBegin += ClusterRenderer_OnSubmitBegin;
            mainPass.OnSubmitEnd += ClusterRenderer_OnSubmitEnd;
#if NO_DEPTHWRITE
            mainPass.RenderPass = Graphics.CreateRenderPass(true, false);
#endif
            CreateResources();

            InitCluster();

            InitLightCompute();
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

            resourceLayout0 = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(2, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
            };

            resourceLayout1 = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(2, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(3, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(4, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(5, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(6, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
            };

            uint[] queue_families = null;
            uint size = 0;
            SharingMode sharingMode = SharingMode.Exclusive;

            if (Device.QFGraphics != Device.QFCompute)
            {
                sharingMode = SharingMode.Concurrent;
                queue_families = new[] { Device.QFGraphics, Device.QFCompute };
                size = (uint)queue_families.Length;
            }

            uboCluster = new DoubleBuffer(BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent,
                (uint)Utilities.SizeOf<ClusterUniforms>(), sharingMode, queue_families);

            light_pos_ranges = new DoubleBuffer(BufferUsageFlags.StorageTexelBuffer, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent,
                MAX_NUM_LIGHTS * 4 * sizeof(float), SharingMode.Exclusive, queue_families);
            light_pos_ranges.CreateView(Format.R32g32b32a32Sfloat);

            light_colors = new DoubleBuffer(BufferUsageFlags.StorageTexelBuffer, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent,
                MAX_NUM_LIGHTS * sizeof(uint), sharingMode, queue_families);
            light_colors.CreateView(Format.R8g8b8a8Unorm);

            resourceSet0[0] = new ResourceSet(resourceLayout0, uboCluster[0], light_pos_ranges[0], light_colors[0]);
            resourceSet0[1] = new ResourceSet(resourceLayout0, uboCluster[1], light_pos_ranges[1], light_colors[1]);

            uint max_grid_count = ((MAX_WIDTH - 1) / TILE_WIDTH + 1) * ((MAX_HEIGHT - 1) / TILE_HEIGHT + 1) * TILE_COUNT_Z;
            grid_flags = Buffer.CreateTexelBuffer(BufferUsageFlags.TransferDst, max_grid_count, Format.R8Uint, sharingMode, queue_families);
            light_bounds = Buffer.CreateTexelBuffer(BufferUsageFlags.TransferDst, MAX_NUM_LIGHTS * 6 * sizeof(uint), Format.R32Uint, sharingMode, queue_families); // max tile count 1d (z 256)
            grid_light_counts = Buffer.CreateTexelBuffer(BufferUsageFlags.TransferDst, max_grid_count * sizeof(uint), Format.R32Uint, sharingMode, queue_families); // light count / grid
            grid_light_count_total = Buffer.CreateTexelBuffer(BufferUsageFlags.TransferDst, sizeof(uint), Format.R32Uint, sharingMode, queue_families); // light count total * max grid count
            grid_light_count_offsets = Buffer.CreateTexelBuffer(BufferUsageFlags.TransferDst, max_grid_count * sizeof(uint), Format.R32Uint, sharingMode, queue_families); // same as above
            light_list = Buffer.CreateTexelBuffer(BufferUsageFlags.TransferDst, 1024 * 1024 * sizeof(uint), Format.R32Uint, sharingMode, queue_families); // light idx
            grid_light_counts_compare = Buffer.CreateTexelBuffer(BufferUsageFlags.TransferDst, max_grid_count * sizeof(uint), Format.R32Uint, sharingMode, queue_families); // light count / grid

            resourceSet1 = new ResourceSet(resourceLayout1,
                grid_flags, light_bounds, grid_light_counts, grid_light_count_total,
                grid_light_count_offsets, light_list, grid_light_counts_compare);

        }

        protected override void OnUpdate()
        {
            RenderView view = View;
            UpdateLight(view);

            tile_count_x = ((uint)view.Width - 1) / TILE_WIDTH + 1;
            tile_count_y = ((uint)view.Height - 1) / TILE_HEIGHT + 1;

            Camera camera = view.Camera;
            clusterUniforms.view = camera.View;

            mat4 clip = new mat4(1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.5f, 0.0f, 0.0f, 0.0f, 0.5f, 1.0f);

            clusterUniforms.projection_clip = clip*camera.Projection;
            //clusterUniforms.projection_clip = camera.VkProjection;

            clusterUniforms.tile_size[0] = (float)(TILE_WIDTH);
            clusterUniforms.tile_size[1] = (float)(TILE_HEIGHT);
            clusterUniforms.grid_dim[0] = tile_count_x;
            clusterUniforms.grid_dim[1] = tile_count_y;

            clusterUniforms.cam_pos = camera.Node.WorldPosition;
            clusterUniforms.cam_far = camera.FarClip;

            clusterUniforms.resolution[0] = (float)(view.Width);
            clusterUniforms.resolution[1] = (float)(view.Height);
            clusterUniforms.num_lights = num_lights;

            uboCluster.SetData(ref clusterUniforms);
            uboCluster.Flush();
        }

        protected void DrawScene(GraphicsPass renderPass, RenderView view)
        {
            renderPass.BeginRenderPass(view);

            //BeginRenderPass(Framebuffers[Graphics.WorkImage], view.ViewRect, ClearColorValue);

            var batches = view.opaqueBatches;

//             if (MultiThreaded)
//             {
                renderPass.DrawBatchesMT(view, batches, view.Set0, resourceSet0[Graphics.WorkContext], resourceSet1);
//             }
//             else
//             {
//                 renderPass.DrawBatches(view, batches, CmdBuffer, view.Set0, resourceSet0[Graphics.WorkContext], resourceSet1);
//             }

            if (view.alphaTestBatches.Count > 0)
            {
                renderPass.DrawBatches(view, view.alphaTestBatches, renderPass.CmdBuffer, view.Set0, resourceSet0[Graphics.WorkContext], resourceSet1);
            }

            if (view.translucentBatches.Count > 0)
            {
                renderPass.DrawBatches(view, view.translucentBatches, renderPass.CmdBuffer, view.Set0, resourceSet0[Graphics.WorkContext], resourceSet1);
            }

            renderPass.EndRenderPass(view);
        }

        private void ClusterRenderer_OnSubmitBegin(CommandBuffer cmd_buf, int imageIndex)
        {
            var queryPool = query_pool[imageIndex];
            cmd_buf.ResetQueryPool(queryPool, 10, 4);
            cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_ONSCREEN * 2);
        }

        private void ClusterRenderer_OnSubmitEnd(CommandBuffer cmd_buf, int imageIndex)
        {
            var queryPool = query_pool[imageIndex];
            cmd_buf.WriteTimestamp(PipelineStageFlags.ColorAttachmentOutput, queryPool, QUERY_ONSCREEN * 2 + 1);

            // clean up buffers
            unsafe
            {
                BufferMemoryBarrier* transfer_barriers = stackalloc BufferMemoryBarrier[]
                {
                    new BufferMemoryBarrier(grid_flags, AccessFlags.ShaderRead | AccessFlags.ShaderWrite, AccessFlags.TransferWrite),
                    new BufferMemoryBarrier(grid_light_counts, AccessFlags.ShaderRead | AccessFlags.ShaderWrite, AccessFlags.TransferWrite),
                    new BufferMemoryBarrier(grid_light_count_offsets, AccessFlags.ShaderRead | AccessFlags.ShaderWrite, AccessFlags.TransferWrite),
                    new BufferMemoryBarrier(light_list, AccessFlags.ShaderRead | AccessFlags.ShaderWrite, AccessFlags.TransferWrite)
                };

                cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_TRANSFER * 2);

                cmd_buf.PipelineBarrier(PipelineStageFlags.FragmentShader,
                            PipelineStageFlags.Transfer,
                            DependencyFlags.ByRegion,
                            0, null,
                            4,
                            transfer_barriers,
                            0, null);

                cmd_buf.FillBuffer(grid_flags, 0, Buffer.WholeSize, 0);
                cmd_buf.FillBuffer(light_bounds, 0, Buffer.WholeSize, 0);
                cmd_buf.FillBuffer(grid_light_counts, 0, Buffer.WholeSize, 0);
                cmd_buf.FillBuffer(grid_light_count_offsets, 0, Buffer.WholeSize, 0);
                cmd_buf.FillBuffer(grid_light_count_total, 0, Buffer.WholeSize, 0);
                cmd_buf.FillBuffer(light_list, 0, Buffer.WholeSize, 0);
                cmd_buf.FillBuffer(grid_light_counts_compare, 0, Buffer.WholeSize, 0);

                BufferMemoryBarrier* transfer_barriers1 = stackalloc BufferMemoryBarrier[]
                {
                    new BufferMemoryBarrier(grid_flags, AccessFlags.TransferWrite, AccessFlags.ShaderRead | AccessFlags.ShaderWrite),
                    new BufferMemoryBarrier(grid_light_counts, AccessFlags.TransferWrite, AccessFlags.ShaderRead | AccessFlags.ShaderWrite),
                    new BufferMemoryBarrier(grid_light_count_offsets, AccessFlags.TransferWrite, AccessFlags.ShaderRead | AccessFlags.ShaderWrite),
                    new BufferMemoryBarrier(light_list, AccessFlags.TransferWrite, AccessFlags.ShaderRead | AccessFlags.ShaderWrite)
                };

                cmd_buf.PipelineBarrier(PipelineStageFlags.Transfer,
                                            PipelineStageFlags.FragmentShader,
                                            DependencyFlags.ByRegion,
                                            0, null,
                                            4,
                                            transfer_barriers1,
                                            0, null);

                cmd_buf.WriteTimestamp(PipelineStageFlags.Transfer, queryPool, QUERY_TRANSFER * 2 + 1);
            }
        }

        private void Renderer_OnSubmit(int imageIndex, PassQueue passQueue)
        {
            var queryPool = query_pool[imageIndex];
            if (passQueue == PassQueue.EarlyGraphics)
            {
                queryPool.GetResults(2, 2, 2 * sizeof(uint), queryData[imageIndex].clustering.Data, sizeof(uint), QueryResults.QueryWait);
            }
            else if (passQueue == PassQueue.Compute)
            {
                queryPool.GetResults(4, 6, 6 * sizeof(uint), queryData[imageIndex].calc_light_grids.Data, sizeof(uint), QueryResults.QueryWait);
            }
            else if (passQueue == PassQueue.Graphics)
            {
                queryPool.GetResults(10, 4, 4 * sizeof(uint), queryData[imageIndex].onscreen.Data, sizeof(uint), QueryResults.QueryWait);
            }
           
        }

    }
}
