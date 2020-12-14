using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ComputeSubpass : Subpass
    {
        protected Pass pass;
        public PipelineResourceSet PipelineResourceSet { get; }

        public Action<PipelineResourceSet> onBindResource;

        public uint GroupCountX { get; }
        public uint GroupCountY { get; }
        public uint GroupCountZ { get; }

        public ComputeSubpass(uint groupCountX, uint groupCountY, uint groupCountZ = 1)
        {
            GroupCountX = groupCountX;
            GroupCountY = groupCountY;
            GroupCountZ = groupCountZ;
        }

        public override void Init()
        {
            CreateResources();
            BindResources();
        }

        public override void DeviceReset()
        {
            BindResources();
        }

        protected virtual void CreateResources()
        {
        }

        protected virtual void BindResources()
        {
            onBindResource?.Invoke(PipelineResourceSet);
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            var pipe = pass.GetComputePipeline();

            cmd.BindPipeline(VkPipelineBindPoint.Compute, pipe);

            foreach (var rs in PipelineResourceSet.ResourceSet)
            {
                cmd.BindGraphicsResourceSet(pass.PipelineLayout, rs.Set, rs);
            }

            cmd.Dispatch(100, 100, 1);
        }
    
    }
}
