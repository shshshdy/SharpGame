using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 0)]
    public class PerfTestScene : Sample
    {
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 800, -800);
            //cameraNode.LookAt(Vector3.Zero);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(30), 0, 0);
            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;
            camera.FarClip = 3000.0f;

            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            var resourceLayoutTex = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };

            var shader = new Shader
            {
                new Pass("shaders/Textured.vert.spv", "shaders/Textured.frag.spv")
            };

            {
                var pipeline = new GraphicsPipeline
                {
                    Shader = shader,
                    CullMode = CullMode.Back,
                    FrontFace = FrontFace.CounterClockwise,
                    ResourceLayout = new[] { resourceLayout, resourceLayoutTex },
                    PushConstantRanges = new[]
                    {
                        new PushConstantRange(ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>())
                    }
                };

                var colorMap = Resources.Load<Texture>("textures/StoneDiffuse.png");
                var mat = new Material
                {
                    Pipeline = pipeline,
                    ResourceSet = new ResourceSet(resourceLayoutTex, colorMap)
                };

                var model = Resources.Load<Model>("Models/cube.obj");

                for (int i = 0; i < 100; i++)
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var node = scene.CreateChild("Model");
                        node.Position = new Vector3(i * 12-50*12, 0, j * 12 - 50 * 12);
                        //node.Rotation = Quaternion.FromEuler(0, MathUtil.DegreesToRadians(MathUtil.Random(0, 90)), 0);
                        var staticModel = node.AddComponent<StaticModel>();
                        staticModel.SetModel(model);
                        staticModel.SetMaterial(mat);
                    }
                }
            }

            Renderer.Instance.MainView.Attach(camera, scene);

        }
    }
}
