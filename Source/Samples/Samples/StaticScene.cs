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
            camera.FarClip = 3000.0f;

            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            var resourceLayoutTex = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };



            {
                var shader = new Shader
                {
                    new Pass("shaders/Textured.vert.spv", "shaders/Textured.frag.spv")
                    {
                        CullMode = CullMode.Back,
                        FrontFace = FrontFace.CounterClockwise,
                        ResourceLayout = new[] { resourceLayout, resourceLayoutTex },
                        PushConstantRanges = new[]
                        {
                            new PushConstantRange(ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>())
                        }
                    }
                };
                /*
                var pipeline = new Pass
                {
                    Shader = shader,
                    CullMode = CullMode.Back,
                    FrontFace = FrontFace.CounterClockwise,
                    ResourceLayout = new[] { resourceLayout, resourceLayoutTex },
                    PushConstantRanges = new[]
                    {
                        new PushConstantRange(ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>())
                    }
                };*/

                var model = Resources.Load<Model>("Models/plane2.dae");
                var node = scene.CreateChild("Plane");
                node.Scaling = new Vector3(3.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
                ref var m = ref node.WorldTransform;

                var colorMap = Resources.Load<Texture>("textures/StoneDiffuse.png");
                var mat = new Material
                {
                    Shader = shader,
                    ResourceSet = new ResourceSet(resourceLayoutTex, colorMap)
                };

                staticModel.SetMaterial(mat);
            }

            {
                var shader = new Shader
                {
                    new Pass("shaders/Textured.vert.spv", "shaders/Textured.frag.spv")
                    {

                        CullMode = CullMode.Back,
                        FrontFace = FrontFace.Clockwise,
                        ResourceLayout = new[] { resourceLayout, resourceLayoutTex },
                        PushConstantRanges = new[]
                        {
                            new PushConstantRange(ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>())
                        }
                    }
                };

                var colorMap = Resources.Load<Texture>("textures/Mushroom.png");
                var mat = new Material
                {
                    Shader = shader,
                    ResourceSet = new ResourceSet(resourceLayoutTex, colorMap)
                };

                var model = Resources.Load<Model>("Models/Mushroom.mdl");

                for(int i = 0; i < 400; i++)
                {
                    var node = scene.CreateChild("Model");
                    node.Position = new Vector3(MathUtil.Random(-20, 20), 0, MathUtil.Random(-20, 20));
                    node.Rotation = Quaternion.FromEuler(0, MathUtil.DegreesToRadians(MathUtil.Random(0, 90)), 0);
                    var staticModel = node.AddComponent<StaticModel>();
                    staticModel.SetModel(model);
                    staticModel.SetMaterial(mat);
                }
            }

            Renderer.Instance.MainView.Attach(camera, scene);

        }
    }
}
