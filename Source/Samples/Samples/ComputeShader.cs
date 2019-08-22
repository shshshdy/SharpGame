using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    public struct Particle
    {
        public Vector2 pos;                              // Particle position
        public Vector2 vel;                              // Particle velocity
        public Vector4 gradientPos;                      // Texture coordiantes for the gradient ramp map
    };

    [SampleDesc(sortOrder = 8)]
    public class ComputeShader : Sample
    {
        const int PARTICLE_COUNT = 256 * 1024;

        DeviceBuffer storageBuffer;
        DeviceBuffer uniformBuffer;

        struct UBO
        {
            public float deltaT;
            public float destX;
            public float destY;
            public int particleCount;
        };

        Pass _computePipeline;
        private CommandBuffer _computeCmdBuffer;
        private ResourceSet _computeResourceSet;

        public override void Init()
        {
            base.Init();

            var shader = Resources.Load<Shader>("shaders/Particle.shader");

            _computePipeline = shader.GetPass("compute");

            Random rand = new Random();
            Particle[] particles = new Particle[PARTICLE_COUNT];
            for(int i = 0; i < PARTICLE_COUNT; i++)
            {
                ref Particle particle = ref particles[i];
                
                particle.pos = rand.NextVector2(new Vector2(-1, -1), new Vector2(1, 1));
                particle.vel = new Vector2(0.0f);
                particle.gradientPos = new Vector4(particle.pos.X / 2.0f, 0, 0, 0);
            }

            storageBuffer = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer | BufferUsageFlags.StorageBuffer, particles, true);
            uniformBuffer = DeviceBuffer.CreateUniformBuffer<UBO>();

            _computeResourceSet = new ResourceSet(_computePipeline.PipelineLayout.ResourceLayout[0], storageBuffer, uniformBuffer);

            RecordComputeCommandBuffer();
        }


        private void RecordComputeCommandBuffer()
        {
            // Record particle movements.
    
            var graphicsToComputeBarrier = new BufferMemoryBarrier(storageBuffer,
                AccessFlags.VertexAttributeRead, AccessFlags.ShaderWrite,
                Graphics.GraphicsQueue.FamilyIndex, Graphics.ComputeQueue.FamilyIndex);

            var computeToGraphicsBarrier = new BufferMemoryBarrier(storageBuffer,
                AccessFlags.ShaderWrite, AccessFlags.VertexAttributeRead,
                Graphics.ComputeQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);
 
            _computeCmdBuffer.Begin();
      
            // Add memory barrier to ensure that the (graphics) vertex shader has fetched attributes
            // before compute starts to write to the buffer.
            _computeCmdBuffer.PipelineBarrier(PipelineStageFlags.VertexInput, PipelineStageFlags.ComputeShader,
                ref graphicsToComputeBarrier);
            _computeCmdBuffer.BindComputePipeline(_computePipeline);
            _computeCmdBuffer.BindComputeResourceSet(_computePipeline.PipelineLayout, 0, _computeResourceSet);
            _computeCmdBuffer.Dispatch((uint)storageBuffer.Count / 256, 1, 1);
            // Add memory barrier to ensure that compute shader has finished writing to the buffer.
            // Without this the (rendering) vertex shader may display incomplete results (partial
            // data from last frame).
            _computeCmdBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.VertexInput,
                ref computeToGraphicsBarrier);
            _computeCmdBuffer.End();
        }
    }
}
