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

        public FrameGraphPass()
        {
        }

        public virtual void Draw(RenderView view)
        {
        }

        public virtual void Submit(int imageIndex)
        {
        }
    }


}
