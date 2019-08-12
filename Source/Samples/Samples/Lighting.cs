using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;

using ImGuiNET;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -2)]
    public unsafe class Lighting : Sample
    {
        Node node;
        public override void Init()
        {
            base.Init();

            var graphics = Graphics.Instance;
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0.0f, 2.0f, -5);
            cameraNode.LookAt(Vector3.Zero);

            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = MathUtil.DegreesToRadians(60);
            camera.AspectRatio = (float)graphics.Width / graphics.Height;

            node = scene.CreateChild("Mesh");
            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel("models/voyager/voyager.dae");

            var colorMap = Resources.Load<Texture2D>("models/voyager/voyager_bc3_unorm.ktx");

            var mat = new Material("Shaders/LitSolid.shader");
            mat.SetTexture("DiffMap", colorMap);
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
