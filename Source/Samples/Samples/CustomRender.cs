using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame.Samples
{
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
        SourceBatch batch;
        ResourceSet resourceSet;
        Matrix worldTransform;

        CameraVS cameraVS = new CameraVS();
        DeviceBuffer ubCameraVS;
        Geometry cube;
        Vector3 cameraPos;
        public override void Init()
        {
            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            var mat = new Material("Shaders/Textured.shader");
            mat.SetTexture("DiffMap", Texture2D.White);

            cube = GeometricPrimitive.CreateCube(10, 10, 10);
            batch = new SourceBatch
            {
                geometry = cube,
                material = mat,
                numWorldTransforms = 1,
                worldTransform = Utilities.AsPointer(ref worldTransform)
            };

            ubCameraVS = DeviceBuffer.CreateUniformBuffer<CameraVS>();

            resourceSet = new ResourceSet(resourceLayout, ubCameraVS);
            worldTransform = Matrix.Identity;

            frameGraph.AddRenderPass(new GraphicsPass
            {
                OnDraw = CustomDraw
            });

            cameraPos = new Vector3(0, 8, -30);

            //Renderer.Instance.MainView.Attach(camera, scene, frameGraph);
            Renderer.Instance.MainView.Attach(null, null, frameGraph);

            //var m = Matrix.LookAtLH(cameraPos,
            //        new Vector3(0, 0, 0), Vector3.UnitY);


            Quaternion newRotation = Quaternion.LookAtLH(cameraPos, new Vector3(0, 0, 0), Vector3.UnitY);

            Vector3 e = newRotation.ToEuler();
           
            yaw = e.Y;
            pitch = e.X;
          


            var m = Matrix.Transformation(ref cameraPos, ref newRotation);
            m.Invert();
            cameraVS.View = m;



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

            offset = Vector3.Zero;
            if (input.IsMouseDown(MouseButton.Right))
            {
                Vector2 delta = (input.MousePosition - mousePos) * Time.Delta * rotSpeed;
                yaw += delta.X;
                pitch += delta.Y;

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

            cameraPos += offset * (Time.Delta * moveSpeed);
            cameraPos += new Vector3(0, 0, input.WheelDelta * (Time.Delta * wheelSpeed));

            mousePos = input.MousePosition;
        }

        void CustomDraw(GraphicsPass pass, RenderView view)
        {
            var m = Matrix.RotationYawPitchRoll(yaw, pitch, 0)
                * Matrix.Translation(cameraPos);
            m.Invert();
            cameraVS.View = m;

            var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4, 16 / 9.0f, 1, 100);
            //Matrix.Invert(ref camera.View, out cameraVS.ViewInv);
            cameraVS.ViewProj = cameraVS.View * proj;
            //cameraVS.CameraPos = camera.Node.Position;
            ubCameraVS.SetData(ref cameraVS);

            worldTransform = Matrix.Identity;
            batch.worldTransform = Utilities.AsPointer(ref worldTransform);

            //pass.DrawBatch(pass.CmdBuffer, batch, view.perFrameSet);// resourceSet);
            pass.DrawBatch(pass.CmdBuffer, batch, resourceSet);
        }
    }
}
