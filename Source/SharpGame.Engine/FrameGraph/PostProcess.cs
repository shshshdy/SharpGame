using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class PostProcess : FrameGraphPass
    {
        public bool HDR { get; set; }

        public FastList<PostSubpass> SubPassList { get; } = new FastList<PostSubpass>();

        public RenderTarget InputTarget { get; private set; }


        public struct PostPassInfo
        {
            public RenderPassBeginInfo rpBeginInfo;
            public FastList<PostSubpass> subPassList;

            public void Clear()
            {
                subPassList.Clear();
            }
        }

        public struct PostSubpass
        {
            public uint subpass;
            public Pass pass;
            public ResourceSet resourceSet;
            public ResourceSet resourceSet1;
            public ResourceSet resourceSet2;
            public RenderPass rp;

            public void Draw(CommandBuffer cb)
            {
                var pipe = pass.GetGraphicsPipeline(rp, subpass, null);

                cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet);

                if (resourceSet1 != null)
                {
                    cb.BindGraphicsResourceSet(pass.PipelineLayout, 1, resourceSet1);
                }

                if (resourceSet2 != null)
                {
                    cb.BindGraphicsResourceSet(pass.PipelineLayout, 2, resourceSet2, -1);
                }

                cb.Draw(3, 1, 0, 0);
            }

        }

        PostPassInfo[] postPassInfo = new PostPassInfo[3]
        {
            new PostPassInfo
            {
                subPassList = new FastList<PostSubpass>()
            },

            new PostPassInfo
            {
                subPassList = new FastList<PostSubpass>()
            },

            new PostPassInfo
            {
                subPassList = new FastList<PostSubpass>()
            }
        };

        public override void Init()
        {
            Format fmt = HDR ? Format.R16g16b16a16Sfloat : Format.R8g8b8a8Unorm;
            InputTarget = new RenderTarget(Graphics.Width, Graphics.Height, 1, fmt,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Color,
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);
        }

        public RenderTarget FinalRenderTarget => Renderer.View.RenderTarget;

        public override void Draw(RenderView view)
        {
            ref var rpInfo = ref postPassInfo[Graphics.WorkImage];
            foreach(var subpass in SubPassList)
            {
                rpInfo.subPassList.Add(subpass);
            }

        }

        protected override void Submit(CommandBuffer cb, int imageIndex)
        {
            ref var rpInfo = ref postPassInfo[imageIndex];
            cb.BeginRenderPass(in rpInfo.rpBeginInfo, SubpassContents.Inline);

            for(int i = 0; i < rpInfo.subPassList.Count; i++)
            {                
                if(i != 0)
                {
                    cb.NextSubpass(SubpassContents.Inline);
                }

                rpInfo.subPassList[i].Draw(cb);
            }

            cb.EndRenderPass();
            rpInfo.Clear();
        }


    }
}
