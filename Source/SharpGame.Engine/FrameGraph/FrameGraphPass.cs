using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SharpGame
{
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

        public RenderPass renderPass { get; set; }

        [IgnoreDataMember]
        public FrameGraph FrameGraph { get; set; }

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

        public void Preappend(FrameGraphPass frameGraphPass)
        {
            int index = FrameGraph.IndexOf(this);
            if(index != -1)
            {
                FrameGraph.InsertRenderPass(index, frameGraphPass);
            }
            else
            {
                Log.Assert("Not in FrameGraph");
            }

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
                Log.Assert("Not in FrameGraph");
            }

        }

        public virtual void Update(RenderView view)
        {
        }

        public virtual void Draw(RenderView view)
        {
        }

        public virtual void Submit(int imageIndex)
        {
        }

        public virtual void Shutdown()
        {
        }
    }


}
