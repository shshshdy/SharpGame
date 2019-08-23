using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    public class FrameGraph : Object
    {
        public RenderTarget[] RenderTargets { get; set; }

        public List<FrameGraphPass> RenderPassList { get; set; } = new List<FrameGraphPass>();

        public FrameGraph()
        {
        }
        
        public void AddGraphicsPass(Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                OnDraw = onDraw,
                FrameGraph = this
            };

            RenderPassList.Add(renderPass);
        }

        public void AddComputePass(Action<ComputePass, RenderView> onDraw)
        {
            var renderPass = new ComputePass
            {
                OnDraw = onDraw,
                FrameGraph = this
            };

            RenderPassList.Add(renderPass);
        }

        public void AddRenderPass(FrameGraphPass renderPass)
        {
            renderPass.FrameGraph = this;
            RenderPassList.Add(renderPass);
        }
      
        public void Draw(RenderView view)
        {
            Profiler.BeginSample("FrameGraph.Draw");
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Draw(view);
            }
            Profiler.EndSample();
        }

        public void Submit(int imageIndex)
        {
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Submit(imageIndex);
            }

        }

    }


    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectVS
    {
        public Matrix Model;
        public Vector4 UOffset;
        public Vector4 VOffset;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SkinVS
    {
        fixed float SkinMatrices[16*64];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialPS
    {
        public Vector4 MatDiffColor;
        public Vector4 MatEmissiveColor;
        public Vector4 MatEnvMapColor;
        public Vector4 MatSpecColor;
        public float cRoughness;
        public float cMetallic;
    }

}
