using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;

using ImGuiNET;

namespace SharpGame.Samples
{
    struct UboVS
    {
        public Vector4 lightPos;
    }

    [SampleDesc(sortOrder = -2)]
    public unsafe class Lighting : Sample
    {
        DeviceBuffer ubLight = new DeviceBuffer();

        UboVS uboVS = new UboVS() { lightPos = new Vector4(0.0f, 1.0f, -5.0f, 1.0f) };
        
        Vector3 rotation = new Vector3(-0.5f, 112.75f + 180, 0.0f);
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
//             ubLight = DeviceBuffer.CreateUniformBuffer<UboVS>();
//             mat.SetBuffer("UBO", ubLight);
             staticModel.SetMaterial(mat);

            Renderer.MainView.Attach(camera, scene);

            //ubLight.SetData(ref uboVS);
        }

        public override void Update()
        {
            base.Update();

            node.Yaw(Time.Delta * 0.1f, TransformSpace.LOCAL);

        }

        protected override void Destroy()
        {
            //ubLight.Dispose();

            base.Destroy();
        }

    }
}
