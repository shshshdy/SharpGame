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

        public uint subpassIndex = 0;

        public uint[] InputAttachments { get; set; } = new uint[0];
        public uint[] OutputAttachments { get; set; } = new uint[1] { 0 };

        public void GetDescription(ref SubpassDescription subpassDescription)
        {
            //todo:
        }


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

        public virtual void Draw(RenderContext rc, CommandBuffer cmd)
        {
        }
    }
}
