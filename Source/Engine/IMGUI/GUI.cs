using NuklearSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using VulkanCore;

using static NuklearSharp.NuklearNative;

namespace SharpGame
{

    public unsafe class GUI : Object
    {
        private GraphicsBuffer _vertexBuffer;
        private GraphicsBuffer _indexBuffer;
        private GraphicsBuffer _projMatrixBuffer;


        private ResourceSet resourceSet_;
        private Shader uiShader_;
        private Pipeline pipeline_;
        private Texture fontTex_;

        public GUI()
        {

            var graphics = Get<Graphics>();
            var cache = Get<ResourceCache>();

            uiShader_ = new Shader(
                "UI",
                new Pass("ImGui.vert.spv", "ImGui.frag.spv")
                {
                    ResourceLayout = new ResourceLayout(
                        new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                        new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
                    )
                }
            );

            _projMatrixBuffer = GraphicsBuffer.CreateUniform<Matrix>();

            pipeline_ = new Pipeline
            {
                VertexInputState = Pos2dTexColorVertex.Layout,
                DepthTestEnable = false,
                DepthWriteEnable = false,
                CullMode = CullModes.None,
                BlendMode = BlendMode.Alpha,
                DynamicStateCreateInfo = new PipelineDynamicStateCreateInfo(DynamicState.Scissor)
            };

            _vertexBuffer = GraphicsBuffer.CreateDynamic<Pos2dTexColorVertex>(BufferUsages.VertexBuffer, 4046);
            _indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsages.IndexBuffer, 4046);

            var resourceLayout = uiShader_.Main.ResourceLayout;
            resourceSet_ = new ResourceSet(resourceLayout, _projMatrixBuffer, fontTex_);


            this.SubscribeToEvent((ref BeginFrame e) => UpdateGUI());

            this.SubscribeToEvent((EndRenderPass e) => RenderGUI(e.renderPass));
        }

        private void UpdateGUI()
        {
        }


        void RenderGUI(RenderPass renderPass)
        {

        }

    }
}
