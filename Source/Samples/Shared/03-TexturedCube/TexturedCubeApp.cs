using System;
using System.Numerics;
using System.Runtime.InteropServices;
using VulkanCore;

namespace SharpGame.Samples.TexturedCube
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WorldViewProjection
    {
        public Matrix4x4 World;
        public Matrix4x4 View;
        public Matrix4x4 Projection;
    }

    public class TexturedCubeApp : Application
    {
        private Pipeline _pipeline;
        private Shader _shader;

        private DescriptorSetLayout _descriptorSetLayout;
        private DescriptorPool _descriptorPool;
        private DescriptorSet _descriptorSet;        

        private Sampler _sampler;
        private Texture _cubeTexture;

        private GraphicsBuffer _cubeVertices;
        private GraphicsBuffer _cubeIndices;

        private GraphicsBuffer _uniformBuffer;
        private WorldViewProjection _wvp;


        protected override void InitializePermanent()
        {
            var cube = GeometricPrimitive.Box(1.0f, 1.0f, 1.0f);

            _cubeTexture         = ResourceCache.Load<Texture>("IndustryForgedDark512.ktx");
            _cubeVertices        = ToDispose(GraphicsBuffer.Vertex(cube.Vertices));
            _cubeIndices         = ToDispose(GraphicsBuffer.Index(cube.Indices));
            _sampler             = ToDispose(CreateSampler());
            _uniformBuffer       = ToDispose(GraphicsBuffer.DynamicUniform<WorldViewProjection>(1));
            _descriptorSetLayout = ToDispose(CreateDescriptorSetLayout());
            _descriptorPool      = ToDispose(CreateDescriptorPool());
            _descriptorSet       = CreateDescriptorSet(); // Will be freed when pool is destroyed.


            _shader = new Shader
            {
                ShaderStageInfo = new[]
                {
                    new ShaderStageInfo
                    {
                        Stage = ShaderStages.Vertex,
                        FileName = "Textured.vert.spv",
                        FuncName = "main"
                    },

                    new ShaderStageInfo
                    {
                        Stage = ShaderStages.Fragment,
                        FileName = "Textured.frag.spv",
                        FuncName = "main"
                    }
                }
            };

            _shader.Load();
        }

        protected override void InitializeFrame()
        {
            _pipeline = new Pipeline(_shader)
            {
                VertexInputStateCreateInfo = new PipelineVertexInputStateCreateInfo(
                new[] { new VertexInputBindingDescription(0, Interop.SizeOf<Vertex>(), VertexInputRate.Vertex) },
                new[]
                {
                    new VertexInputAttributeDescription(0, 0, Format.R32G32B32SFloat, 0),  // Position.
                    new VertexInputAttributeDescription(1, 0, Format.R32G32B32SFloat, 12), // Normal.
                    new VertexInputAttributeDescription(2, 0, Format.R32G32SFloat, 24)     // TexCoord.
                }
            ),
            RasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
                {
                    PolygonMode = PolygonMode.Fill,
                    CullMode = CullModes.Back,
                    FrontFace = FrontFace.CounterClockwise,
                    LineWidth = 1.0f
                },

                DepthStencilStateCreateInfo = new PipelineDepthStencilStateCreateInfo
                {
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.LessOrEqual,
                    Back = new StencilOpState
                    {
                        FailOp = StencilOp.Keep,
                        PassOp = StencilOp.Keep,
                        CompareOp = CompareOp.Always
                    },
                    Front = new StencilOpState
                    {
                        FailOp = StencilOp.Keep,
                        PassOp = StencilOp.Keep,
                        CompareOp = CompareOp.Always
                    }
                },

                ColorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo(new[]
                {
                    new PipelineColorBlendAttachmentState
                    {
                        SrcColorBlendFactor = BlendFactor.One,
                        DstColorBlendFactor = BlendFactor.Zero,
                        ColorBlendOp = BlendOp.Add,
                        SrcAlphaBlendFactor = BlendFactor.One,
                        DstAlphaBlendFactor = BlendFactor.Zero,
                        AlphaBlendOp = BlendOp.Add,
                        ColorWriteMask = ColorComponents.All
                    }
                }),

                PipelineLayoutInfo = new PipelineLayoutCreateInfo(
                    new[] { _descriptorSetLayout }
                    )
            };

            SetViewProjection();
        }

        protected override void Update(Timer timer)
        {
            const float twoPi      = (float)Math.PI * 2.0f;
            const float yawSpeed   = twoPi / 4.0f;
            const float pitchSpeed = 0.0f;
            const float rollSpeed  = twoPi / 4.0f;

            _wvp.World = Matrix4x4.CreateFromYawPitchRoll(
                timer.TotalTime * yawSpeed % twoPi,
                timer.TotalTime * pitchSpeed % twoPi,
                timer.TotalTime * rollSpeed % twoPi);

            UpdateUniformBuffers();
        }

        protected override void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex)
        {
            var renderPassBeginInfo = new RenderPassBeginInfo(
                Renderer._framebuffers[imageIndex],
                new Rect2D(0, 0, Platform.Width, Platform.Height),
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)),
                new ClearDepthStencilValue(1.0f, 0));

            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo);

            var pipeline = _pipeline.GetGraphicsPipeline(Renderer.MainRenderPass);
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, _pipeline.pipelineLayout, _descriptorSet);

            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            cmdBuffer.CmdBindVertexBuffer(_cubeVertices);
            cmdBuffer.CmdBindIndexBuffer(_cubeIndices);
            cmdBuffer.CmdDrawIndexed(_cubeIndices.Count);
            cmdBuffer.CmdEndRenderPass();
        }

        private Sampler CreateSampler()
        {
            var createInfo = new SamplerCreateInfo
            {
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                MipmapMode = SamplerMipmapMode.Linear
            };
            // We also enable anisotropic filtering. Because that feature is optional, it must be
            // checked if it is supported by the device.
            if (Graphics.Features.SamplerAnisotropy)
            {
                createInfo.AnisotropyEnable = true;
                createInfo.MaxAnisotropy = Graphics.Properties.Limits.MaxSamplerAnisotropy;
            }
            else
            {
                createInfo.MaxAnisotropy = 1.0f;
            }
            return Graphics.Device.CreateSampler(createInfo);
        }

        private void SetViewProjection()
        {
            const float cameraDistance = 2.5f;
            _wvp.View = Matrix4x4.CreateLookAt(Vector3.UnitZ * cameraDistance, Vector3.Zero, Vector3.UnitY);
            _wvp.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                (float)Math.PI / 4,
                (float)Graphics.Host.Width / Graphics.Host.Height,
                1.0f, 1000.0f);
        }

        private void UpdateUniformBuffers()
        {
            IntPtr ptr = _uniformBuffer.Map(0, Interop.SizeOf<WorldViewProjection>());
            Interop.Write(ptr, ref _wvp);
            _uniformBuffer.Unmap();
        }

        private DescriptorPool CreateDescriptorPool()
        {
            var descriptorPoolSizes = new[]
            {
                new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
            };
            return Graphics.Device.CreateDescriptorPool(
                new DescriptorPoolCreateInfo(descriptorPoolSizes.Length, descriptorPoolSizes));
        }

        private DescriptorSet CreateDescriptorSet()
        {
            DescriptorSet descriptorSet = _descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(1, _descriptorSetLayout))[0];
            // Update the descriptor set for the shader binding point.
            var writeDescriptorSets = new[]
            {
                new WriteDescriptorSet(descriptorSet, 0, 0, 1, DescriptorType.UniformBuffer,
                    bufferInfo: new[] { new DescriptorBufferInfo(_uniformBuffer) }),
                new WriteDescriptorSet(descriptorSet, 1, 0, 1, DescriptorType.CombinedImageSampler,
                    imageInfo: new[] { new DescriptorImageInfo(_sampler, _cubeTexture.View, ImageLayout.General) })
            };
            _descriptorPool.UpdateSets(writeDescriptorSets);
            return descriptorSet;
        }

        private DescriptorSetLayout CreateDescriptorSetLayout()
        {
            return Graphics.Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)));
        }

    }
}
