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
            cameraNode.Position = new Vector3(0, 0, -3);
            cameraNode.LookAt(Vector3.Zero);

            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)graphics.Width / graphics.Height;

            var model = ResourceCache.Instance.Load<Model>("Models/Mushroom.mdl");

            var node = scene.CreateChild("Model");
            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel(model);

            var colorMap = new Texture2D();
            colorMap.LoadFromFile("models/voyager/voyager_bc3_unorm.ktx",
                    Format.Bc3UnormBlock);

            var resourceLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            var resourceLayoutTex = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };

            var mat = new Material
            {
                Shader = new Shader
                {
                    new Pass("shaders/Textured.vert.spv", "shaders/Textured.frag.spv")
                },

                Pipeline = new Pipeline
                {
                    CullMode = CullMode.None,
                    FrontFace = FrontFace.Clockwise,

                    ResourceLayout = new[] { resourceLayout, resourceLayoutTex }
                },

                ResourceSet = new ResourceSet(resourceLayoutTex, colorMap)
            };

            //mat.SetTexture("sampler_Color", _cubeTexture);
            staticModel.SetMaterial(0, mat);

            Renderer.Instance.MainView.Attach(camera, scene);

        }
    }
}
