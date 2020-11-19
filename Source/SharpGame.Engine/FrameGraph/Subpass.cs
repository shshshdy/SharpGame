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

        public PipelineBindPoint pipelineBindPoint = PipelineBindPoint.Graphics;

        public uint[] InputAttachments { get; set; } = new uint[0];
        public uint[] OutputAttachments { get; set; } = new uint[1] { 0 };
        public bool DisableDepthStencilAttachment { get; set; } = true;

        public SubpassDependency Dependency { get; set; }

        public void GetDescription(AttachmentDescription[] attachmentDescriptions, ref SubpassDescription subpassDescription)
        {
            subpassDescription.pipelineBindPoint = pipelineBindPoint;

            if(OutputAttachments.Length > 0)
            {
                subpassDescription.pColorAttachments = new AttachmentReference[OutputAttachments.Length];
                for(int i = 0; i < OutputAttachments.Length; i++)
                {
                    if(Device.IsDepthFormat(attachmentDescriptions[OutputAttachments[i]].format))
                    {
                        Debug.Assert(false);
                    }

                    var initialLayout = attachmentDescriptions[OutputAttachments[i]].initialLayout;
                    var imageLayout = initialLayout == ImageLayout.Undefined ? ImageLayout.ColorAttachmentOptimal : initialLayout;
                    subpassDescription.pColorAttachments[i] = new AttachmentReference(OutputAttachments[i], imageLayout);
                }
            }

            if (InputAttachments.Length > 0)
            {
                subpassDescription.pColorAttachments = new AttachmentReference[InputAttachments.Length];
                for (int i = 0; i < InputAttachments.Length; i++)
                {
                    var initialLayout = attachmentDescriptions[InputAttachments[i]].initialLayout;
                    var imageLayout = initialLayout == ImageLayout.Undefined ? ImageLayout.ShaderReadOnlyOptimal : initialLayout;
                    subpassDescription.pInputAttachments[i] = new AttachmentReference(InputAttachments[i], imageLayout);
                }
            }

            //todo: subpassDescription.pResolveAttachments


            if (!DisableDepthStencilAttachment)
            {
                var index = Array.FindIndex(attachmentDescriptions, (attachment) => Device.IsDepthFormat(attachment.format));
                if(index != -1)
                {
                    var initialLayout = attachmentDescriptions[index].initialLayout;
                    var imageLayout = initialLayout == ImageLayout.Undefined ? ImageLayout.DepthStencilAttachmentOptimal : initialLayout;

                    subpassDescription.pDepthStencilAttachment = new AttachmentReference[1]
                    {
                        new AttachmentReference((uint)index, imageLayout)
                    };

                }
                else
                {
                    Debug.Assert(false);
                }
            }
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
