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
        float lodBias = 1.0f;

        string[] names = {"sphere", "teapot", "torusknot" };
        string[] filenames = { "models/sphere.obj", "models/teapot.dae", "models/torusknot.obj" };
        int selected;
        StaticModel staticModel;

        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 5, -10);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(30), 0, 0);

            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;
            camera.FarClip = 3000.0f;

            var cubeMap = Resources.Load<Texture>("textures/cubemap_yokohama_bc3_unorm.ktx");
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

                var node = scene.CreateChild("Model");
                node.Scaling = new Vector3(0.1f);

                staticModel = node.AddComponent<StaticModel>();
                SetModel(filenames[0]);
                staticModel.SetMaterial(material);

            }

            Renderer.MainView.Attach(camera, scene);

        }

        void SetModel(string filePath)
        {
            var model = Resources.Load<Model>(filePath);

            staticModel.SetModel(model);
        }

        public override void OnGUI()
        {
            if (ImGuiNET.ImGui.Combo("Model", ref selected, names, names.Length))
            {
                SetModel(filenames[selected]);
            }

            if (ImGuiNET.ImGui.SliderFloat("lodBias", ref lodBias, 0, 10))
            {
                material.SetShaderParameter("lodBias", lodBias);
            }

        }

    }
}
