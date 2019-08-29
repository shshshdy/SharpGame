using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


namespace SharpGame.Samples
{
    using Vector3 = System.Numerics.Vector3;
    using Matrix = System.Numerics.Matrix4x4;

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraVS
    {
        public Matrix View;
        public Matrix ViewInv;
        public Matrix ViewProj;
        public Vector3 CameraPos;
        public float NearClip;
        public Vector3 FrustumSize;
        public float FarClip;
    }

    [SampleDesc(sortOrder = 6)]
    public class CustomRender : Sample
    {
        FrameGraph frameGraph = new FrameGraph();
        List<SourceBatch> batches = new List<SourceBatch>();
        ResourceSet resourceSet;

        CameraVS cameraVS = new CameraVS();
        DeviceBuffer ubCameraVS;
        DeviceBuffer ubObjectVS;
        Geometry cube;
        Vector3 cameraPos;
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

            ubCameraVS = DeviceBuffer.CreateUniformBuffer<CameraVS>();
            ubObjectVS = DeviceBuffer.CreateUniformBuffer<Matrix>(COUNT*4);

            ubObjectVS.Map();

            resourceSet = new ResourceSet(resourceLayout, ubCameraVS, ubObjectVS);
            
            uint offset = 0;
            float gridSize = 15;
            for(int i = 0; i < COUNT; i++)
            {
                Matrix worldTransform = Matrix.CreateTranslation(gridSize * (i/10), 0, gridSize * (i % 10));

                batches[i].offset = offset;

                Utilities.CopyMemory(ubObjectVS.Mapped + (int)offset, Utilities.AsPointer(ref worldTransform), Utilities.SizeOf<Matrix>());

                offset += (uint)Utilities.SizeOf<Matrix>()*4;
            }

            ubObjectVS.Flush();

            frameGraph.AddGraphicsPass(CustomDraw);

            cameraPos = new Vector3(0, 5, 50);
            //pitch = MathUtil.Radians(15);
           
            Renderer.MainView.Attach(null, null, frameGraph);

        }

        public override void Update()
        {
            var input = Input.Instance;
            if (input.snapshot == null)
            {
                return;
            }

            if (mousePos == Vector2.Zero)
                mousePos = input.MousePosition;

            offset = default;
            if (input.IsMouseDown(MouseButton.Right))
            {
                Vector2 delta = (input.MousePosition - mousePos) * Time.Delta * rotSpeed;
                yaw += -delta.X;
                pitch += -delta.Y;

                if (input.IsKeyPressed(Key.W))
                {
                    offset.Z += 1.0f;
                }

                if (input.IsKeyPressed(Key.S))
                {
                    offset.Z -= 1.0f;
                }

                if (input.IsKeyPressed(Key.A))
                {
                    offset.X -= 1.0f;
                }

                if (input.IsKeyPressed(Key.D))
                {
                    offset.X += 1.0f;
                }
            }

            if (input.IsMouseDown(MouseButton.Middle))
            {
                Vector2 delta = input.MousePosition - mousePos;
                offset.X = -delta.X;
                offset.Y = delta.Y;
            }

            cameraPos += (Vector3)offset * (Time.Delta * moveSpeed);
            cameraPos += new Vector3(0, 0, -input.WheelDelta * wheelSpeed);

            mousePos = input.MousePosition;
        }

        void CustomDraw(GraphicsPass pass, RenderView view)
        {
            var m = Matrix.CreateFromYawPitchRoll(yaw, pitch, 0) * Matrix.CreateTranslation(cameraPos);        
            Matrix.Invert(m, out cameraVS.View);

            var proj = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 4, 16 / 9.0f, 1, 1000);
            proj.M22 = -proj.M22;
            cameraVS.ViewProj = cameraVS.View * proj;
            ubCameraVS.SetData(ref cameraVS);

            for(int i = 0; i < COUNT; i++)
            {
                var batch = batches[i];
                pass.DrawBatch(pass.CmdBuffer, batch, resourceSet, null, batch.offset);
            }
        }
    }
}
