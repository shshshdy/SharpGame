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
        Texture2D colorMap = new Texture2D();

        DeviceBuffer ubLight = new DeviceBuffer();

        UboVS uboVS = new UboVS() { lightPos = new Vector4(0.0f, 1.0f, -5.0f, 1.0f) };
        
        Vector3 rotation = new Vector3(-0.5f, 112.75f + 180, 0.0f);
     
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

            var node = scene.CreateChild("Mesh");
            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel("models/voyager/voyager.dae");

            LoadMesh();

            var mat = new Material("Shaders/LitSolid.shader");
            mat.SetTexture("DiffMap", colorMap);
            ubLight = DeviceBuffer.CreateUniformBuffer<UboVS>();
            mat.SetBuffer("UBO", ubLight);
            staticModel.SetMaterial(mat);

            Renderer.Instance.MainView.Attach(camera, scene);
        }

        void LoadMesh()
        {
            if (Device.Features.textureCompressionBC == 1)
            {
                colorMap.LoadFromFile("models/voyager/voyager_bc3_unorm.ktx",
                    Format.Bc3UnormBlock);
            }
            else if (Device.Features.textureCompressionASTC_LDR == 1)
            {
                colorMap.LoadFromFile("models/voyager/voyager_astc_8x8_unorm.ktx", Format.Astc8x8UnormBlock);
            }
            else if (Device.Features.textureCompressionETC2 == 1)
            {
                colorMap.LoadFromFile("models/voyager/voyager_etc2_unorm.ktx", Format.Etc2R8g8b8a8UnormBlock);
            }
            else
            {
                throw new InvalidOperationException("Device does not support any compressed texture format!");
            }
        }

        public override void Update()
        {
            base.Update();

            rotation.Y += Time.Delta * 10;

            ubLight.SetData(ref uboVS);
        }

        protected override void Destroy()
        {
            colorMap.Dispose();
            ubLight.Dispose();

            base.Destroy();
        }

    }
}
