﻿using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 3)]
    public class Sponza : Sample
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

                var model = Resources.Load<Model>("Models/crysponza_bubbles/sponza.obj");
                var node = scene.CreateChild("Plane");
                node.Scaling = new Vector3(1.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
            }
            
            Renderer.MainView.Attach(camera, scene);

        }
    }
}
