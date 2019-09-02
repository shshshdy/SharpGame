using SharpGame;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


namespace SharpGame.Samples
{
    using static SharpGame.glm;

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraVS
    {
        public mat4 View;
        public mat4 ViewInv;
        public mat4 ViewProj;
        public vec3 CameraPos;
        public float NearClip;
        public vec3 FrustumSize;
        public float FarClip;
    }

    [SampleDesc(sortOrder = 6)]
    public class CustomRender : Sample
    {
        FrameGraph frameGraph = new FrameGraph();
        List<SourceBatch> batches = new List<SourceBatch>();

        CameraVS cameraVS = new CameraVS();

        ResourceSet[] resourceSet = new ResourceSet[2];

        DeviceBuffer[] ubCameraVS = new DeviceBuffer[2];
        DeviceBuffer[] ubObjectVS = new DeviceBuffer[2];

        Geometry cube;
        vec3 cameraPos;
        const int COUNT = 100;

        public override void Init()
        {
            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                new ResourceLayoutBinding(1, DescriptorType.UniformBufferDynamic, ShaderStage.Vertex),
            };

            var mat = new Material("Shaders/Basic.shader");
            mat.SetTexture("DiffMap", Texture.White);

            cube = GeometricPrimitive.CreateCube(10, 10, 10);

            for(int i = 0; i < COUNT; i++)
            {
                var batch = new SourceBatch
                {
                    geometry = cube,
                    material = mat,
                    numWorldTransforms = 1,
                };

                batches.Add(batch);
            }

            ubCameraVS[0] = DeviceBuffer.CreateUniformBuffer<CameraVS>();
            ubCameraVS[1] = DeviceBuffer.CreateUniformBuffer<CameraVS>();

            ubObjectVS[0] = DeviceBuffer.CreateUniformBuffer<mat4>(COUNT*4);
            ubObjectVS[0].Map();

            ubObjectVS[1] = DeviceBuffer.CreateUniformBuffer<mat4>(COUNT * 4);
            ubObjectVS[1].Map();

            resourceSet[0] = new ResourceSet(resourceLayout, ubCameraVS[0], ubObjectVS[0]);
            resourceSet[1] = new ResourceSet(resourceLayout, ubCameraVS[1], ubObjectVS[1]);

            frameGraph.AddGraphicsPass(CustomDraw);

            cameraPos = new vec3(0, 5, -50);
            //pitch = MathUtil.Radians(15);
           
            Renderer.MainView.Attach(null, null, frameGraph);

        }

        public override void Update()
        {
            uint offset = 0;
            float gridSize = 15;

            for (int i = 0; i < COUNT; i++)
            {
                mat4 worldTransform = translate(gridSize * (i / 10), 0, gridSize * (i % 10));

                batches[i].offset = offset;

                Utilities.CopyMemory(ubObjectVS[Graphics.WorkContext].Mapped + (int)offset, Utilities.AsPointer(ref worldTransform), Utilities.SizeOf<mat4>());

                offset += (uint)64 * 4;
            }

            ubObjectVS[Graphics.WorkContext].Flush();

            UpdateInput();
        }

        private void UpdateInput()
        {
            var input = Input.Instance;
            if (input.snapshot == null)
            {
                return;
            }

            if (mousePos == Vector2.Zero)
                mousePos = input.MousePosition;

            vec3 offset = default;
            if (input.IsMouseDown(MouseButton.Right))
            {
                Vector2 delta = (input.MousePosition - mousePos) * Time.Delta * rotSpeed;
                yaw += delta.X;
                pitch += delta.Y;

                if (input.IsKeyPressed(Key.W))
                {
                    offset.z += 1.0f;
                }

                if (input.IsKeyPressed(Key.S))
                {
                    offset.z -= 1.0f;
                }

                if (input.IsKeyPressed(Key.A))
                {
                    offset.x -= 1.0f;
                }

                if (input.IsKeyPressed(Key.D))
                {
                    offset.x += 1.0f;
                }
            }

            if (input.IsMouseDown(MouseButton.Middle))
            {
                Vector2 delta = input.MousePosition - mousePos;
                offset.x = delta.X;
                offset.y = delta.Y;
            }

            cameraPos += offset * (Time.Delta * moveSpeed);
            cameraPos += new vec3(0, 0, input.WheelDelta * wheelSpeed);

            mousePos = input.MousePosition;
        }

        void CustomDraw(GraphicsPass pass, RenderView view)
        {
            mat4 rotM = mat4(1.0f);
            
            rotM = yawPitchRoll(yaw, pitch, 0);

            var m = translate(cameraPos)* rotM ;
            cameraVS.View = inverse(m);
            
            var proj = perspective((float)Math.PI / 4, 16 / 9.0f, 1, 1000);
            proj[1][1] = -proj[1][1];

            cameraVS.ViewProj = proj*cameraVS.View ;
            ubCameraVS[Graphics.WorkContext].SetData(ref cameraVS);

            for(int i = 0; i < COUNT; i++)
            {
                var batch = batches[i];
                pass.DrawBatch(pass.CmdBuffer, batch, resourceSet[Graphics.WorkContext], null, batch.offset);
            }
        }
    }
}
