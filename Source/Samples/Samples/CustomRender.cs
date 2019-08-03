using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame.Samples
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraVS
    {
        public Matrix4x4 View;
        public Matrix4x4 ViewInv;
        public Matrix4x4 ViewProj;
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
        Matrix4x4 worldTransform;

        CameraVS cameraVS = new CameraVS();
        DeviceBuffer ubCameraVS;
        Geometry cube;
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 20, -30);
            //cameraNode.LookAt(Vector3.Zero);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(30), 0, 0);

            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;
            camera.FarClip = 3000.0f;

            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };
            resourceLayout.Build();

            var resourceLayoutTex = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };

            var shader = new Shader
            {
                new Pass("shaders/Textured.vert.spv", "shaders/Textured.frag.spv")
                {
                    CullMode = CullMode.Back,
                    FrontFace = FrontFace.CounterClockwise,
                    ResourceLayout = new[] { resourceLayout, resourceLayoutTex },
                    PushConstantRanges = new[]
                    {
                        new PushConstantRange(ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>())
                    }
                }
            };

            var mat = new Material
            {
                Shader = shader,
                ResourceSet = new ResourceSet(resourceLayoutTex, Texture.White)
            };

            cube = GeometricPrimitive.CreateCube(10, 10, 10);
            batch = new SourceBatch
            {
                geometry = cube,
                material = mat,
                numWorldTransforms = 1,
                worldTransform = (IntPtr)Utilities.AsPointer(ref worldTransform)
            };

            ubCameraVS = DeviceBuffer.CreateUniformBuffer<CameraVS>();
            resourceSet = new ResourceSet(resourceLayout, ubCameraVS);

            worldTransform = Matrix4x4.Identity; ;
           

            frameGraph.AddRenderPass(new GraphicsPass
            {
                OnDraw = CustomDraw
            });

           
            //Renderer.MainView.Attach(null, null, frameGraph);
            Renderer.Instance.MainView.Attach(camera, scene, frameGraph);

        }

        void CustomDraw(GraphicsPass pass, RenderView view)
        {
            cameraVS.View = Matrix4x4.CreateLookAt(new System.Numerics.Vector3(0, 20, -30),
                new System.Numerics.Vector3(0, 0, 0), System.Numerics.Vector3.UnitY);

            var proj = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, 16 / 9.0f, 1, 100);
            //Matrix.Invert(ref camera.View, out cameraVS.ViewInv);
            cameraVS.ViewProj = cameraVS.View * proj;
            //cameraVS.CameraPos = camera.Node.Position;
            ubCameraVS.SetData(ref cameraVS);

            worldTransform = Matrix4x4.Identity;
            batch.worldTransform = (IntPtr)Utilities.AsPointer(ref worldTransform);

            //pass.DrawBatch(pass.CmdBuffer, batch, view.perFrameSet);// resourceSet);
            pass.DrawBatch(pass.CmdBuffer, batch, resourceSet);
        }
    }
}
