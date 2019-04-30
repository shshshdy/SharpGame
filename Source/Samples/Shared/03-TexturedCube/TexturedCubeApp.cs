using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
//using Utf8Json;
//using Utf8Json.Resolvers;
using VulkanCore;

namespace SharpGame.Samples.TexturedCube
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

    public class TexturedCubeApp : Application
    {
        private Geometry geometry_;
        private Pipeline pipeline_;
        private Shader texturedShader_;

        private DescriptorSetLayout _descriptorSetLayout;
        private DescriptorPool _descriptorPool;
        private DescriptorSet _descriptorSet;        

        private Texture _cubeTexture;

        private GraphicsBuffer _uniformBuffer;
        private WorldViewProjection _wvp;


        protected override void OnInit()
        {
            SubscribeToEvent<BeginRenderPass>(Handle);

            geometry_ = GeometricPrimitive.CreateCube(1.0f, 1.0f, 1.0f);

            texturedShader_ = new Shader
            {
                Name = "Textured",
                ["main"] = new Pass("Textured.vert.spv", "Textured.frag.spv")
            };

            _cubeTexture         = resourceCache_.Load<Texture>("IndustryForgedDark512.ktx").Result;
            _uniformBuffer       = UniformBuffer.Create<WorldViewProjection>(1);

            _descriptorSetLayout = CreateDescriptorSetLayout();
            _descriptorPool      = CreateDescriptorPool();
            _descriptorSet       = CreateDescriptorSet();


            pipeline_ = new Pipeline
            {
                PipelineLayoutInfo = new PipelineLayoutCreateInfo(new[] { _descriptorSetLayout }),
                VertexInputState = PosNormTex.Layout
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

        public override void Dispose()
        {
            geometry_.Dispose();
            texturedShader_.Dispose();
            pipeline_.Dispose();

            base.Dispose();
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

            UpdateUniformBuffers();
        }

        void Handle(BeginRenderPass e)
        {
            var cmdBuffer = e.commandBuffer;

            var pipeline = pipeline_.GetGraphicsPipeline(e.renderPass, texturedShader_, geometry_);
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline_.pipelineLayout, _descriptorSet);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            geometry_.Draw(cmdBuffer);
        }
        
        private void SetViewProjection()
        {
            const float cameraDistance = 2.5f;
            _wvp.View = Matrix.LookAtLH(-Vector3.UnitZ * cameraDistance, Vector3.Zero, Vector3.UnitY);
            _wvp.Projection = Matrix.PerspectiveFovLH(
            (float)Math.PI / 4,
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
