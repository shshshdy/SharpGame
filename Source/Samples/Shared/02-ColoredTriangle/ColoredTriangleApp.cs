using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.ColoredTriangle
{
    public class ColoredTriangleApp : Application
    {
        private RenderPass _renderPass;
        private ImageView[] _imageViews;
        private Framebuffer[] _framebuffers;
        private PipelineLayout _pipelineLayout;
        private Pipeline _pipeline;

        protected override void InitializePermanent()
        {
            _renderPass     = Graphics.CreateRenderPass();
            _pipelineLayout = ToDispose(CreatePipelineLayout());
        }

        protected override void InitializeFrame()
        {
            _imageViews   = ToDispose(CreateImageViews());
            _framebuffers = ToDispose(CreateFramebuffers());
            _pipeline     = Pipeline.Create(_pipelineLayout, _renderPass);
        }

        private ImageView[] CreateImageViews()
        {
            var imageViews = new ImageView[Graphics.SwapchainImages.Length];
            for (int i = 0; i < Graphics.SwapchainImages.Length; i++)
            {
                imageViews[i] = Graphics.SwapchainImages[i].CreateView(new ImageViewCreateInfo(
                    Graphics.Swapchain.Format,
                    new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1)));
            }
            return imageViews;
        }

        private Framebuffer[] CreateFramebuffers()
        {
            var framebuffers = new Framebuffer[Graphics.SwapchainImages.Length];
            for (int i = 0; i < Graphics.SwapchainImages.Length; i++)
            {
                framebuffers[i] = _renderPass.CreateFramebuffer(new FramebufferCreateInfo(
                    new[] { _imageViews[i] }, 
                    Platform.Width, 
                    Platform.Height));
            }
            return framebuffers;
        }

        private PipelineLayout CreatePipelineLayout()
        {
            var layoutCreateInfo = new PipelineLayoutCreateInfo();
            return Graphics.Device.CreatePipelineLayout(layoutCreateInfo);
        }
        /*
        private Pipeline CreateGraphicsPipeline()
        {
            ShaderModule vertexShader   = ResourceCache.Load<ShaderModule>("Shader.vert.spv");
            ShaderModule fragmentShader = ResourceCache.Load<ShaderModule>("Shader.frag.spv");
            var shaderStageCreateInfos = new[]
            {
                new PipelineShaderStageCreateInfo(ShaderStages.Vertex, vertexShader, "main"),
                new PipelineShaderStageCreateInfo(ShaderStages.Fragment, fragmentShader, "main")
            };

            var vertexInputStateCreateInfo = new PipelineVertexInputStateCreateInfo();
            var inputAssemblyStateCreateInfo = new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology.TriangleList);
            var viewportStateCreateInfo = new PipelineViewportStateCreateInfo(
                new Viewport(0, 0, Platform.Width, Platform.Height),
                new Rect2D(0, 0, Platform.Width, Platform.Height));
            var rasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
            {
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModes.Back,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            };
            var multisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
            {
                RasterizationSamples = SampleCounts.Count1,
                MinSampleShading = 1.0f
            };
            var colorBlendAttachmentState = new PipelineColorBlendAttachmentState
            {
                SrcColorBlendFactor = BlendFactor.One,
                DstColorBlendFactor = BlendFactor.Zero,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOp = BlendOp.Add,
                ColorWriteMask = ColorComponents.All
            };
            var colorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo(
                new[] { colorBlendAttachmentState });

            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                _pipelineLayout, _renderPass, 0,
                shaderStageCreateInfos,
                inputAssemblyStateCreateInfo,
                vertexInputStateCreateInfo,
                rasterizationStateCreateInfo,
                viewportState: viewportStateCreateInfo,
                multisampleState: multisampleStateCreateInfo,
                colorBlendState: colorBlendStateCreateInfo);
            return Graphics.Device.CreateGraphicsPipeline(pipelineCreateInfo);
        }
        */
        protected override void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex)
        {
            var renderPassBeginInfo = new RenderPassBeginInfo(
                _framebuffers[imageIndex],
                new Rect2D(Offset2D.Zero, new Extent2D(Platform.Width, Platform.Height)),
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)));

            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, _pipeline.pipeline);
            cmdBuffer.CmdDraw(3);
            cmdBuffer.CmdEndRenderPass();
        }
    }
}
