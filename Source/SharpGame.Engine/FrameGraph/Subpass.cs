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

        public VkPipelineBindPoint pipelineBindPoint = VkPipelineBindPoint.Graphics;

        public uint[] InputAttachments { get; set; } = new uint[0];
        public uint[] OutputAttachments { get; set; } = new uint[1] { 0 };
        public bool DisableDepthStencilAttachment { get; set; } = true;

        public VkSubpassDependency Dependency { get; set; }

        public void GetDescription(VkAttachmentDescription[] attachmentDescriptions, ref SubpassDescription subpassDescription)
        {
            subpassDescription.pipelineBindPoint = pipelineBindPoint;

            if(OutputAttachments != null && OutputAttachments.Length > 0)
            {
                subpassDescription.pColorAttachments = new VkAttachmentReference[OutputAttachments.Length];
                for(int i = 0; i < OutputAttachments.Length; i++)
                {
                    if(Device.IsDepthFormat(attachmentDescriptions[OutputAttachments[i]].format))
                    {
                        Debug.Assert(false);
                    }

                    var initialLayout = attachmentDescriptions[OutputAttachments[i]].initialLayout;
                    var imageLayout = initialLayout == VkImageLayout.Undefined ? VkImageLayout.ColorAttachmentOptimal : initialLayout;
                    subpassDescription.pColorAttachments[i] = new VkAttachmentReference(OutputAttachments[i], imageLayout);
                }
            }

            if (InputAttachments != null && InputAttachments.Length > 0)
            {
                subpassDescription.pColorAttachments = new VkAttachmentReference[InputAttachments.Length];
                for (int i = 0; i < InputAttachments.Length; i++)
                {
                    var initialLayout = attachmentDescriptions[InputAttachments[i]].initialLayout;
                    var imageLayout = initialLayout == VkImageLayout.Undefined ? VkImageLayout.ShaderReadOnlyOptimal : initialLayout;
                    subpassDescription.pInputAttachments[i] = new VkAttachmentReference(InputAttachments[i], imageLayout);
                }
            }

            //todo: subpassDescription.pResolveAttachments


            if (!DisableDepthStencilAttachment)
            {
                var index = Array.FindIndex(attachmentDescriptions, (attachment) => Device.IsDepthFormat(attachment.format));
                if(index != -1)
                {
                    var initialLayout = attachmentDescriptions[index].initialLayout;
                    var imageLayout = initialLayout == VkImageLayout.Undefined ? VkImageLayout.DepthStencilAttachmentOptimal : initialLayout;

                    subpassDescription.pDepthStencilAttachment = new VkAttachmentReference[1]
                    {
                        new VkAttachmentReference((uint)index, imageLayout)
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
