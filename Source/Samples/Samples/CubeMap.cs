using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 7)]

    public class CubeMap : Sample
    {
        Material material;
        private float lodBias = 1.0f;
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 5, -10);
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
                material = new Material("Shaders/Reflect.shader");
                material.SetTexture("samplerColor", cubeMap);
                material.SetShaderParameter("lodBias", lodBias);

                List<string> filenames = new List<string> { "models/sphere.obj"/*, "models/teapot.dae", "models/torusknot.obj"*/ };
                foreach (string file in filenames)
                {
                    var model = Resources.Load<Model>(file);

                    var node = scene.CreateChild("Model");
                    node.Scaling = new Vector3(0.1f);

                    var staticModel = node.AddComponent<StaticModel>();
                    staticModel.SetModel(model);
                    staticModel.SetMaterial(material);
                }
            }

            Renderer.MainView.Attach(camera, scene);

        }

        public override void OnGUI()
        {
            if(ImGui.SliderFloat("lodBias", ref lodBias, 0, 10))
            {
                material.SetShaderParameter("lodBias", lodBias);
            }
        }

    }
}
