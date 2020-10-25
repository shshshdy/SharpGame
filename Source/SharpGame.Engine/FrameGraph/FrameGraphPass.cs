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
        public PassQueue PassQueue { get; set; } = PassQueue.Graphics;
        public RenderPass RenderPass { get; set; }
        public uint Subpass { get; set; }

        [IgnoreDataMember]
        public RenderPipeline Renderer { get; set; }

        protected CommandBuffer cmdBuffer;
        public CommandBuffer CmdBuffer => cmdBuffer;

        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;


        [IgnoreDataMember]
        public Framebuffer[] Framebuffers { get; set; }
        [IgnoreDataMember]
        public Framebuffer Framebuffer { set => Framebuffers = new[] { value, value, value }; }
        public ClearColorValue[] ClearColorValue { get; set; } = { new ClearColorValue(0.25f, 0.25f, 0.25f, 1) };
        public ClearDepthStencilValue? ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);



        public FrameGraphPass()
        {
        }

        public virtual void Init()
        {
        }

        public virtual void Reset()
        {
        }

        public virtual void Update(RenderView view)
        {
        }

        public virtual void Draw(RenderView view)
        {
        }

        protected virtual void Submit(CommandBuffer cb, int imageIndex)
        {
        }

        public virtual void Shutdown()
        {
        }
    }


}
