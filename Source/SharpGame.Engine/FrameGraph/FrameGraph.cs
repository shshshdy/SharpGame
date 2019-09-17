using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    public class FrameGraph : Object, IEnumerable<FrameGraphPass>
    {
        public RenderTarget[] RenderTargets { get; set; }

        public List<FrameGraphPass> RenderPassList { get; set; } = new List<FrameGraphPass>();

        public static FrameGraph Simple()
        {
            return new FrameGraph
            {
                new ShadowPass(),
                new ScenePass()
            };
        }

        public FrameGraph()
        {
        }
        
        public void AddGraphicsPass(Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                onDraw
            };

            renderPass.Add(onDraw);

            AddRenderPass(renderPass);
        }

        public void InsertGraphicsPass(int index, Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                onDraw
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

        public void Add(FrameGraphPass renderPass)
        {
            renderPass.FrameGraph = this;
            RenderPassList.Add(renderPass);
        }

        public IEnumerator<FrameGraphPass> GetEnumerator()
        {
            return RenderPassList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)RenderPassList).GetEnumerator();
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

}
