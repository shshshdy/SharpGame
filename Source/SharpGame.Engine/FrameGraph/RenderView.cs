using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
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
        public mat4 ViewInv;
        public vec3 CameraPos;
        float pading1;
        public vec4 DepthReconstruct;
        public vec2 GBufferInvSize;
        public float NearClip;
        public float FarClip;
    }


    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LightParameter
    {
        public Color4 AmbientColor;
        public Color4 SunlightColor;
        public vec3 SunlightDir;
        public float LightPS_pading1;

        public vec4 cascadeSplits;
        public FixedArray4<mat4> lightMatrices;

        public FixedArray8<Color4> lightColor;
        public FixedArray8<vec4> lightVec;
        
        public void SetLightMatrices(int index, ref mat4 mat)
        {
            lightMatrices[index] = mat;
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
        public Rect2D ViewRect => new Rect2D((int)Viewport.x, (int)Viewport.y, (int)Viewport.width, (int)Viewport.height);

        public float Width => viewport.width;
        public float Height => viewport.height;

        public bool DrawDebug { get => Renderer.DrawDebug && drawDebug; set => drawDebug = value; }
        bool drawDebug = true;
        GraphicsPass debugPass;

        public uint ViewMask { get; set; } = 1;
        public ref FrameInfo Frame => ref frameInfo;
        public ref LightParameter LightParam => ref lightParameter;

        internal FastList<Drawable> drawables = new FastList<Drawable>();

        internal FastList<Light> lights = new FastList<Light>();

        internal FastList<SourceBatch>[] batches = new FastList<SourceBatch>[]
        {
            new FastList<SourceBatch>(), new FastList<SourceBatch>(), new FastList<SourceBatch>()
        };

        internal FastList<SourceBatch> opaqueBatches => batches[(int)BlendType.None];
        internal FastList<SourceBatch> alphaTestBatches => batches[(int)BlendType.AlphaTest];
        internal FastList<SourceBatch> translucentBatches => batches[(int)BlendType.AlphaBlend];

        FrustumOctreeQuery frustumOctreeQuery = new FrustumOctreeQuery();

        private FrameInfo frameInfo;

        private FrameUniform frameUniform = new FrameUniform();
        private CameraVS cameraVS = new CameraVS();
        private CameraPS cameraPS = new CameraPS();
        private LightParameter lightParameter = new LightParameter();

        internal Buffer ubFrameInfo;
        public DoubleBuffer ubCameraVS;


        internal Buffer ubCameraPS;
        internal Buffer ubLight;


        private ResourceLayout vsResLayout;

        public ResourceSet Set0 => vsResourceSet[Graphics.Instance.WorkContext];
        ResourceSet[] vsResourceSet = new ResourceSet[2];

        private ResourceLayout psResLayout;
        public ResourceSet Set1 => psResourceSet[Graphics.Instance.WorkContext];
        ResourceSet[] psResourceSet = new ResourceSet[2];

        Graphics Graphics => Graphics.Instance;
        Renderer Renderer => Renderer.Instance;

        public RenderView()
        {
            CreateBuffers();

            var renderPass = Graphics.CreateRenderPass();

            debugPass = new GraphicsPass
            {
                RenderPass = renderPass,
                Framebuffers = Graphics.CreateSwapChainFramebuffers(renderPass),
            };

            debugPass.Add((pass, view) =>
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
            });

        }

        public void Attach(Camera camera, Scene scene, FrameGraph frameGraph = null)
        {
            this.scene = scene;
            this.camera = camera;
                        
            FrameGraph = frameGraph;
            FrameGraph?.Init();
        }

        protected void CreateBuffers()
        {
            ubFrameInfo = Buffer.CreateUniformBuffer<FrameUniform>();

            ubCameraVS = new DoubleBuffer(BufferUsageFlags.UniformBuffer, (uint)Utilities.SizeOf<CameraVS>());

            ubCameraPS = Buffer.CreateUniformBuffer<CameraPS>();
            ubLight = Buffer.CreateUniformBuffer<LightParameter>();

            vsResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                new ResourceLayoutBinding(1, DescriptorType.UniformBufferDynamic, ShaderStage.Vertex),
            };

            vsResourceSet[0] = new ResourceSet(vsResLayout, ubCameraVS[0], Renderer.TransformBuffer[0]);
            vsResourceSet[1] = new ResourceSet(vsResLayout, ubCameraVS[1], Renderer.TransformBuffer[1]);

            psResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(2, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
            };

            psResourceSet[0] = new ResourceSet(psResLayout, ubCameraPS, ubLight, ShadowPass.DepthRT);
            psResourceSet[1] = new ResourceSet(psResLayout, ubCameraPS, ubLight, ShadowPass.DepthRT);
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
                    batches[(int)batch.material.BlendType].Add(batch);

                    if (batch.frameNum != Frame.frameNumber)
                    {
                        batch.frameNum = Frame.frameNumber;
                        batch.offset = Renderer.GetTransform(batch.worldTransform, (uint)batch.numWorldTransforms);
                    }

                }
            }
        
        }

        public void Update(ref FrameInfo frame)
        {
            Profiler.BeginSample("ViewUpdate");

            var g = Graphics.Instance;

            this.frameInfo = frame;
            this.frameInfo.camera = camera;
            this.frameInfo.viewSize = new Int2((int)g.Width, (int)g.Height);

            if (FrameGraph == null)
            {
                FrameGraph =  FrameGraph.Simple();
                FrameGraph.Init();
            }

            Viewport.Define(0, 0, g.Width, g.Height);

            if (camera && camera.AutoAspectRatio)
            {
                camera.SetAspectRatio((float)frameInfo.viewSize.X / (float)frameInfo.viewSize.Y);
            }

            drawables.Clear();
            lights.Clear();
            opaqueBatches.Clear();

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

            FrameGraph.Update(this);

            if (DrawDebug)
            {
                debugPass?.Update(this);
            }

            OverlayPass?.Update(this);

            Profiler.EndSample();
        }

        public void Render()
        {
            FrameGraph.Draw(this);

            if (DrawDebug)
            {
                debugPass?.Draw(this);
            }

            OverlayPass?.Draw(this);

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
            //camera.GetHalfViewSize()
            //cameraVS.FrustumSize = camera.Frustum;
            cameraVS.NearClip = camera.NearClip;
            cameraVS.FarClip = camera.FarClip;

            ubCameraVS.SetData(ref cameraVS);
            ubCameraVS.Flush();

            cameraPS.ViewInv = cameraVS.ViewInv;
            cameraPS.CameraPos = camera.Node.Position;
            cameraPS.NearClip = camera.NearClip;
            cameraPS.FarClip = camera.FarClip;

            ubCameraPS.SetData(ref cameraPS);

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

        public void Submit(int imageIndex)
        {
            Profiler.BeginSample("Submit");

            FrameGraph?.Submit(imageIndex);

            if (DrawDebug)
            {
                debugPass?.Submit(imageIndex);
            }

            OverlayPass?.Submit(imageIndex);

            Profiler.EndSample();
        }
    }
    

}
