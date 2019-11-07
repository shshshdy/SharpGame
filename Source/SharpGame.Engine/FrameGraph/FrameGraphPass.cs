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
        public RenderPipeline RenderPipeline { get; set; }

        protected CommandBuffer cmdBuffer;
        public CommandBuffer CmdBuffer => cmdBuffer;

        public Graphics Graphics => Graphics.Instance;

        public RenderSystem Renderer => RenderSystem.Instance;

        public FrameGraphPass()
        {
        }

        public FrameGraphPass(RenderPipeline renderPipeline)
        {
            renderPipeline.Add(this);
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

        public virtual void Submit(CommandBuffer cb, int imageIndex)
        {
        }

        public virtual void Shutdown()
        {
        }
    }


}
