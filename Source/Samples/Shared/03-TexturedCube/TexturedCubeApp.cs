using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
//using Utf8Json;
//using Utf8Json.Resolvers;
using VulkanCore;

namespace SharpGame.Samples.TexturedCube
{

    public class TexturedCubeApp : Application
    {
        private Geometry geometry_;
        private Pipeline pipeline_;
        private Shader texturedShader_;

        private ResourceLayout _descriptorSetLayout;
        private ResourceSet _descriptorSet;

        private Texture _cubeTexture;

        private GraphicsBuffer _uniformBuffer;
        private WorldViewProjection _wvp;


        protected override void OnInit()
        {
            this.SubscribeToEvent<BeginRenderPass>(Handle);

            geometry_ = GeometricPrimitive.CreateCube(1.0f, 1.0f, 1.0f);

            texturedShader_ = new Shader
            (
                Name = "Textured",

                new Pass("Textured.vert.spv", "Textured.frag.spv")
                {
                    ResourceLayout = new ResourceLayout
                    (
                        new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                        new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
                    )
                }
            );

            _cubeTexture         = resourceCache_.Load<Texture>("IndustryForgedDark512.ktx").Result;
            _uniformBuffer       = UniformBuffer.Create<WorldViewProjection>(1);

            _descriptorSetLayout = new ResourceLayout
            (
                new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
            );
            
            CreateDescriptorSet();

            pipeline_ = new Pipeline
            {
                PipelineLayoutInfo = new PipelineLayoutCreateInfo(new[] { _descriptorSetLayout.descriptorSetLayout }),
                VertexInputState = PosNormTex.Layout,
                FrontFace = FrontFace.CounterClockwise
            };

            /*
            JsonSerializer.SetDefaultResolver(StandardResolver.ExcludeNullSnakeCase);
            {
                byte[] bytes = Utf8Json.JsonSerializer.Serialize(texturedShader_);
                var json = Utf8Json.JsonSerializer.PrettyPrint(bytes);

                File.WriteAllText("test_shader.json", json);
            }

            {
                byte[] bytes = Utf8Json.JsonSerializer.Serialize(geometry_);
                var json = Utf8Json.JsonSerializer.PrettyPrint(bytes);

                File.WriteAllText("test_geom.json", json);
            }*/


        }

        protected override void Destroy()
        {
            geometry_.Dispose();
            texturedShader_.Dispose();
            pipeline_.Dispose();

            base.Destroy();
        }

        protected override void Update(Timer timer)
        {
            const float twoPi      = (float)Math.PI * 2.0f;
            const float yawSpeed   = twoPi / 4.0f;
            const float pitchSpeed = 0.0f;
            const float rollSpeed  = twoPi / 4.0f;

            _wvp.World = Matrix.RotationYawPitchRoll(
                timer.TotalTime * yawSpeed % twoPi,
                timer.TotalTime * pitchSpeed % twoPi,
                timer.TotalTime * rollSpeed % twoPi);
            
            SetViewProjection();
        }

        void Handle(BeginRenderPass e)
        {
            var cmdBuffer = e.commandBuffer;

            var pipeline = pipeline_.GetGraphicsPipeline(e.renderPass, texturedShader_, geometry_);
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline_.pipelineLayout, _descriptorSet.descriptorSet);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            geometry_.Draw(cmdBuffer);
        }
        
        private void SetViewProjection()
        {
            const float cameraDistance = 2.5f;
            _wvp.View = Matrix.LookAtLH(-Vector3.UnitZ * cameraDistance, Vector3.Zero, Vector3.UnitY);
            var projection = Matrix.PerspectiveFovLH(
            (float)Math.PI / 4,
            (float)graphics_.GameWindow.Width / graphics_.GameWindow.Height,
            1.0f, 1000.0f);
            _wvp.ViewProj = _wvp.View * projection;

            _uniformBuffer.SetData(ref _wvp);
        }

        private void CreateDescriptorSet()
        {
            _descriptorSet = new ResourceSet(_descriptorSetLayout);
            _descriptorSet.Bind(0, _uniformBuffer)
                .Bind(1, _cubeTexture)
                .UpdateSets();
          

        }

    }
}
