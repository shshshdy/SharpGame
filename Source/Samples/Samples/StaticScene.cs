using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 1)]
    public class StaticScene : Sample
    {

        public override void Init()
        {
            var graphics = Graphics.Instance;

            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 0, -5);
            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)graphics.Width / graphics.Height;

            var model = ResourceCache.Instance.Load<Model>("Models/Mushroom.mdl");

            var node = scene.CreateChild("Model");
            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel(model);

            //_cubeTexture = ResourceCache.Load<Texture>("IndustryForgedDark512.ktx").Result;

            var mat = new Material();
            mat.Shader = new Shader
            {
                new Pass("shaders/Textured.vert.spv", "shaders/Textured.frag.spv")
                {
                    ResourceLayout = new []
                    {
                        new ResourceLayout
                        {
                            new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                            new ResourceLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment)
                        }
                    }
                }
            };
            //mat.SetTexture("sampler_Color", _cubeTexture);
            staticModel.SetMaterial(0, mat);

            Renderer.Instance.MainView.Attach(camera, scene);

        }
    }
}
