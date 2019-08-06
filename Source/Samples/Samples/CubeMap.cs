using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 7)]

    public class CubeMap : Sample
    {
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 5, -10);
            //cameraNode.LookAt(Vector3.Zero);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(30), 0, 0);

            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;
            camera.FarClip = 3000.0f;

            var cubeMap = Resources.Load<TextureCube>("textures/cubemap_yokohama_bc3_unorm.ktx");
            {
                var model = Resources.Load<Model>("Models/cube.obj");
                var node = scene.CreateChild("Sky");
                node.Scaling = new Vector3(30.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);

                var mat = new Material("Shaders/Skybox.shader");
                mat.SetTexture("samplerCubeMap", cubeMap);

                staticModel.SetMaterial(mat);
            }
                        
            {
                var mat = new Material("Shaders/Reflect.shader");
                mat.SetTexture("samplerColor", cubeMap);

                List<string> filenames = new List<string> { "models/sphere.obj"/*, "models/teapot.dae", "models/torusknot.obj"*/ };
                foreach (string file in filenames)
                {

                    var model = Resources.Load<Model>(file);

                    var node = scene.CreateChild("Model");
                    node.Scaling = new Vector3(0.1f);
                    //node.Position = new Vector3(i * 40 - 20 * 40, 50 * k, j * 40 - 20 * 40);
                    //node.Rotation = Quaternion.FromEuler(0, MathUtil.DegreesToRadians(MathUtil.Random(0, 90)), 0);
                    var staticModel = node.AddComponent<StaticModel>();
                    staticModel.SetModel(model);

                    staticModel.SetMaterial(mat);
                }
            }

            Renderer.Instance.MainView.Attach(camera, scene);

        }
    }
}
