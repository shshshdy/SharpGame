using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{

    public class View : Object
    {
        public Scene scene;
        public Camera camera;
        public RenderPath renderPath;

        private List<RenderPass> renderPasses = new List<RenderPass>();

        internal PipelineViewportStateCreateInfo viewportStateCreateInfo = new PipelineViewportStateCreateInfo();
        public RenderPass RenderPass { get; }

        public Graphics Graphics => Get<Graphics>();

        public View()
        {
            RenderPass = new ScenePass();
        }

        public void Update()
        {
            int index = Graphics.WorkContext;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                Framebuffer = RenderPass.framebuffer_[index],
                RenderPass = RenderPass.renderPass_
            };

            CommandBuffer cmdBuffer = Graphics.SecondaryCmdBuffers[index].Get();
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit | CommandBufferUsages.RenderPassContinue | CommandBufferUsages.SimultaneousUse
                , inherit
                ));

            RenderPass.Draw(cmdBuffer, index);

            cmdBuffer.End();

        }

        public void Summit(int imageIndex)
        {

            RenderPass.Summit(imageIndex);
        }
    }
}
