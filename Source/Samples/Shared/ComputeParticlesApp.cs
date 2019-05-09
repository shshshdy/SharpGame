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
        private Texture _particleDiffuseMap;
        private ResourceLayout _graphicsDescriptorSetLayout;
        private ResourceSet _graphicsDescriptorSet;

        private Pipeline _graphicsPipeline;
        private Shader _shader;

        private GraphicsBuffer _storageBuffer;
        private GraphicsBuffer _uniformBuffer;
        private ResourceLayout _computeDescriptorSetLayout;
        private ResourceSet _computeDescriptorSet;

        private Pipeline _computePipeline;
        private Pass _computePass;

        private CommandBuffer _computeCmdBuffer;
        private Fence _computeFence;

        protected override void Init()
        {
            this.SubscribeToEvent((Resizing e) => RecordComputeCommandBuffer());

            this.SubscribeToEvent<BeginRender>(Handle);

            this.SubscribeToEvent<BeginRenderPass>(Handle);

            _particleDiffuseMap = resourceCache_.Load<Texture>("ParticleDiffuse.ktx").Result;
            _graphicsDescriptorSetLayout = new ResourceLayout(
                new DescriptorSetLayoutBinding(0, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment));
            _graphicsDescriptorSet = new ResourceSet(_graphicsDescriptorSetLayout, _particleDiffuseMap);

            _storageBuffer = CreateStorageBuffer();
            _uniformBuffer = UniformBuffer.Create<UniformBufferObject>(1);
            _computeDescriptorSetLayout = new ResourceLayout(
                new DescriptorSetLayoutBinding(0, DescriptorType.StorageBuffer, 1, ShaderStages.Compute),
                new DescriptorSetLayoutBinding(1, DescriptorType.UniformBuffer, 1, ShaderStages.Compute));

            _computeDescriptorSet = new ResourceSet(_computeDescriptorSetLayout, _storageBuffer, _uniformBuffer);

            _computeCmdBuffer = graphics_.ComputeCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
            _computeFence = Graphics.CreateFence();

            _shader = new Shader
            (
                Name = "Shader",
                new Pass("Shader.vert.spv", "Shader.frag.spv")
                {
                    ResourceLayout = _graphicsDescriptorSetLayout
                }
            );
            
            _computePass = new Pass(computeShader: "shader.comp.spv")
            {
                ResourceLayout = _computeDescriptorSetLayout
            };

            _computePipeline = new Pipeline
            {             
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

            };

            RecordComputeCommandBuffer();

        }

        protected override void Update()
        {
            const float radius = 0.5f;
            const float rotationSpeed = 0.5f;

            var global = new UniformBufferObject
            {
                DeltaTime = Time.Delta,
                DstPosition = new Vector2(
                    radius * (float)Math.Cos(Time.Total * rotationSpeed),
                    radius * (float)Math.Sin(Time.Total * rotationSpeed))
            };

            _uniformBuffer.SetData(ref global);
            
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
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, _graphicsPipeline.pipelineLayout, _graphicsDescriptorSet.descriptorSet);
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
            var pipeline = _computePipeline.GetComputePipeline(_computePass);
                _computeCmdBuffer.CmdBindPipeline(PipelineBindPoint.Compute, pipeline);
            _computeCmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Compute, _computePipeline.pipelineLayout, _computeDescriptorSet.descriptorSet);
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
            
            int numParticles = gameWindow_.Platform == PlatformType.Android
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

            return StorageBuffer.Create(particles);
        }

    }
}
