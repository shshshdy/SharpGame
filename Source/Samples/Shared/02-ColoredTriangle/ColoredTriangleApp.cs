using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.ColoredTriangle
{
    public class ColoredTriangleApp : Application
    {
        private Pipeline pipeline_;
        private Shader testShader_;

        Node node_;
        Node cameraNode_;

        protected override void OnInit()
        {
            SubscribeToEvent<BeginRenderPass>(Handle);

            testShader_ = new Shader
            {
                Name = "Test",
                ["main"] = new Pass("Test.vert.spv", "Test.frag.spv")
            };

            pipeline_ = new Pipeline
            {
                FrontFace = FrontFace.CounterClockwise
            };

            node_ = new Node
            {
                Position = new Vector3(0, 0, 0)
            };

            cameraNode_ = new Node
            {
                Position = new Vector3(0, 0, -3)
            };

            var cam = node_.AddComponent<Camera>();
            cameraNode_.LookAt(Vector3.Zero);

            Model model = resourceCache_.Load<Model>("Models/Mushroom.mdl").Result;
        }

        public override void Dispose()
        {
            testShader_.Dispose();
            pipeline_.Dispose();

            base.Dispose();
        }


        void Handle(BeginRenderPass e)
        {
            var cmdBuffer = e.commandBuffer;

            var pipeline = pipeline_.GetGraphicsPipeline(e.renderPass, testShader_, null);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            cmdBuffer.CmdDraw(3);
        }
    }
}
