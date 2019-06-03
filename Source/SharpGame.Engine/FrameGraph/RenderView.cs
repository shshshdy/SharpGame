﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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

        public FrameGraph RenderPath { get; set; }

        private Viewport viewport;
        public ref Viewport Viewport => ref viewport;

        public uint ViewMask { get; set; }
        
        internal FastList<Drawable> drawables = new FastList<Drawable>();
        internal FastList<Light> lights = new FastList<Light>();

        private FrameInfo frameInfo;

        private FrameUniform frameUniform = new FrameUniform();
        private CameraVS cameraVS = new CameraVS();

        private CameraPS cameraPS = new CameraPS();
        private LightPS light = new LightPS();

        internal DeviceBuffer ubFrameInfo;
        internal DeviceBuffer ubCameraVS;
        internal DeviceBuffer ubCameraPS;
        internal DeviceBuffer ubLight;

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
                        
            RenderPath = renderPath;

            if (RenderPath == null)
            {
                RenderPath = new FrameGraph();
                RenderPath.AddRenderPass(new FrameGraphScenePass());
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

        }

        public void Update(ref FrameInfo frameInfo)
        {
            var graphics = Graphics.Instance;

            this.frameInfo = frameInfo;
            this.frameInfo.camera = Camera;
            this.frameInfo.viewSize = new Int2(graphics.Width, graphics.Height);

            Viewport.Define(0, 0, graphics.Width, graphics.Height);

            frameUniform.DeltaTime = (float)Time.Delta;
            frameUniform.ElapsedTime = (float)Time.Elapsed;
            ubFrameInfo.SetData(ref frameUniform);

            this.SendGlobalEvent(new BeginView { view = this });

            Profiler.BeginSample("ViewUpdate");

            UpdateDrawables();

            if(camera != null)
            {
                UpdateViewParameters();
            }

            UpdateLightParameters();

            Profiler.EndSample();

            RenderPath.Draw(this);
            
            this.SendGlobalEvent(new EndView { view = this });

        }

        private void UpdateDrawables()
        {

            drawables.Clear();

            if (!Scene || !Camera)
            {
                return;
            }
        
            FrustumOctreeQuery frustumOctreeQuery = new FrustumOctreeQuery
            {
                view = this,
                camera = Camera
            };

            Scene.GetDrawables(frustumOctreeQuery, drawables);

            //todo:multi thread
            foreach(var drawable in drawables)
            {
                drawable.UpdateGeometry(ref frameInfo);
            }

            foreach (var drawable in drawables)
            {
                drawable.UpdateBatches(ref frameInfo);
            }

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
            RenderPath?.Summit(imageIndex);
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
