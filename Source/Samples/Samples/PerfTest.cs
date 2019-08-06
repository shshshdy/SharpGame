﻿using System;
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
            cameraNode.Position = new Vector3(0, 1000, -1000);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(50), 0, 0);
            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;
            camera.FarClip = 3000.0f;

            {
                var mat = new Material("Shaders/Textured.shader");
                mat.SetTexture("DiffMap", Texture2D.White);

                var model = Resources.Load<Model>("Models/sphere.obj");

                for (int i = 0; i < 40; i++)
                {
                    for (int j = 0; j < 40; j++)
                    {
                        for(int k = 0; k < 5; k++)
                        {
                            var node = scene.CreateChild("Model");
                            node.Position = new Vector3(i * 40 - 20 * 40, 50 * k, j * 40 - 20 * 40);
                            //node.Rotation = Quaternion.FromEuler(0, MathUtil.DegreesToRadians(MathUtil.Random(0, 90)), 0);
                            var staticModel = node.AddComponent<StaticModel>();
                            staticModel.SetModel(model);
                            staticModel.SetMaterial(mat);
                        }
                    }
                }
            }

            Renderer.Instance.MainView.Attach(camera, scene);
            
        }
    }
}
