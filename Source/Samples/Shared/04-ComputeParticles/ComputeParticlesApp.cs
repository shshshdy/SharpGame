using SharpGame;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using VulkanCore;

namespace SharpGame.Samples.ComputeParticles
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VertexParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UniformBufferObject
    {
        public Vector2 DstPosition;
        public float DeltaTime;
        public float Padding;
    }

    public class ComputeParticlesApp : Application
    {
        private DescriptorPool _descriptorPool;        

        private Sampler _sampler;
        private Texture _particleDiffuseMap;

        private DescriptorSetLayout _graphicsDescriptorSetLayout;
        private DescriptorSet _graphicsDescriptorSet;
        private Pipeline _graphicsPipeline;
        private Shader _shader;

        private GraphicsBuffer _storageBuffer;
        private GraphicsBuffer _uniformBuffer;
        private DescriptorSetLayout _computeDescriptorSetLayout;
        private PipelineLayout _computePipelineLayout;
        private Pipeline _computePipeline;
        private DescriptorSet _computeDescriptorSet;
        private CommandBuffer _computeCmdBuffer;
        private Fence _computeFence;

        protected override void InitializePermanent()
        {
            _descriptorPool              = ToDispose(CreateDescriptorPool());

            _sampler                     = ToDispose(CreateSampler());
            _particleDiffuseMap          = ResourceCache.Load<Texture>("ParticleDiffuse.ktx");
            _graphicsDescriptorSetLayout = ToDispose(CreateGraphicsDescriptorSetLayout());
            _graphicsDescriptorSet       = CreateGraphicsDescriptorSet();

            _storageBuffer               = ToDispose(CreateStorageBuffer());
            _uniformBuffer               = ToDispose(GraphicsBuffer.DynamicUniform<UniformBufferObject>(Graphics, 1));
            _computeDescriptorSetLayout  = ToDispose(CreateComputeDescriptorSetLayout());
            _computePipelineLayout       = ToDispose(CreateComputePipelineLayout());
            _computeDescriptorSet        = CreateComputeDescriptorSet();
            _computeCmdBuffer            = Graphics.ComputeCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
            _computeFence                = ToDispose(Graphics.Device.CreateFence());

            _shader = new Shader
            {
                ShaderStageInfo = new[]
                {
                    new ShaderStageInfo
                    {
                        Stage = ShaderStages.Vertex,
                        FileName = "Shader.vert.spv",
                        FuncName = "main"
                    },

                    new ShaderStageInfo
                    {
                        Stage = ShaderStages.Fragment,
                        FileName = "Shader.frag.spv",
                        FuncName = "main"
                    }
                }
            };

            _shader.Load();
        }

        protected override void InitializeFrame()
        {
            _computePipeline  = ToDispose(CreateComputePipeline());
            
            _graphicsPipeline = new Pipeline(_shader)
            {
                PrimitiveTopology = PrimitiveTopology.PointList,

                VertexInputStateCreateInfo = new PipelineVertexInputStateCreateInfo
                (
                    new[] { new VertexInputBindingDescription(0, Interop.SizeOf<VertexParticle>(), VertexInputRate.Vertex) },
                    new[]
                    {
                        new VertexInputAttributeDescription(0, 0, Format.R32G32SFloat, 0),
                        new VertexInputAttributeDescription(1, 0, Format.R32G32SFloat, 8),
                        new VertexInputAttributeDescription(2, 0, Format.R32G32B32SFloat, 16)
                    }
                ),

                RasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
                {
                    PolygonMode = PolygonMode.Fill,
                    CullMode = CullModes.None,
                    FrontFace = FrontFace.CounterClockwise,
                    LineWidth = 1.0f
                },

                MultisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
                {
                    RasterizationSamples = SampleCounts.Count1,
                    MinSampleShading = 1.0f
                },

                DepthStencilStateCreateInfo = new PipelineDepthStencilStateCreateInfo(),

                ColorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo(new[]
                {
                    new PipelineColorBlendAttachmentState
                    {
                        BlendEnable = true,
                        ColorWriteMask = ColorComponents.All,
                        ColorBlendOp = BlendOp.Add,
                        SrcColorBlendFactor = BlendFactor.One,
                        DstColorBlendFactor = BlendFactor.One,
                        AlphaBlendOp = BlendOp.Add,
                        SrcAlphaBlendFactor = BlendFactor.SrcAlpha,
                        DstAlphaBlendFactor = BlendFactor.DstAlpha
                    }
                }),

                PipelineLayoutInfo = new PipelineLayoutCreateInfo(new[] { _graphicsDescriptorSetLayout })
            };

            RecordComputeCommandBuffer();
        }

        protected override void Update(Timer timer)
        {
            const float radius = 0.5f;
            const float rotationSpeed = 0.5f;

            var global = new UniformBufferObject
            {
                DeltaTime = timer.DeltaTime,
                DstPosition = new Vector2(
                    radius * (float)Math.Cos(timer.TotalTime * rotationSpeed),
                    radius * (float)Math.Sin(timer.TotalTime * rotationSpeed))
            };

            IntPtr ptr = _uniformBuffer.Map(0, Constant.WholeSize);
            Interop.Write(ptr, ref global);
            _uniformBuffer.Unmap();
        }
        /*
        protected override void Draw(Timer timer)
        {
            // Submit compute commands.
            Graphics.ComputeQueue.Submit(new SubmitInfo(commandBuffers: new[] { _computeCmdBuffer }), _computeFence);
            _computeFence.Wait();
            _computeFence.Reset();

            // Submit graphics commands.
            base.Draw(timer);
        }*/

        protected override void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex)
        {
            cmdBuffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                Renderer._framebuffers[imageIndex],
                new Rect2D(0, 0, Platform.Width, Platform.Height),
                new ClearColorValue(new ColorF4(0, 0, 0, 0)),
                new ClearDepthStencilValue(1.0f, 0)));

            var pipeline = _graphicsPipeline.GetGraphicsPipeline(Renderer.MainRenderPass);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, _graphicsPipeline.pipelineLayout, _graphicsDescriptorSet);
            cmdBuffer.CmdBindVertexBuffer(_storageBuffer);
            cmdBuffer.CmdDraw(_storageBuffer.Count);
            cmdBuffer.CmdEndRenderPass();
        }

        private void RecordComputeCommandBuffer()
        {
            // Record particle movements.

            var graphicsToComputeBarrier = new BufferMemoryBarrier(_storageBuffer,
                Accesses.VertexAttributeRead, Accesses.ShaderWrite,
                Graphics.GraphicsQueue.FamilyIndex, Graphics.ComputeQueue.FamilyIndex);

            var computeToGraphicsBarrier = new BufferMemoryBarrier(_storageBuffer,
                Accesses.ShaderWrite, Accesses.VertexAttributeRead,
                Graphics.ComputeQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);

            _computeCmdBuffer.Begin();

            // Add memory barrier to ensure that the (graphics) vertex shader has fetched attributes
            // before compute starts to write to the buffer.
            _computeCmdBuffer.CmdPipelineBarrier(PipelineStages.VertexInput, PipelineStages.ComputeShader,
                bufferMemoryBarriers: new[] { graphicsToComputeBarrier });
            _computeCmdBuffer.CmdBindPipeline(PipelineBindPoint.Compute, _computePipeline.pipeline);
            _computeCmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Compute, _computePipelineLayout, _computeDescriptorSet);
            _computeCmdBuffer.CmdDispatch(_storageBuffer.Count / 256, 1, 1);
            // Add memory barrier to ensure that compute shader has finished writing to the buffer.
            // Without this the (rendering) vertex shader may display incomplete results (partial
            // data from last frame).
            _computeCmdBuffer.CmdPipelineBarrier(PipelineStages.ComputeShader, PipelineStages.VertexInput,
                bufferMemoryBarriers: new[] { computeToGraphicsBarrier });

            _computeCmdBuffer.End();
        }

        private GraphicsBuffer CreateStorageBuffer()
        {
            var random = new Random();
            
            int numParticles = Platform.Platform == PlatformType.Android
                ? 256 * 1024
                : 256 * 2048; // ~500k particles.

            var particles = new VertexParticle[numParticles];
            for (int i = 0; i < numParticles; i++)
            {
                particles[i] = new VertexParticle
                {
                    Position = new Vector2(
                        ((float)random.NextDouble() - 0.5f) * 2.0f,
                        ((float)random.NextDouble() - 0.5f) * 2.0f),
                    Color = new Vector4(
                        0.5f + (float)random.NextDouble() / 2.0f,
                        (float)random.NextDouble() / 2.0f,
                        (float)random.NextDouble() / 2.0f,
                        1.0f)
                };
            }

            return GraphicsBuffer.Storage(Graphics, particles);
        }

        private DescriptorPool CreateDescriptorPool()
        {
            return Graphics.Device.CreateDescriptorPool(new DescriptorPoolCreateInfo(3, new[]
            {
                new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                new DescriptorPoolSize(DescriptorType.StorageBuffer, 1),
                new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
            }));
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
            
        private DescriptorSetLayout CreateGraphicsDescriptorSetLayout()
        {
            return Graphics.Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                new DescriptorSetLayoutBinding(0, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)));
        }

        private DescriptorSet CreateGraphicsDescriptorSet()
        {
            DescriptorSet descriptorSet = _descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(1, _graphicsDescriptorSetLayout))[0];
            _descriptorPool.UpdateSets(new[]
            {
                // Particle diffuse map.
                new WriteDescriptorSet(descriptorSet, 0, 0, 1, DescriptorType.CombinedImageSampler,
                    new[] { new DescriptorImageInfo(_sampler, _particleDiffuseMap.View, ImageLayout.ColorAttachmentOptimal) })
            });
            return descriptorSet;
        }

        private DescriptorSetLayout CreateComputeDescriptorSetLayout()
        {
            return Graphics.Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                new DescriptorSetLayoutBinding(0, DescriptorType.StorageBuffer, 1, ShaderStages.Compute),
                new DescriptorSetLayoutBinding(1, DescriptorType.UniformBuffer, 1, ShaderStages.Compute)));
        }

        private PipelineLayout CreateComputePipelineLayout()
        {
            return Graphics.Device.CreatePipelineLayout(new PipelineLayoutCreateInfo(new[] { _computeDescriptorSetLayout }));
        }

        private Pipeline CreateComputePipeline()
        {
            var pipelineCreateInfo = new ComputePipelineCreateInfo(
                new PipelineShaderStageCreateInfo(ShaderStages.Compute, ResourceCache.Load<ShaderModule>("shader.comp.spv"), "main"),
                _computePipelineLayout);

            var pipeline = new Pipeline
            {
                pipeline = Graphics.Device.CreateComputePipeline(pipelineCreateInfo)
            };

            return pipeline;
        }

        private DescriptorSet CreateComputeDescriptorSet()
        {
            DescriptorSet descriptorSet = _descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(1, _computeDescriptorSetLayout))[0];
            _descriptorPool.UpdateSets(new[]
            {
                // Particles storage buffer.
                new WriteDescriptorSet(descriptorSet, 0, 0, 1, DescriptorType.StorageBuffer,
                    bufferInfo: new[] { new DescriptorBufferInfo(_storageBuffer) }),
                // Global simulation data (ie. delta time, etc).
                new WriteDescriptorSet(descriptorSet, 1, 0, 1, DescriptorType.UniformBuffer,
                    bufferInfo: new[] { new DescriptorBufferInfo(_uniformBuffer) }),
            });
            return descriptorSet;
        }
    }
}
