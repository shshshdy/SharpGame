using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 9)]
    public class PbrLighting : Sample
    {
        public override void Init()
        {
            base.Init();

            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(80.0f, 0.0f, -150);
            cameraNode.Rotate(Quaternion.FromEuler(0, MathUtil.DegreesToRadians(-45), 0), TransformSpace.LOCAL);
            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = MathUtil.DegreesToRadians(60);
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;

            var cubeMap = TextureCube.LoadFromFile("textures/hdr/gcanyon_cube.ktx", Format.R16g16b16a16Sfloat);
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
                var node = scene.CreateChild("Mesh");

                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel("models/cerberus/cerberus.fbx");

                var colorMap = Texture2D.LoadFromFile("models/cerberus/albedo.ktx", Format.R8g8b8a8Unorm);
                var normalMap = Texture2D.LoadFromFile("models/cerberus/normal.ktx", Format.R8g8b8a8Unorm);
                var metallicMap = Texture2D.LoadFromFile("models/cerberus/metallic.ktx", Format.R8Unorm);
                var roughnessMap = Texture2D.LoadFromFile("models/cerberus/roughness.ktx", Format.R8Unorm);
                var aoMap = Texture2D.LoadFromFile("models/cerberus/ao.ktx", Format.R8Unorm);

                var mat = new Material("Shaders/Pbr.shader");
                mat.SetTexture("albedoMap", colorMap);
                mat.SetTexture("normalMap", normalMap);
                mat.SetTexture("metallicMap", metallicMap);
                mat.SetTexture("roughnessMap", roughnessMap);
                mat.SetTexture("aoMap", aoMap);

                staticModel.SetMaterial(mat);
            }


            Renderer.MainView.Attach(camera, scene);
        }

    }
}
