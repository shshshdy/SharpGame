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

        private Pipeline _computePipeline;
        private Pass _computePass;

        private DescriptorSet _computeDescriptorSet;
        private CommandBuffer _computeCmdBuffer;
        private Fence _computeFence;

        protected override void OnInit()
        {
            SubscribeToEvent((Resizing e) => RecordComputeCommandBuffer());

            SubscribeToEvent<BeginRender>(Handle);

            SubscribeToEvent<BeginRenderPass>(Handle);

            _descriptorPool = CreateDescriptorPool();

            _sampler = graphics_.CreateSampler();
            _particleDiffuseMap = resourceCache_.Load<Texture>("ParticleDiffuse.ktx").Result;
            _graphicsDescriptorSetLayout = CreateGraphicsDescriptorSetLayout();
            _graphicsDescriptorSet = CreateGraphicsDescriptorSet();

            _storageBuffer = CreateStorageBuffer();
            _uniformBuffer = GraphicsBuffer.DynamicUniform<UniformBufferObject>(1);
            _computeDescriptorSetLayout = CreateComputeDescriptorSetLayout();
            _computeDescriptorSet = CreateComputeDescriptorSet();
            _computeCmdBuffer = graphics_.ComputeCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
            _computeFence = graphics_.CreateFence();

            _shader = new Shader
            {
                Name = "Shader",
                ["main"] = new Pass("Shader.vert.spv", "Shader.frag.spv")
            };
            
            _computePass = new Pass("shader.comp.spv");

            _computePipeline = new Pipeline
            {
                PipelineLayoutInfo = new PipelineLayoutCreateInfo(new[] { _computeDescriptorSetLayout })               
            };
            
            _graphicsPipeline = new Pipeline
            {
                PrimitiveTopology = PrimitiveTopology.PointList,

                VertexInputState = new PipelineVertexInputStateCreateInfo
                (
                    new[] { new VertexInputBindingDescription(0, Interop.SizeOf<VertexParticle>(), VertexInputRate.Vertex) },
                    new[]
                    {
                        new VertexInputAttributeDescription(0, 0, Format.R32G32SFloat, 0),
                        new VertexInputAttributeDescription(1, 0, Format.R32G32SFloat, 8),
                        new VertexInputAttributeDescription(2, 0, Format.R32G32B32SFloat, 16)
                    }
                ),

                CullMode = CullModes.None,

                DepthStencilState = new PipelineDepthStencilStateCreateInfo(),

                ColorBlendState = new PipelineColorBlendStateCreateInfo(new[]
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

        void Handle(BeginRender e)
        {
            // Submit compute commands.
            graphics_.ComputeQueue.Submit(new SubmitInfo(commandBuffers: new[] { _computeCmdBuffer }), _computeFence);
            _computeFence.Wait();
            _computeFence.Reset();            
        }

        void Handle(BeginRenderPass e)
        {
            var cmdBuffer = e.commandBuffer;
            var pipeline = _graphicsPipeline.GetGraphicsPipeline(e.renderPass, _shader, null);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, _graphicsPipeline.pipelineLayout, _graphicsDescriptorSet);
            cmdBuffer.CmdBindVertexBuffer(_storageBuffer);
            cmdBuffer.CmdDraw(_storageBuffer.Count);
        }

        private void RecordComputeCommandBuffer()
        {
            // Record particle movements.
            var graphicsToComputeBarrier = new BufferMemoryBarrier(_storageBuffer,
                Accesses.VertexAttributeRead, Accesses.ShaderWrite,
                graphics_.GraphicsQueue.FamilyIndex, graphics_.ComputeQueue.FamilyIndex);

            var computeToGraphicsBarrier = new BufferMemoryBarrier(_storageBuffer,
                Accesses.ShaderWrite, Accesses.VertexAttributeRead,
                graphics_.ComputeQueue.FamilyIndex, graphics_.GraphicsQueue.FamilyIndex);

            _computeCmdBuffer.Begin();

            // Add memory barrier to ensure that the (graphics) vertex shader has fetched attributes
            // before compute starts to write to the buffer.
            _computeCmdBuffer.CmdPipelineBarrier(PipelineStages.VertexInput, PipelineStages.ComputeShader,
                bufferMemoryBarriers: new[] { graphicsToComputeBarrier });
            var pipeline = _computePipeline.GetComputePipeline(renderer_.MainRenderPass, _computePass);
                _computeCmdBuffer.CmdBindPipeline(PipelineBindPoint.Compute, pipeline);
            _computeCmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Compute, _computePipeline.pipelineLayout, _computeDescriptorSet);
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
            
            int numParticles = platform_.Platform == PlatformType.Android
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

            return GraphicsBuffer.Storage(particles);
        }

        private DescriptorPool CreateDescriptorPool()
        {
            return graphics_.CreateDescriptorPool(new[]
            {
                new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                new DescriptorPoolSize(DescriptorType.StorageBuffer, 1),
                new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
            });
        }
            
        private DescriptorSetLayout CreateGraphicsDescriptorSetLayout()
        {
            return graphics_.CreateDescriptorSetLayout(new DescriptorSetLayoutBinding(0, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment));
        }

        private DescriptorSet CreateGraphicsDescriptorSet()
        {
            DescriptorSet descriptorSet = _descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(1, _graphicsDescriptorSetLayout))[0];
            _descriptorPool.UpdateSets(new[]
            {
                // Particle diffuse map.
                new WriteDescriptorSet(descriptorSet, 0, 0, 1, DescriptorType.CombinedImageSampler,
                    new[] { new DescriptorImageInfo(_sampler, _particleDiffuseMap.View, ImageLayout.General/*ColorAttachmentOptimal*/) })
            });
            return descriptorSet;
        }

        private DescriptorSetLayout CreateComputeDescriptorSetLayout()
        {
            return graphics_.CreateDescriptorSetLayout(
                new DescriptorSetLayoutBinding(0, DescriptorType.StorageBuffer, 1, ShaderStages.Compute),
                new DescriptorSetLayoutBinding(1, DescriptorType.UniformBuffer, 1, ShaderStages.Compute));
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
