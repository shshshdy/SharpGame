using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Utf8Json;
//using Utf8Json.Resolvers;
using VulkanCore;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 1)]
    public class TexturedCubeApp : Sample
    {
        private Geometry geometry_;
        private Material material;
        private Pipeline pipeline_;

        private GraphicsBuffer _uniformBuffer;
        private WorldViewProjection _wvp;


        public override void Init()
        {
            this.SubscribeToEvent<BeginRenderPass>(Handle);

            geometry_ = GeometricPrimitive.CreateCube(1.0f, 1.0f, 1.0f);
            
            material = new Material
            {
                Shader = new Shader
                (
                    "Textured",
                    new Pass("Textured.vert.spv", "Textured.frag.spv")
                    {
                        ResourceLayout = new ResourceLayout
                        (
                            new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                            new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
                        )
                    }
                )
            };

            var cubeTexture         = ResourceCache.Load<Texture>("IndustryForgedDark512.ktx").Result;
            _uniformBuffer       = GraphicsBuffer.CreateUniform<WorldViewProjection>(1);

            material.ResourceSet.Bind(0, _uniformBuffer)
                .Bind(1, cubeTexture)
                .UpdateSets();
            
            pipeline_ = new Pipeline();

            JsonSerializer.SetDefaultResolver(Utf8Json.Resolvers.StandardResolver.ExcludeNullSnakeCase);
            {
                byte[] bytes = Utf8Json.JsonSerializer.Serialize(material);
                var json = Utf8Json.JsonSerializer.PrettyPrint(bytes);

                System.IO.File.WriteAllText("test_mat.json", json);
            }

            {
                byte[] bytes = Utf8Json.JsonSerializer.Serialize(geometry_);
                var json = Utf8Json.JsonSerializer.PrettyPrint(bytes);

                System.IO.File.WriteAllText("test_geom.json", json);
            }


        }

        public override void Shutdown()
        {
            geometry_.Dispose();
            material.Dispose();
            pipeline_.Dispose();

            base.Destroy();
        }

        public override void Update()
        {
            const float twoPi      = (float)Math.PI * 2.0f;
            const float yawSpeed   = twoPi / 4.0f;
            const float pitchSpeed = 0.0f;
            const float rollSpeed  = twoPi / 4.0f;

            _wvp.World = Matrix.RotationYawPitchRoll(
                Time.Total * yawSpeed % twoPi,
                Time.Total * pitchSpeed % twoPi,
                Time.Total * rollSpeed % twoPi);
            
            SetViewProjection();
        }

        void Handle(BeginRenderPass e)
        {
            e.renderPass.DrawGeometry(geometry_, pipeline_, material);
        }
        
        private void SetViewProjection()
        {
            const float cameraDistance = 2.5f;
            _wvp.View = Matrix.LookAtLH(-Vector3.UnitZ * cameraDistance, Vector3.Zero, Vector3.UnitY);
            var projection = Matrix.PerspectiveFovLH(
            (float)Math.PI / 4,
            (float)Graphics.Width / Graphics.Height,
            1.0f, 1000.0f);
            _wvp.ViewProj = _wvp.View * projection;

            _uniformBuffer.SetData(ref _wvp);
        }


    }
}
