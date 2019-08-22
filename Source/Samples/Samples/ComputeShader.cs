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
        Particle[] particles;

        private CommandBuffer _computeCmdBuffer;

        public override void Init()
        {
            base.Init();

            Random rand = new Random();

            particles = new Particle[PARTICLE_COUNT];
            for(int i = 0; i < PARTICLE_COUNT; i++)
            {
                ref Particle particle = ref particles[i];
                
                particle.pos = rand.NextVector2(new Vector2(-1, -1), new Vector2(1, 1));
                particle.vel = new Vector2(0.0f);
                particle.gradientPos = new Vector4(particle.pos.X / 2.0f, 0, 0, 0);
            }

            storageBuffer = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer | BufferUsageFlags.StorageBuffer, particles, true);
        }


        private void RecordComputeCommandBuffer()
        {
            // Record particle movements.
           /*
            var graphicsToComputeBarrier = new BufferMemoryBarrier(storageBuffer,
                AccessFlags.VertexAttributeRead, AccessFlags.ShaderWrite,
                Graphics.GraphicsQueue.FamilyIndex, Context.ComputeQueue.FamilyIndex);

            var computeToGraphicsBarrier = new BufferMemoryBarrier(_storageBuffer,
                Accesses.ShaderWrite, Accesses.VertexAttributeRead,
                Context.ComputeQueue.FamilyIndex, Context.GraphicsQueue.FamilyIndex);
 
            _computeCmdBuffer.Begin();

            // Add memory barrier to ensure that the (graphics) vertex shader has fetched attributes
            // before compute starts to write to the buffer.
            _computeCmdBuffer.CmdPipelineBarrier(PipelineStages.VertexInput, PipelineStages.ComputeShader,
                bufferMemoryBarriers: new[] { graphicsToComputeBarrier });
            _computeCmdBuffer.CmdBindPipeline(PipelineBindPoint.Compute, _computePipeline);
            _computeCmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Compute, _computePipelineLayout, _computeDescriptorSet);
            _computeCmdBuffer.CmdDispatch(_storageBuffer.Count / 256, 1, 1);
            // Add memory barrier to ensure that compute shader has finished writing to the buffer.
            // Without this the (rendering) vertex shader may display incomplete results (partial
            // data from last frame).
            _computeCmdBuffer.CmdPipelineBarrier(PipelineStages.ComputeShader, PipelineStages.VertexInput,
                bufferMemoryBarriers: new[] { computeToGraphicsBarrier });

            _computeCmdBuffer.End();*/
        }
    }
}
