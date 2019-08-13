using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 8)]
    public class PbrLighting : Sample
    {
        Node node;
        public override void Init()
        {
            base.Init();

            var graphics = Graphics.Instance;
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0.0f, 2.0f, -150);

            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = MathUtil.DegreesToRadians(60);
            camera.AspectRatio = (float)graphics.Width / graphics.Height;

            node = scene.CreateChild("Mesh");

            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel("models/cerberus/cerberus.fbx");

            var colorMap = Texture2D.LoadFromFile("models/cerberus/albedo.ktx", Format.R8g8b8a8Unorm);
            var normalMap = Texture2D.LoadFromFile("models/cerberus/normal.ktx", Format.R8g8b8a8Unorm);
            //var metallicMap = Texture2D.LoadFromFile("models/cerberus/metallic.ktx", Format.R8Unorm);
            //var roughnessMap = Texture2D.LoadFromFile("models/cerberus/roughness.ktx", Format.R8Unorm);
            //var aoMap = Texture2D.LoadFromFile("models/cerberus/ao.ktx", Format.R8Unorm);

            var mat = new Material("Shaders/Pbr.shader");
            mat.SetTexture("albedoMap", colorMap);
            mat.SetTexture("normalMap", normalMap);
            //mat.SetTexture("metallicMap", metallicMap);
            //mat.SetTexture("roughnessMap", roughnessMap);
            //mat.SetTexture("aoMap", aoMap);

            staticModel.SetMaterial(mat);

            Renderer.MainView.Attach(camera, scene);
        }

        public override void Update()
        {
            base.Update();

            node.Yaw(Time.Delta * 0.1f, TransformSpace.LOCAL);
        }

    }
}
