using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SharpGame
{
    public class FGPass : Object
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

        public ulong passID;

        [IgnoreDataMember]
        public FrameGraph FrameGraph { get; set; }

        protected CommandBuffer cmdBuffer;
        public CommandBuffer CmdBuffer => cmdBuffer;

        protected RenderPass renderPass;

        public FGPass()
        {
        }

        public virtual void Draw(RenderView view)
        {
        }

        public virtual void Summit(int imageIndex)
        {
        }
    }


}
