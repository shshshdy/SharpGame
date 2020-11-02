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

        uint[] inputAttachments = new uint[0];
        uint[] outputAttachments = new uint[2];

        public virtual void Init()
        {
        }

        public virtual void DeviceLost()
        {
        }

        public virtual void DeviceReset()
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
