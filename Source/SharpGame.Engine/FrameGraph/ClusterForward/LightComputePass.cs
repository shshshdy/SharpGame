using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class LightComputePass : ComputePass
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

        private DeviceBuffer storageBuffer;
        private DeviceBuffer uniformBuffer;

        private Pass computePipeline;
        private ResourceSet computeResourceSet;

        public LightComputePass()
        {
            OnDraw = DoCompute;

            storageBuffer = new DeviceBuffer(BufferUsageFlags.StorageTexelBuffer | BufferUsageFlags.TransferDst,
                MemoryPropertyFlags.DeviceLocal, 111, 1);
        }

        void DoCompute(ComputePass renderPass, RenderView view)
        {
            var cb = renderPass.CmdBuffer;
            var graphicsToComputeBarrier = new BufferMemoryBarrier(storageBuffer,
                AccessFlags.VertexAttributeRead, AccessFlags.ShaderWrite,
                Graphics.GraphicsQueue.FamilyIndex, Graphics.ComputeQueue.FamilyIndex);

            var computeToGraphicsBarrier = new BufferMemoryBarrier(storageBuffer,
                AccessFlags.ShaderWrite, AccessFlags.VertexAttributeRead,
                Graphics.ComputeQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);

            cb.Begin();

            // Add memory barrier to ensure that the (graphics) vertex shader has fetched attributes
            // before compute starts to write to the buffer.
            cb.PipelineBarrier(PipelineStageFlags.VertexInput, PipelineStageFlags.ComputeShader, ref graphicsToComputeBarrier);
            cb.BindComputePipeline(computePipeline);
            cb.BindComputeResourceSet(computePipeline.PipelineLayout, 0, computeResourceSet);
            cb.Dispatch((uint)storageBuffer.Count / 256, 1, 1);
            // Add memory barrier to ensure that compute shader has finished writing to the buffer.
            // Without this the (rendering) vertex shader may display incomplete results (partial
            // data from last frame).
            cb.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.VertexInput, ref computeToGraphicsBarrier);
            cb.End();
        }
    }
}
