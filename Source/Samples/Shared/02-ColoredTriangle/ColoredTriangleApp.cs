using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.ColoredTriangle
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WorldViewProjection
    {
        public Matrix World;
        public Matrix View;
        public Matrix Projection;
        public Matrix ViewProj;
        public Matrix WorldViewProj;
    }

    public class ColoredTriangleApp : Application
    {
        private Pipeline pipeline_;
        private Shader testShader_;

        Node node_;
        Node cameraNode_;
        Model model_;



        private DescriptorSetLayout _descriptorSetLayout;
        private DescriptorPool _descriptorPool;
        private DescriptorSet _descriptorSet;

        private Texture _cubeTexture;

        private GraphicsBuffer _uniformBuffer;
        private WorldViewProjection _wvp;
        Geometry geometry_;

        protected override void OnInit()
        {
            SubscribeToEvent<BeginRenderPass>(Handle);

            _cubeTexture = resourceCache_.Load<Texture>("IndustryForgedDark512.ktx").Result;
            _uniformBuffer = GraphicsBuffer.DynamicUniform<WorldViewProjection>(1);

            _descriptorSetLayout = CreateDescriptorSetLayout();
            _descriptorPool = CreateDescriptorPool();
            _descriptorSet = CreateDescriptorSet();

            testShader_ = new Shader
            {
                Name = "Test",
                ["main"] = new Pass("Textured.vert.spv", "Textured.frag.spv")
            };

            pipeline_ = new Pipeline
            {
                //   FrontFace = FrontFace.CounterClockwise
                PipelineLayoutInfo = new PipelineLayoutCreateInfo(new[] { _descriptorSetLayout })
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

            model_ = resourceCache_.Load<Model>("Models/Mushroom.mdl").Result;

            geometry_ = GeometricPrimitive.Create(1.0f, 1.0f, 1.0f);
        }

        public override void Dispose()
        {
            testShader_.Dispose();
            pipeline_.Dispose();

            base.Dispose();
        }


        protected override void Update(Timer timer)
        {
            const float twoPi = (float)System.Math.PI * 2.0f;
            const float yawSpeed = twoPi / 4.0f;
            const float pitchSpeed = 0.0f;
            const float rollSpeed = twoPi / 4.0f;

            _wvp.World = Matrix.RotationYawPitchRoll(
                timer.TotalTime * yawSpeed % twoPi,
                timer.TotalTime * pitchSpeed % twoPi,
                timer.TotalTime * rollSpeed % twoPi);

            SetViewProjection();

            UpdateUniformBuffers();
        }

        private void SetViewProjection()
        {
            const float cameraDistance = 2.5f;
            _wvp.View = Matrix.LookAtLH(-Vector3.UnitZ * cameraDistance, Vector3.Zero, Vector3.UnitY);
            _wvp.Projection = Matrix.PerspectiveFovLH(
            (float)System.Math.PI / 4,
            (float)graphics_.Platform.Width / graphics_.Platform.Height,
            1.0f, 1000.0f);
            _wvp.ViewProj = _wvp.View * _wvp.Projection;
            _wvp.WorldViewProj = _wvp.World * _wvp.View * _wvp.Projection;
        }

        private void UpdateUniformBuffers()
        {
            IntPtr ptr = _uniformBuffer.Map(0, Interop.SizeOf<WorldViewProjection>());
            Interop.Write(ptr, ref _wvp);
            _uniformBuffer.Unmap();
        }

        void Handle(BeginRenderPass e)
        {
            var cmdBuffer = e.commandBuffer;
            var geo = model_.GetGeometry(0, 0);
            var pipeline = pipeline_.GetGraphicsPipeline(e.renderPass, testShader_, geo);
            //cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            //cmdBuffer.CmdDraw(3);

            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline_.pipelineLayout, _descriptorSet);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            geo.Draw(cmdBuffer);
        }


        private DescriptorPool CreateDescriptorPool()
        {
            var descriptorPoolSizes = new[]
            {
                new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
            };
            return graphics_.CreateDescriptorPool(descriptorPoolSizes);
        }

        private DescriptorSet CreateDescriptorSet()
        {
            DescriptorSet descriptorSet = _descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(1, _descriptorSetLayout))[0];
            // Update the descriptor set for the shader binding point.
            var writeDescriptorSets = new[]
            {
                new WriteDescriptorSet(descriptorSet, 0, 0, 1, DescriptorType.UniformBuffer,
                    bufferInfo: new[] { new DescriptorBufferInfo(_uniformBuffer) }),
                new WriteDescriptorSet(descriptorSet, 1, 0, 1, DescriptorType.CombinedImageSampler,
                    imageInfo: new[] { new DescriptorImageInfo(_cubeTexture.Sampler, _cubeTexture.View, ImageLayout.General) })
            };
            _descriptorPool.UpdateSets(writeDescriptorSets);
            return descriptorSet;
        }

        private DescriptorSetLayout CreateDescriptorSetLayout()
        {
            return graphics_.CreateDescriptorSetLayout(
                new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment));
        }
    }
}
