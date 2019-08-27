using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    using vec2 = Vector2;
    using vec3 = Vector3;
    using vec4 = Vector4;
    using mat4 = Matrix;


    [StructLayout(LayoutKind.Sequential)]
    public struct FrameUniform
    {
        public float DeltaTime;
        public float ElapsedTime;
    }

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

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraPS
    {
        public vec3 CameraPos;
        float pading1;
        public vec4 DepthReconstruct;
        public vec2 GBufferInvSize;
        public float NearClip;
        public float FarClip;
    }


    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LightPS
    {
        public Color4 AmbientColor;
        public Color4 SunlightColor;
        public vec3 SunlightDir;
        public float LightPS_pading1;

        public fixed float LightColor[4 * 8];
        public fixed float LightVec[4 * 8];

        public ref Color4 color(int index)
        {
            return ref Unsafe.As<float, Color4>(ref LightColor[index * 4]);
        }

        public ref Vector4 lightVec(int index)
        {
            return ref Unsafe.As<float, Vector4>(ref LightVec[index * 4]);
        }
    }

    public class RenderView : Object
    {
        public static bool NegativeViewport { get; set; } = false;

        private Scene scene;
        public Scene Scene => scene;

        private Camera camera;
        public Camera Camera => camera;

        public FrameGraph FrameGraph { get; set; }
        public GraphicsPass OverlayPass { get; set; }

        private Viewport viewport;
        public ref Viewport Viewport => ref viewport;

        public uint ViewMask { get; set; }
        
        internal FastList<Drawable> drawables = new FastList<Drawable>();
        internal FastList<Light> lights = new FastList<Light>();
        internal FastList<SourceBatch> batches = new FastList<SourceBatch>();

        private FrameInfo frameInfo;

        private FrameUniform frameUniform = new FrameUniform();
        private CameraVS cameraVS = new CameraVS();
        private CameraPS cameraPS = new CameraPS();
        private LightPS light = new LightPS();

        internal DeviceBuffer ubFrameInfo;
        internal DeviceBuffer ubCameraVS;
        internal DeviceBuffer ubCameraPS;
        internal DeviceBuffer ubLight;

        internal DoubleBuffer ubMatrics;

        private ResourceLayout perObjectResLayout;

        public ResourceSet VSSet => vsResourceSet[Graphics.Instance.WorkContext];
        ResourceSet[] vsResourceSet = new ResourceSet[2];

        private ResourceLayout psResLayout;
        public ResourceSet PSSet => psResourceSet[Graphics.Instance.WorkContext];
        ResourceSet[] psResourceSet = new ResourceSet[2];

        public RenderView(Camera camera = null, Scene scene = null, FrameGraph renderPath = null)
        {
            Attach(camera, scene, renderPath);
        }

        public void Attach(Camera camera, Scene scene, FrameGraph renderPath = null)
        {
            this.scene = scene;
            this.camera = camera;
                        
            FrameGraph = renderPath;

            if (FrameGraph == null)
            {
                FrameGraph = new FrameGraph();
                FrameGraph.AddRenderPass(new ScenePass());
            }

            CreateBuffers();
        }

        protected void CreateBuffers()
        {
            if (ubFrameInfo == null)
            {
                ubFrameInfo = DeviceBuffer.CreateUniformBuffer<FrameUniform>();
            }

            if (ubCameraVS == null)
            {
                ubCameraVS = DeviceBuffer.CreateUniformBuffer<CameraVS>();
            }

            if (ubCameraPS == null)
            {
                ubCameraPS = DeviceBuffer.CreateUniformBuffer<CameraPS>();
            }

            if (ubLight == null)
            {
                ubLight = DeviceBuffer.CreateUniformBuffer<LightPS>();
            }

            if(ubMatrics == null)
            {
                uint size = 6400 * 1024;
                ubMatrics = new DoubleBuffer(size);
            }

            perObjectResLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                new ResourceLayoutBinding(1, DescriptorType.UniformBufferDynamic, ShaderStage.Vertex),
            };

            vsResourceSet[0] = new ResourceSet(perObjectResLayout, ubCameraVS, ubMatrics.Buffer[0]);
            vsResourceSet[1] = new ResourceSet(perObjectResLayout, ubCameraVS, ubMatrics.Buffer[1]);

            psResLayout = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.UniformBuffer, ShaderStage.Fragment),
            };

            psResourceSet[0] = new ResourceSet(psResLayout, ubCameraPS, ubLight);
            psResourceSet[1] = new ResourceSet(psResLayout, ubCameraPS, ubLight);
        }

        public void AddDrawable(Drawable drawable)
        {
            drawables.Add(drawable);

            foreach (SourceBatch batch in drawable.Batches)
            {
                batches.Add(batch);
                batch.offset = GetTransform(batch.worldTransform, (uint)batch.numWorldTransforms);
            }
        }

        public void AddBatch(SourceBatch batch)
        {
            batches.Add(batch);
            batch.offset = GetTransform(batch.worldTransform, (uint)batch.numWorldTransforms);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe uint GetTransform(IntPtr pos, uint count)
        {
            uint sz = (uint)Utilities.SizeOf<Matrix>() * count;
            return ubMatrics.Alloc(sz, pos);
        }

        public void Update(ref FrameInfo frameInfo)
        {
            Profiler.BeginSample("ViewUpdate");

            var g = Graphics.Instance;
            this.frameInfo = frameInfo;
            this.frameInfo.camera = camera;
            this.frameInfo.viewSize = new Int2(g.Width, g.Height);

            ubMatrics.Clear();

            if (NegativeViewport)
            {
                Viewport.Define(0, g.Height, g.Width, -g.Height);
            }
            else
            {
                Viewport.Define(0, 0, g.Width, g.Height);
            }

            frameUniform.DeltaTime = Time.Delta;
            frameUniform.ElapsedTime = Time.Elapsed;

            ubFrameInfo.SetData(ref frameUniform);

            this.SendGlobalEvent(new BeginView { view = this });

            UpdateDrawables();

            if(camera != null)
            {
                UpdateViewParameters();
            }

            UpdateLightParameters();

            FrameGraph.Draw(this);

            OverlayPass?.Draw(this);

            this.SendGlobalEvent(new EndView { view = this });

            ubMatrics.Flush();

            Profiler.EndSample();
        }

        private void UpdateDrawables()
        {
            drawables.Clear();
            batches.Clear();

            if (!scene || !camera)
            {
                return;
            }

            if(camera.AutoAspectRatio)
            {
                camera.SetAspectRatio((float)frameInfo.viewSize.X / (float)frameInfo.viewSize.Y);
            }

            Profiler.BeginSample("Culling");
            FrustumOctreeQuery frustumOctreeQuery = new FrustumOctreeQuery
            {
                view = this,
                camera = camera
            };

            Scene.GetDrawables(frustumOctreeQuery, AddDrawable);
            Profiler.EndSample();
            
            Profiler.BeginSample("UpdateDrawable");
            
            Parallel.ForEach(drawables, drawable => drawable.Update(ref frameInfo));

            Profiler.EndSample();
            
            Profiler.BeginSample("UpdateBatches");

            Parallel.ForEach(drawables, drawable => drawable.UpdateBatches(ref frameInfo));

            Profiler.EndSample();

            Profiler.BeginSample("UpdateGeometry");
            Parallel.ForEach(drawables, drawable => drawable.UpdateGeometry(ref frameInfo));
            Profiler.EndSample();
        }

        private void UpdateViewParameters()
        {
            cameraVS.View = camera.View;
            Matrix.Invert(ref camera.View, out cameraVS.ViewInv);
            cameraVS.ViewProj = camera.View*camera.Projection;
            cameraVS.CameraPos = camera.Node.Position;
            //cameraVS.FrustumSize = camera.Frustum;
            cameraVS.NearClip = camera.NearClip;
            cameraVS.FarClip = camera.FarClip;

            ubCameraVS.SetData(ref cameraVS);

            cameraPS.CameraPos = camera.Node.Position;
            cameraPS.NearClip = camera.NearClip;
            cameraPS.FarClip = camera.FarClip;
            ubCameraPS.SetData(ref cameraPS);

        }

        Vector4[] lightVec = new Vector4[]
        {
            new Vector4(-1, 0, 0, 0),
            new Vector4(1, 0, 0, 0),
            new Vector4(0, -1, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(1, 1, 0, 0),
            new Vector4(-1, -1, 0, 0),
            new Vector4(1, 0, 1, 1),
            new Vector4(-1, 0,  -1, 0),
        };

        private void UpdateLightParameters()
        {
            light.AmbientColor = new Color4(0.15f, 0.15f, 0.25f, 1.0f);
            light.SunlightColor = new Color4(0.5f);
            light.SunlightDir = new Vector3(-1, -1, 1);
            light.SunlightDir.Normalize();

            for(int i = 0; i < 8; i++)
            {
                light.color(i) = Color4.White;
                light.lightVec(i) = Vector4.Normalize(lightVec[i]);
            }

            ubLight.SetData(ref light);
        }

        public void Render(int imageIndex)
        {
            Profiler.BeginSample("ViewRender");

            FrameGraph?.Submit(imageIndex);
            OverlayPass?.Submit(imageIndex);

            Profiler.EndSample();
        }
    }
    

}
