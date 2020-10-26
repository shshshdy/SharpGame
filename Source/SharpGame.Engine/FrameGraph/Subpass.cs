using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Subpass
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

        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;
        public FrameGraphPass FrameGraphPass { get; internal set; }
        public RenderView View => FrameGraphPass.View;

        public CommandBuffer CmdBuffer => FrameGraph.GetWorkCmdBuffer(FrameGraphPass.Queue);

        public virtual void Init()
        {
        }
        
        public virtual void Update()
        {
        }

        public virtual void Draw(CommandBuffer cmd, uint subpass)
        {
        }
    }
}
