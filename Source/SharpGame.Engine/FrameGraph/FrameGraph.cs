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

        public void InsertGraphicsPass(int index, Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                OnDraw = onDraw,
                FrameGraph = this
            };

            InsertRenderPass(index, renderPass);
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

        public void InsertRenderPass(int index, FrameGraphPass renderPass)
        {
            renderPass.FrameGraph = this;
            RenderPassList.Insert(index, renderPass);
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
        public mat4 Model;

        public vec4 UOffset1;
        public vec4 VOffset1;
        public vec4 UOffset2;
        public vec4 VOffset2;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SkinVS
    {
        fixed float SkinMatrices[16*64];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialPS
    {
        public vec4 MatDiffColor;
        public vec4 MatEmissiveColor;
        public vec4 MatEnvMapColor;
        public vec4 MatSpecColor;
        public float cRoughness;
        public float cMetallic;
    }

}
