using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
 
    public class RenderView : Object
    {
        public static bool NegativeViewport { get; set; } = false;

        private Scene scene;
        public Scene Scene => scene;

        private Camera camera;
        public Camera Camera => camera;

        public RenderPipeline Renderer { get; set; }

        private Viewport viewport;
        public ref Viewport Viewport => ref viewport;
        public Rect2D ViewRect => new Rect2D((int)Viewport.x, (int)Viewport.y, (int)Viewport.width, (int)Viewport.height);

        public float Width => viewport.width;
        public float Height => viewport.height;

        public bool DrawDebug { get => FrameGraph.drawDebug && drawDebug; set => drawDebug = value; }
        bool drawDebug = true;
        FrameGraph debugPass;

        public uint ViewMask { get; set; } = 1;
        public ref FrameInfo Frame => ref frameInfo;
        public ref LightParameter LightParam => ref lightParameter;

        internal FastList<Drawable> drawables = new FastList<Drawable>();

        internal FastList<Light> lights = new FastList<Light>();

        internal FastList<SourceBatch>[] batches = new FastList<SourceBatch>[]
        {
            new FastList<SourceBatch>(), new FastList<SourceBatch>(), new FastList<SourceBatch>()
        };

        internal FastList<SourceBatch> opaqueBatches => batches[0];
        internal FastList<SourceBatch> alphaTestBatches => batches[1];
        internal FastList<SourceBatch> translucentBatches => batches[2];

        FrustumOctreeQuery frustumOctreeQuery = new FrustumOctreeQuery();

        private FrameInfo frameInfo;

        private FrameUniform frameUniform = new FrameUniform();
        private CameraVS cameraVS = new CameraVS();
        private CameraPS cameraPS = new CameraPS();
        private LightParameter lightParameter = new LightParameter();

        internal Buffer ubFrameInfo;
        public SharedBuffer ubCameraVS;

        internal SharedBuffer ubCameraPS;
        internal Buffer ubLight;


        private ResourceLayout vsResLayout;

        public ResourceSet Set0 => vsResourceSet[Graphics.Instance.WorkImage];
        ResourceSet[] vsResourceSet = new ResourceSet[3];

        private ResourceLayout psResLayout;
        public ResourceSet Set1 => psResourceSet[Graphics.Instance.WorkImage];
        ResourceSet[] psResourceSet = new ResourceSet[3];

        Graphics Graphics => Graphics.Instance;
        FrameGraph FrameGraph => FrameGraph.Instance;

        public RenderView()
        {
            CreateBuffers();

            /*
            var renderPass = Graphics.CreateRenderPass();

            debugPass = new GraphicsPass
            {
                RenderPass = renderPass,
                Framebuffers = Graphics.CreateSwapChainFramebuffers(renderPass),
                OnDraw = (pass, view) =>
                {
                    if (view.Scene == null)
                    {
                        return;
                    }

                    var debug = view.Scene.GetComponent<DebugRenderer>();
                    if (debug == null)
                    {
                        return;
                    }

                    var cmdBuffer = pass.CmdBuffer;
                    debug.Render(view, cmdBuffer);
                }
            };*/


        }

        public void Reset()
        {
            Renderer?.Reset();
            //debugPass?.Reset();
        }

        public void Attach(Camera camera, Scene scene, RenderPipeline frameGraph = null)
        {
            this.scene = scene;
            this.camera = camera;

            Renderer = frameGraph;

            Renderer?.Init(this);


        }

        protected void CreateBuffers()
        {
            ubFrameInfo = Buffer.CreateUniformBuffer<FrameUniform>();

            ubCameraVS = new SharedBuffer(BufferUsageFlags.UniformBuffer, (uint)Utilities.SizeOf<CameraVS>());

            ubCameraPS = new SharedBuffer(BufferUsageFlags.UniformBuffer, (uint)Utilities.SizeOf<CameraPS>());
            ubLight = Buffer.CreateUniformBuffer<LightParameter>();

            vsResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                new ResourceLayoutBinding(1, DescriptorType.UniformBufferDynamic, ShaderStage.Vertex),
            };

            vsResourceSet[0] = new ResourceSet(vsResLayout, ubCameraVS[0], FrameGraph.TransformBuffer[0]);
            vsResourceSet[1] = new ResourceSet(vsResLayout, ubCameraVS[1], FrameGraph.TransformBuffer[1]);
            vsResourceSet[2] = new ResourceSet(vsResLayout, ubCameraVS[2], FrameGraph.TransformBuffer[2]);

            psResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(2, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
            };

            psResourceSet[0] = new ResourceSet(psResLayout, ubCameraPS[0], ubLight, ShadowPass.DepthRT);
            psResourceSet[1] = new ResourceSet(psResLayout, ubCameraPS[1], ubLight, ShadowPass.DepthRT);
            psResourceSet[2] = new ResourceSet(psResLayout, ubCameraPS[2], ubLight, ShadowPass.DepthRT);

        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void AddDrawable(Drawable drawable)
        {
            if(drawable.DrawableFlags == Drawable.DRAWABLE_LIGHT)
            {
                lights.Add(drawable as Light);
            }
            else
            {
                drawables.Add(drawable);

                foreach (SourceBatch batch in drawable.Batches)
                {
                    batches[batch.material.blendType].Add(batch);

                    if (batch.frameNum != Frame.frameNumber)
                    {
                        batch.frameNum = Frame.frameNumber;
                        batch.offset = FrameGraph.GetTransform(batch.worldTransform, (uint)batch.numWorldTransforms);
                    }

                }
            }
        
        }

        public void Update(ref FrameInfo frame)
        {
            Profiler.BeginSample("ViewUpdate");

            var g = Graphics.Instance;

            frameInfo = frame;
            frameInfo.camera = camera;
            frameInfo.viewSize = new Int2((int)g.Width, (int)g.Height);

            Viewport.Define(0, 0, g.Width, g.Height);

            if (camera && camera.AutoAspectRatio)
            {
                camera.SetAspectRatio((float)frameInfo.viewSize.X / (float)frameInfo.viewSize.Y);
            }

            drawables.Clear();
            lights.Clear();
            opaqueBatches.Clear();
            alphaTestBatches.Clear();
            translucentBatches.Clear();

            UpdateDrawables();

            frameUniform.DeltaTime = Time.Delta;
            frameUniform.ElapsedTime = Time.Elapsed;

            ubFrameInfo.SetData(ref frameUniform);

            if (camera != null)
            {
                UpdateViewParameters();
            }

            UpdateLightParameters();

            UpdateGeometry();

            if (Renderer == null)
            {
                Renderer = new ForwardRenderer();
                Renderer.Init(this);
            }

            Renderer.Update();

            if (DrawDebug)
            {
                debugPass?.Update();
            }

            Profiler.EndSample();
        }

        public void Render()
        {
            Renderer.Draw();

            if (DrawDebug)
            {
            //    debugPass?.Draw(this);
            }


        }

        private void UpdateDrawables()
        {
            if (!scene || !camera)
            {
                return;
            }

            Profiler.BeginSample("Culling");

            frustumOctreeQuery.Init(camera, Drawable.DRAWABLE_ANY, ViewMask);
            Scene.GetDrawables(frustumOctreeQuery, AddDrawable);

            //translucentBatches.Sort()

            Profiler.EndSample();

        }

        private void UpdateGeometry()
        {
            Profiler.BeginSample("UpdateBatches");
            Parallel.ForEach(drawables, drawable => drawable.UpdateBatches(in frameInfo));
            Profiler.EndSample();

            Profiler.BeginSample("UpdateGeometry");
            Parallel.ForEach(drawables, drawable => drawable.UpdateGeometry(in frameInfo));
            Profiler.EndSample();
        }

        private void UpdateViewParameters()
        {
            cameraVS.View = camera.View;
            cameraVS.ViewInv = glm.inverse(cameraVS.View);
            cameraVS.ViewProj = camera.VkProjection*camera.View;
            cameraVS.CameraPos = camera.Node.Position;

            camera.GetFrustumSize(out var nearVector, out var farVector);

            float nearClip = camera.NearClip;
            float farClip = camera.FarClip;

            cameraVS.FrustumSize = farVector;
            cameraVS.NearClip = camera.NearClip;
            cameraVS.FarClip = camera.FarClip;

            if(camera.Orthographic)
            {
                cameraVS.DepthMode = new vec4(1, 0, 1, 0);
            }
            else
            {
                cameraVS.DepthMode = new vec4(0, 0, 0, 1 / farClip);
            }

            ubCameraVS.SetData(ref cameraVS);
            ubCameraVS.Flush();

            cameraPS.ViewInv = cameraVS.ViewInv;
            cameraPS.CameraPos = camera.Node.Position;

            vec4 depthReconstruct = new vec4(farClip / (farClip - nearClip), -nearClip / (farClip - nearClip),
                camera.Orthographic ? 1.0f : 0.0f, camera.Orthographic ? 0.0f : 1.0f);
            cameraPS.DepthReconstruct = depthReconstruct;

            cameraPS.GBufferInvSize = new vec2(1.0f / Width, 1.0f / Height);

            cameraPS.NearClip = nearClip;
            cameraPS.FarClip = farClip;

            ubCameraPS.SetData(ref cameraPS);
            ubCameraPS.Flush();

        }

        vec4[] lightVec = new vec4[]
        {
            new vec4(-1, 0, 0, 0),
            new vec4(1, 0, 0, 0),
            new vec4(0, -1, 0, 0),
            new vec4(0, 1, 0, 0),
            new vec4(1, 1, 0, 0),
            new vec4(-1, -1, 0, 0),
            new vec4(1, 0, 1, 1),
            new vec4(-1, 0,  -1, 0),
        };

        private void UpdateLightParameters()
        {
            Environment env = scene?.GetComponent<Environment>();

            if(env)
            {
                lightParameter.AmbientColor = env.AmbientColor;
                lightParameter.SunlightColor = env.SunlightColor;
                lightParameter.SunlightDir = env.SunlightDir;
            }
            else
            {

                lightParameter.AmbientColor = new Color4(0.15f, 0.15f, 0.25f, 1.0f);
                lightParameter.SunlightColor = new Color4(0.5f);
                lightParameter.SunlightDir = new vec3(-1, -1, 1);
                lightParameter.SunlightDir.Normalize();

                for (int i = 0; i < 8; i++)
                {
                    lightParameter.lightColor[i] = Color4.White;
                    lightParameter.lightVec[i] = glm.normalize(lightVec[i]);
                }

            }

            ubLight.SetData(ref lightParameter);
        }

    }
    

}
