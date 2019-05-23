using System;
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
        public Scene Scene { get; set; }
        public Camera Camera { get; set; }
        public FrameGraph RenderPath { get; set; }

        private Viewport viewport;
        public ref Viewport Viewport => ref viewport;

        public uint ViewMask { get; set; }

        public PassHandler OverlayPass { get; set; }

        internal FastList<Drawable> drawables = new FastList<Drawable>();
        internal FastList<Light> lights = new FastList<Light>();

        private FrameInfo frameInfo;

        private FrameUniform frameUniform = new FrameUniform();
        private CameraVS cameraVS = new CameraVS();

        private CameraPS cameraPS = new CameraPS();
        private LightPS light = new LightPS();

        GraphicsBuffer ubFrameInfo;
        GraphicsBuffer ubCameraVS;
        GraphicsBuffer ubCameraPS;
        GraphicsBuffer ubLight;

        public RenderView(Camera camera = null, Scene scene = null, FrameGraph renderPath = null)
        {
            Attach(camera, scene, renderPath);
        }

        public void Attach(Camera camera, Scene scene, FrameGraph renderPath = null)
        {
            Scene = scene;
            Camera = camera;
            RenderPath = renderPath;

            if (RenderPath == null)
            {
                RenderPath = new FrameGraph();
                RenderPath.AddRenderPass(new ScenePassHandler());
            }

            CreateBuffers();
        }

        protected void CreateBuffers()
        {
            if (ubFrameInfo == null)
            {
                ubFrameInfo = GraphicsBuffer.CreateUniformBuffer<FrameUniform>();
            }

            if (ubCameraVS == null)
            {
                ubCameraVS = GraphicsBuffer.CreateUniformBuffer<CameraVS>();
            }

            if (ubCameraPS == null)
            {
                ubCameraPS = GraphicsBuffer.CreateUniformBuffer<CameraPS>();
            }

            if (ubLight == null)
            {
                ubLight = GraphicsBuffer.CreateUniformBuffer<LightPS>();
            }

        }

        public void Update(ref FrameInfo frameInfo)
        {
            var graphics = Graphics.Instance;

            this.frameInfo = frameInfo;
            this.frameInfo.camera = Camera;
            this.frameInfo.viewSize = new Int2(graphics.Width, graphics.Height);

            Viewport.Define(0, 0, graphics.Width, graphics.Height);

            this.SendGlobalEvent(new BeginView { view = this });

            UpdateDrawables();

            RenderPath.Draw(this);

            OverlayPass?.Draw(this);

            this.SendGlobalEvent(new EndView { view = this });
        }

        private void UpdateDrawables()
        {
            drawables.Clear();

            if (Scene == null || Camera == null)
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

        public void Render(int imageIndex)
        {
            RenderPath?.Summit(imageIndex);
            OverlayPass?.Summit(imageIndex);
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
        public vec3 CameraPos;
        public float NearClip;
        public vec4 DepthMode;
        public vec3 FrustumSize;
        public float FarClip;
        public vec4 GBufferOffsets;
        public mat4 View;
        public mat4 ViewInv;
        public mat4 ViewProj;
        public vec4 ClipPlane;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraPS
    {
        public vec3 CameraPosPS;
        float pading1;
        public vec4 DepthReconstruct;
        public vec2 GBufferInvSize;
        public float NearClipPS;
        public float FarClipPS;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct LightPS
    {
        public vec4 cLightColor;
        public vec4 cLightPosPS;
        public vec3 cLightDirPS;
        float pading1;
        public vec4 cNormalOffsetScalePS;
        public vec4 cShadowCubeAdjust;
        public vec4 cShadowDepthFade;
        public vec2 cShadowIntensity;
        public vec2 cShadowMapInvSize;
        public vec4 cShadowSplits;
        /*
        mat4 cLightMatricesPS [4];
        */
        //    vec2 cVSMShadowParams;

        public float cLightRad;
        public float cLightLength;

    }

}
