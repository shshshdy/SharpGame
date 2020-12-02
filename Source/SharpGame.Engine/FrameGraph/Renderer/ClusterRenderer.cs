using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ClusterUBO
    {
        public mat4 view;
        public mat4 projection_clip;
        public mat4 inv_view_proj;
        public vec3 cam_pos;
        public float cam_near;
        public vec3 cam_forward;
        public float cam_far;
        public vec2 tile_size;
        public FixedArray2<uint> grid_dim;     
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


        protected const uint QUERY_DEPTH_PASS = 0;
        protected const uint QUERY_CLUSTERING = 1;
        protected const uint QUERY_CALC_LIGHT_GRIDS = 2;
        protected const uint QUERY_CALC_GRID_OFFSETS = 3;
        protected const uint QUERY_CALC_LIGHT_LIST = 4;
        protected const uint QUERY_ONSCREEN = 5;
        protected const uint QUERY_TRANSFER = 6;
        protected const uint QUERY_HSIZE = 7;


        uint query_count_;

        protected ClusterUBO clusterUniforms = new ClusterUBO();

        protected SharedBuffer uboCluster;
        private Buffer gridFlags;

        protected DescriptorSetLayout resourceLayout0;
        protected DescriptorSetLayout resourceLayout1;
        protected DescriptorSetLayout clusterLayout1;

        protected DescriptorSet resourceSet0;
        protected DescriptorSet resourceSet1;
        protected DescriptorSet clusterSet1;

        protected VkQueryPool[] query_pool = new VkQueryPool[3];
        protected QueryData[] queryData = new QueryData[3];

        public ref QueryData QueryData => ref queryData[Graphics.WorkContext];
        public VkQueryPool QueryPool => query_pool[Graphics.WorkContext];

        protected ComputePass lightCull;

        public ClusterRenderer()
        {
            FrameGraph.OnSubmit += Renderer_OnSubmit;
        }
        
        protected override void Destroy(bool disposing)
        {
            for (int i = 0; i < 3; i++)
            {
                query_pool[i].Dispose();
            }

            FrameGraph.OnSubmit -= Renderer_OnSubmit;

            base.Destroy(disposing);
        }
        
        protected override void OnInit()
        {
            CreateResources();

            InitLightCompute();

            CreateRenderPath();
        }

        protected virtual void CreateResources()
        {
            query_count_ = (uint)QUERY_HSIZE * 2;
            for (int i = 0; i < 3; i++)
            {
                query_pool[i] = new VkQueryPool(VkQueryType.Timestamp, query_count_);
            }

            uint[] queue_families = null;

            VkSharingMode sharingMode = VkSharingMode.Exclusive;

            if (Device.QFGraphics != Device.QFCompute)
            {
                sharingMode = VkSharingMode.Concurrent;
                queue_families = new[] { Device.QFGraphics, Device.QFCompute };               
            }

            uboCluster = new SharedBuffer(VkBufferUsageFlags.UniformBuffer, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                (uint)Utilities.SizeOf<ClusterUBO>(), sharingMode, queue_families);

            uint max_grid_count = ((MAX_WIDTH - 1) / TILE_WIDTH + 1) * ((MAX_HEIGHT - 1) / TILE_HEIGHT + 1) * TILE_COUNT_Z;
            gridFlags = Buffer.CreateTexelBuffer(VkBufferUsageFlags.TransferDst, max_grid_count, VkFormat.R8UInt, sharingMode, queue_families);

            clusterLayout1 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.StorageTexelBuffer, VkShaderStageFlags.Fragment),
            };

            clusterSet1 = new DescriptorSet(clusterLayout1, uboCluster, gridFlags);
        }

        protected FrameGraphPass CreateClusteringPass()
        {
            var depthFormat = Graphics.DepthFormat;
            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;

            var clustering = new FrameGraphPass(SubmitQueue.EarlyGraphics)
            {
                new RenderTextureInfo((uint)width, (uint)height, 1, depthFormat, VkImageUsageFlags.DepthStencilAttachment),

                new SceneSubpass("clustering")
                {
                    Set1 = clusterSet1
                }

            };

            clustering.renderPassCreator = OnCreateClusterRenderPass;
            return clustering;
        }

        RenderPass OnCreateClusterRenderPass()
        {
            VkFormat depthFormat = Device.GetSupportedDepthFormat();
            VkAttachmentDescription[] attachments =
            {
                new VkAttachmentDescription(depthFormat, finalLayout : VkImageLayout.DepthStencilReadOnlyOptimal)
            };

            var depthStencilAttachment = new[]
            {
                 new VkAttachmentReference(0, VkImageLayout.DepthStencilAttachmentOptimal)
            };

            SubpassDescription[] subpassDescription =
            {
		        // clustering subpass
                new SubpassDescription
                {
                    pipelineBindPoint = VkPipelineBindPoint.Graphics,
                    pDepthStencilAttachment = depthStencilAttachment
                },
            };

            // Subpass dependencies for layout transitions
            VkSubpassDependency[] dependencies =
            {
                new VkSubpassDependency
                {
                    srcSubpass = Vulkan.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = VkPipelineStageFlags.BottomOfPipe,
                    dstStageMask = VkPipelineStageFlags.VertexShader,
                    srcAccessMask = VkAccessFlags.MemoryWrite,
                    dstAccessMask = VkAccessFlags.UniformRead,
                    dependencyFlags = VkDependencyFlags.ByRegion
                },

                new VkSubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = Vulkan.SubpassExternal,
                    srcStageMask = VkPipelineStageFlags.FragmentShader,
                    dstStageMask = VkPipelineStageFlags.ComputeShader,
                    srcAccessMask =  VkAccessFlags.ShaderWrite,
                    dstAccessMask = VkAccessFlags.ShaderRead,
                    dependencyFlags = VkDependencyFlags.ByRegion
                },
            };

            return new RenderPass(attachments, subpassDescription, dependencies);
           
        }

        protected virtual void CreateRenderPath()
        {
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

            //clusterUniforms.projection_clip = clip*camera.Projection;
            clusterUniforms.projection_clip = camera.VkProjection;
            clusterUniforms.inv_view_proj = glm.inverse(camera.VkProjection * camera.View);
            clusterUniforms.tile_size[0] = (float)(TILE_WIDTH);
            clusterUniforms.tile_size[1] = (float)(TILE_HEIGHT);
            clusterUniforms.grid_dim[0] = tile_count_x;
            clusterUniforms.grid_dim[1] = tile_count_y;

            clusterUniforms.cam_pos = camera.Node.WorldPosition;
            clusterUniforms.cam_near = camera.NearClip;
            clusterUniforms.cam_forward = camera.Node.WorldDirection;
            clusterUniforms.cam_far = camera.FarClip;


            clusterUniforms.resolution[0] = (float)(view.Width);
            clusterUniforms.resolution[1] = (float)(view.Height);
            clusterUniforms.num_lights = num_lights;

            uboCluster.SetData(ref clusterUniforms);
            uboCluster.Flush();
        }

        protected void ClearBuffers(CommandBuffer cmd_buf, int imageIndex)
        {
            var queryPool = query_pool[imageIndex];

            // clean up buffers
            unsafe
            {
                VkBufferMemoryBarrier* transfer_barriers = stackalloc VkBufferMemoryBarrier[]
                {
                    new VkBufferMemoryBarrier(gridFlags, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite, VkAccessFlags.TransferWrite),
                    new VkBufferMemoryBarrier(gridLightCounts, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite, VkAccessFlags.TransferWrite),
                    new VkBufferMemoryBarrier(gridLightCountOffsets, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite, VkAccessFlags.TransferWrite),
                    new VkBufferMemoryBarrier(lightList, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite, VkAccessFlags.TransferWrite)
                };

                //cmd_buf.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_TRANSFER * 2);

                cmd_buf.PipelineBarrier(VkPipelineStageFlags.FragmentShader,
                            VkPipelineStageFlags.Transfer,
                            VkDependencyFlags.ByRegion,
                            0, null,
                            4,
                            transfer_barriers,
                            0, null);

                cmd_buf.FillBuffer(gridFlags, 0, Vulkan.WholeSize, 0);
                cmd_buf.FillBuffer(lightBounds, 0, Vulkan.WholeSize, 0);
                cmd_buf.FillBuffer(gridLightCounts, 0, Vulkan.WholeSize, 0);
                cmd_buf.FillBuffer(gridLightCountOffsets, 0, Vulkan.WholeSize, 0);
                cmd_buf.FillBuffer(gridLightCountTotal, 0, Vulkan.WholeSize, 0);
                cmd_buf.FillBuffer(lightList, 0, Vulkan.WholeSize, 0);
                cmd_buf.FillBuffer(gridLightCountsCompare, 0, Vulkan.WholeSize, 0);

                VkBufferMemoryBarrier* transfer_barriers1 = stackalloc VkBufferMemoryBarrier[]
                {
                    new VkBufferMemoryBarrier(gridFlags, VkAccessFlags.TransferWrite, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite),
                    new VkBufferMemoryBarrier(gridLightCounts, VkAccessFlags.TransferWrite, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite),
                    new VkBufferMemoryBarrier(gridLightCountOffsets, VkAccessFlags.TransferWrite, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite),
                    new VkBufferMemoryBarrier(lightList, VkAccessFlags.TransferWrite, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite)
                };

                cmd_buf.PipelineBarrier(VkPipelineStageFlags.Transfer,
                                            VkPipelineStageFlags.FragmentShader,
                                            VkDependencyFlags.ByRegion,
                                            0, null,
                                            4,
                                            transfer_barriers1,
                                            0, null);

                //cmd_buf.WriteTimestamp(PipelineStageFlags.Transfer, queryPool, QUERY_TRANSFER * 2 + 1);
            }
        }

        private void Renderer_OnSubmit(RenderContext renderFrame, SubmitQueue passQueue)
        {
            //var queryPool = query_pool[imageIndex];
            if (passQueue == SubmitQueue.EarlyGraphics)
            {
                //queryPool.GetResults(2, 2, 2 * sizeof(uint), queryData[imageIndex].clustering.Data, sizeof(uint), QueryResults.QueryWait);
            }
            else if (passQueue == SubmitQueue.Compute)
            {
               // queryPool.GetResults(4, 6, 6 * sizeof(uint), queryData[imageIndex].calc_light_grids.Data, sizeof(uint), QueryResults.QueryWait);
            }
            else if (passQueue == SubmitQueue.Graphics)
            {
                //queryPool.GetResults(10, 4, 4 * sizeof(uint), queryData[imageIndex].onscreen.Data, sizeof(uint), QueryResults.QueryWait);
            }
           
        }

    }
}
