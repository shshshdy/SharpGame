using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SharpGame
{
    public enum PassQueue
    {
        None,
        EarlyGraphics = 1,
        Compute = 2,
        Graphics = 4,
        All = 7
    }

    public class FrameGraphPass : Object
    {
        private StringID name;
        public StringID Name
        {
            get => name;
            set
            {
                name = value;
                passID = Pass.GetID(name);
            }
        }

        protected ulong passID = 1;
        public PassQueue PassQueue { get; set; }
        public RenderPass RenderPass { get; set; }
        public uint Subpass { get; set; }

        [IgnoreDataMember]
        public FrameGraph FrameGraph
        {
            get => frameGraph;
            set
            {
                frameGraph = value;
                OnSetFrameGraph(frameGraph);
            }
        }

        FrameGraph frameGraph;

        protected CommandBuffer cmdBuffer;
        public CommandBuffer CmdBuffer => cmdBuffer;

        public Graphics Graphics => Graphics.Instance;

        public Renderer Renderer => Renderer.Instance;

        public FrameGraphPass()
        {
        }

        public virtual void Init()
        {
        }

        public virtual void Reset()
        {
        }

        protected virtual void OnSetFrameGraph(FrameGraph frameGraph)
        {
        }

        public GraphicsPass PreappendGraphicsPass(string name, int workCount, Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass(name, workCount)
            {
                OnDraw = onDraw
            };

            Preappend(renderPass);
            return renderPass;
        }

        public ComputePass PreappendComputePass(Action<ComputePass, RenderView> onDraw)
        {
            var renderPass = new ComputePass
            {
                OnDraw = onDraw
            };

            Preappend(renderPass);
            return renderPass;
        }

        public void Preappend(FrameGraphPass frameGraphPass)
        {
            int index = FrameGraph.IndexOf(this);
            if(index != -1)
            {
                FrameGraph.InsertRenderPass(index, frameGraphPass);
            }
            else
            {
                Debug.Assert(false, "Not in FrameGraph");
            }

        }

        public void AppendGraphicsPass(string name, int workCount, Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass(name, workCount)
            {
                OnDraw = onDraw
            };

            Append(renderPass);
        }

        public void Append(FrameGraphPass frameGraphPass)
        {
            int index = FrameGraph.IndexOf(this);
            if (index != -1)
            {
                FrameGraph.InsertRenderPass(index + 1, frameGraphPass);
            }
            else
            {
                Debug.Assert(false, "Not in FrameGraph");
            }

        }

        public virtual void Update(RenderView view)
        {
        }

        public virtual void Draw(RenderView view)
        {
        }

        public virtual void Submit(CommandBuffer cb, int imageIndex)
        {
        }

        public virtual void Shutdown()
        {
        }
    }


}
