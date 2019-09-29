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

        bool initialized = false;

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

        public void Init()
        {
            if (initialized)
            {
                return;
            }

            foreach(var rp in RenderPassList)
            {
                rp.Init();
            }

            initialized = true;
        }

        public void Shutdown()
        {
            foreach (var rp in RenderPassList)
            {
                rp.Shutdown();
            }

            initialized = false;
        }

        public T Get<T>() where T : FrameGraphPass
        {
            foreach(var rp in RenderPassList)
            {
                if(rp is T)
                {
                    return rp as T;
                }
            }
            return null;
        }


        public void AddGraphicsPass(Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                onDraw
            };
            
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
            };

            AddRenderPass(renderPass);
        }

        public void InsertRenderPass(int index, FrameGraphPass renderPass)
        {
            RenderPassList.Insert(index, renderPass);
            renderPass.FrameGraph = this;

            if (initialized)
            {
                renderPass.Init();
            }
        }

        public void AddRenderPass(FrameGraphPass renderPass)
        {
            RenderPassList.Add(renderPass);
            renderPass.FrameGraph = this;

            if(initialized)
            {
                renderPass.Init();
            }
        }

        public int IndexOf(FrameGraphPass frameGraphPass)
        {
            return RenderPassList.IndexOf(frameGraphPass);
        }

        public void Update(RenderView view)
        {
            Profiler.BeginSample("FrameGraph.Update");
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Update(view);
            }
            Profiler.EndSample();
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

        public void EarlySubmit(int imageIndex)
        {
            foreach (var renderPass in RenderPassList)
            {
                if(renderPass.PassQueue == PassQueue.EarlyGraphics)
                {
                    renderPass.Submit(imageIndex);
                }
            }
        }

        public void Submit(int imageIndex)
        {
            foreach (var renderPass in RenderPassList)
            {
                if(renderPass.PassQueue != PassQueue.EarlyGraphics)
                {
                    renderPass.Submit(imageIndex);
                }
            }

        }

        public void Add(FrameGraphPass renderPass)
        {
            RenderPassList.Add(renderPass);
            renderPass.FrameGraph = this;
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
