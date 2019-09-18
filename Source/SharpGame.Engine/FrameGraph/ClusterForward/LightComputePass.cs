
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

        private Buffer p_grid_flags_;
        private Buffer p_light_bounds_;
        private Buffer p_grid_light_counts_;
        private Buffer p_grid_light_count_total_;
        private Buffer p_grid_light_count_offsets_;
        private Buffer p_light_list_;
        private Buffer p_grid_light_counts_compare_;

        private Pass computePipeline;
        private ResourceSet computeResourceSet;

        public LightComputePass()
        {
            OnDraw = DoCompute;

            uint[] queue_families = null;
            uint size = 0;
            SharingMode sharingMode = SharingMode.Exclusive;

            if (Device.QFGraphics != Device.QFCompute)
            {
                sharingMode = SharingMode.Concurrent;
                queue_families = new[]
                {
                    Device.QFGraphics, Device.QFCompute
                };

                size = (uint)queue_families.Length;
            }
            
            uint max_grid_count = ((MAX_WIDTH - 1) / TILE_WIDTH + 1) * ((MAX_HEIGHT - 1) / TILE_HEIGHT + 1) * TILE_COUNT_Z;
            p_grid_flags_ = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageBuffer | BufferUsageFlags.TransferDst,
                max_grid_count, Format.R8Uint, sharingMode, queue_families);

            p_light_bounds_ = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageBuffer | BufferUsageFlags.TransferDst,
                     MAX_NUM_LIGHTS * 6 * sizeof(uint), Format.R32Uint,
                     sharingMode, queue_families); // max tile count 1d (z 256)

            p_grid_light_counts_ = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageBuffer | BufferUsageFlags.TransferDst,
                                  max_grid_count * sizeof(uint), Format.R32Uint,
                                  sharingMode, queue_families); // light count / grid

            p_grid_light_count_total_ = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageBuffer | BufferUsageFlags.TransferDst,
                                   1 * sizeof(uint), Format.R32Uint,
                                   sharingMode, queue_families); // light count total * max grid count

            p_grid_light_count_offsets_ = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageBuffer | BufferUsageFlags.TransferDst,
                                     max_grid_count * sizeof(uint), Format.R32Uint,
                                     sharingMode, queue_families); // same as above

            p_light_list_ = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageBuffer | BufferUsageFlags.TransferDst,
                               1024 * 1024 * sizeof(uint), Format.R32Uint,
                               sharingMode, queue_families); // light idx

            p_grid_light_counts_compare_ = Buffer.CreateTexelBuffer(
                BufferUsageFlags.StorageBuffer | BufferUsageFlags.TransferDst,
                                      max_grid_count * sizeof(uint), Format.R32Uint,
                                      sharingMode, queue_families); // light count / grid
        }

        void DoCompute(ComputePass renderPass, RenderView view)
        {
            var cb = renderPass.CmdBuffer;
            var graphicsToComputeBarrier = new BufferMemoryBarrier(p_grid_flags_,
                AccessFlags.VertexAttributeRead, AccessFlags.ShaderWrite,
                Graphics.GraphicsQueue.FamilyIndex, Graphics.ComputeQueue.FamilyIndex);

            var computeToGraphicsBarrier = new BufferMemoryBarrier(p_grid_flags_,
                AccessFlags.ShaderWrite, AccessFlags.VertexAttributeRead,
                Graphics.ComputeQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);

            cb.Begin();

            // Add memory barrier to ensure that the (graphics) vertex shader has fetched attributes
            // before compute starts to write to the buffer.
            cb.PipelineBarrier(PipelineStageFlags.VertexInput, PipelineStageFlags.ComputeShader, ref graphicsToComputeBarrier);
            cb.BindComputePipeline(computePipeline);
            cb.BindComputeResourceSet(computePipeline.PipelineLayout, 0, computeResourceSet);
            cb.Dispatch((uint)p_grid_flags_.Count / 256, 1, 1);
            // Add memory barrier to ensure that compute shader has finished writing to the buffer.
            // Without this the (rendering) vertex shader may display incomplete results (partial
            // data from last frame).
            cb.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.VertexInput, ref computeToGraphicsBarrier);
            cb.End();
        }
    }
}
