﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    using vec2 = Vector2;
    using vec3 = Vector3;
    using vec4 = Vector4;
    using mat4 = Matrix;

    public class RenderView : Object
    {
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

        private ResourceLayout perFrameResLayout;
        internal ResourceSet perFrameSet;

        ResourceSet perViewResourceSet;
        ResourceSet perObjectResourceSet;

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

            perFrameResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            perFrameSet = new ResourceSet(perFrameResLayout, ubCameraVS);
        }

        public void AddDrawable(Drawable drawable)
        {
            drawables.Add(drawable);

            foreach (SourceBatch batch in drawable.Batches)
            {
                batches.Add(batch);
            }
        }

        public void Update(ref FrameInfo frameInfo)
        {
            Profiler.BeginSample("ViewUpdate");

            var g = Graphics.Instance;
            this.frameInfo = frameInfo;
            this.frameInfo.camera = Camera;
            this.frameInfo.viewSize = new Int2(g.Width, g.Height);

            Viewport.Define(0, 0, g.Width, g.Height);

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

            Profiler.EndSample();
        }

        private void UpdateDrawables()
        {

            drawables.Clear();
            batches.Clear();

            if (!Scene || !Camera)
            {
                return;
            }

            Profiler.BeginSample("Culling");
            FrustumOctreeQuery frustumOctreeQuery = new FrustumOctreeQuery
            {
                view = this,
                camera = Camera
            };

            Scene.GetDrawables(frustumOctreeQuery, AddDrawable);
            Profiler.EndSample();
            
            Profiler.BeginSample("UpdateGeometry");
            
            Parallel.ForEach(drawables, drawable => drawable.Update(ref frameInfo));

            Profiler.EndSample();
            
            Profiler.BeginSample("UpdateBatches");

            Parallel.ForEach(drawables, drawable => drawable.UpdateBatches(ref frameInfo));

            Profiler.EndSample();

        }

        private void UpdateViewParameters()
        {
            cameraVS.View = camera.View;
            Matrix.Invert(ref camera.View, out cameraVS.ViewInv);
            cameraVS.ViewProj = camera.View*camera.Projection;
            cameraVS.CameraPos = camera.Node.Position;
            ubCameraVS.SetData(ref cameraVS);
        }

        private void UpdateLightParameters()
        {
        }

        public void Render(int imageIndex)
        {
            Profiler.BeginSample("ViewRender");

            FrameGraph?.Summit(imageIndex);
            OverlayPass?.Summit(imageIndex);

            Profiler.EndSample();
        }
    }
    

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
    public struct LightPS
    {
        public vec4 LightColor;
        public vec4 LightPos;
        public vec3 LightDir;
        float pading1;
        public vec4 NormalOffsetScale;
        public vec4 ShadowCubeAdjust;
        public vec4 ShadowDepthFade;
        public vec2 ShadowIntensity;
        public vec2 ShadowMapInvSize;
        public vec4 ShadowSplits;
        /*
        mat4 LightMatricesPS [4];
        */
        //    vec2 VSMShadowParams;

        public float LightRad;
        public float LightLength;

    }

}
