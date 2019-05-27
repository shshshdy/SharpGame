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
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 20, -30);
            //cameraNode.LookAt(Vector3.Zero);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(30), 0, 0);

            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;

            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            var resourceLayoutTex = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };

            var pipeline = new GraphicsPipeline
            {
                CullMode = CullMode.None,
                FrontFace = FrontFace.Clockwise,
                ResourceLayout = new[] { resourceLayout, resourceLayoutTex },
                PushConstantRanges = new []
                {
                    new PushConstantRange(ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>())
                }
            };

            var shader = new Shader
            {
                new Pass("shaders/Textured.vert.spv", "shaders/Textured.frag.spv")
            };
           /*
            {
                var model = ResourceCache.Load<Model>("Models/Plane.obj");
                var node = scene.CreateChild("Plane");
                node.Scaling = new Vector3(2, 2, 2);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);

                var colorMap = ResourceCache.Load<Texture>("textures/StoneDiffuse.png");
                var mat = new Material
                {
                    Shader = shader,
                    Pipeline = pipeline,
                    ResourceSet = new ResourceSet(resourceLayoutTex, colorMap)
                };

                staticModel.SetMaterial(0, mat);
            }*/

            {
                var colorMap = ResourceCache.Load<Texture>("textures/Mushroom.png");
                var mat = new Material
                {
                    Shader = shader,
                    Pipeline = pipeline,
                    ResourceSet = new ResourceSet(resourceLayoutTex, colorMap)
                };

                var model = ResourceCache.Load<Model>("Models/Mushroom.mdl");

                for(int i = 0; i < 100; i++)
                {
                    var node = scene.CreateChild("Model");
                    node.Position = new Vector3(MathUtil.Random(-20, 20), 0, MathUtil.Random(-20, 20));
                    var staticModel = node.AddComponent<StaticModel>();
                    staticModel.SetModel(model);
                    staticModel.SetMaterial(0, mat);
                }
            }

            Renderer.Instance.MainView.Attach(camera, scene);

        }
    }
}
