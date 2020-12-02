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
        private Scene scene;
        public Scene Scene => scene;

        private Camera camera;
        public Camera Camera => camera;

        public RenderPipeline Renderer { get; set; }

        private VkViewport viewport;
        public ref VkViewport Viewport => ref viewport;
        public VkRect2D ViewRect => new VkRect2D((int)Viewport.x, (int)Viewport.y, (int)Viewport.width, (int)Viewport.height);

        public float Width => viewport.width;
        public float Height => viewport.height;

        bool drawDebug = true;
        public bool DrawDebug
        {
            get => FrameGraph.drawDebug && drawDebug; 
            
            set
            {
                drawDebug = value;
            }
        }

        FrameGraphPass debugPass;

        public uint ViewMask { get; set; } = 1;
        public ref FrameInfo Frame => ref frameInfo;
        public ref LightUBO LightParam => ref lightUBO;

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

        private GlobalUBO cameraUBO = new GlobalUBO();
        private LightUBO lightUBO = new LightUBO();

        public SharedBuffer ubGlobal;

        internal Buffer ubLight;

        private DescriptorSetLayout set0Layout;

        public DescriptorSet Set0 => descriptorSetGlobal;
        DescriptorSet descriptorSetGlobal;

        private DescriptorSetLayout set1Layout;
        public DescriptorSet Set1 => descriptorSetLight;
        DescriptorSet descriptorSetLight;

        Graphics Graphics => Graphics.Instance;
        FrameGraph FrameGraph => FrameGraph.Instance;

        public RenderView()
        {
            CreateBuffers();

            CreateDebugPass();
        }

        public void Reset()
        {
            Renderer?.DeviceReset();
            debugPass?.DeviceReset();
        }

        public void Attach(Camera camera, Scene scene, RenderPipeline frameGraph = null)
        {
            this.scene = scene;
            this.camera = camera;
            Renderer = frameGraph;
            Renderer?.Init(this);
            Renderer?.Add(debugPass);
        }

        protected void CreateBuffers()
        {
            ubGlobal = new SharedBuffer(VkBufferUsageFlags.UniformBuffer, (uint)Utilities.SizeOf<GlobalUBO>());

            ubLight = Buffer.CreateUniformBuffer<LightUBO>();

            set0Layout = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.All),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.UniformBufferDynamic, VkShaderStageFlags.Vertex),
            };

            descriptorSetGlobal = new DescriptorSet(set0Layout, ubGlobal, FrameGraph.TransformBuffer);

            set1Layout = new DescriptorSetLayout(1)
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment),
            };

            descriptorSetLight = new DescriptorSet(set1Layout, ubLight, ShadowPass.DepthRT);

        }

        void CreateDebugPass()
        {
            if(debugPass != null)
            {
                return;
            }

            debugPass = new FrameGraphPass
            {
                new GraphicsSubpass
                {
                   OnDraw = DrawOverlay
                }
            };

            debugPass.renderPassCreator = () => Graphics.CreateRenderPass();
            //debugPass.frameBufferCreator = () => Graphics.CreateSwapChainFramebuffers(rp);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void AddDrawable(Drawable drawable)
        {
            if (drawable.DrawableFlags == Drawable.DRAWABLE_LIGHT)
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

            Viewport = new VkViewport(0, 0, g.Width, g.Height);

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

            if (camera != null)
            {
                UpdateGlobalParameters();
            }

            UpdateLightParameters();

            UpdateGeometry();

            if (Renderer == null)
            {
                Renderer = new ForwardRenderer();
                Renderer.Init(this);
                Renderer.Add(debugPass);
            }

            Renderer.Update();

//             if (DrawDebug)
//             {
//                 if(debugPass == null)
//                 {
//                     CreateDebugPass();
//                 }
// 
//                 debugPass.Update();
//             }

            Profiler.EndSample();
        }

        public void Render(RenderContext rc)
        {
            Renderer.Draw(rc);

//             if (DrawDebug)
//             {
//                 debugPass?.Draw(rc, rc.RenderCmdBuffer);
//             }


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

        private void UpdateGlobalParameters()
        {
            cameraUBO.View = camera.View;
            cameraUBO.ViewInv = glm.inverse(cameraUBO.View);
            cameraUBO.Proj = camera.VkProjection;
            cameraUBO.ProjInv = glm.inverse(cameraUBO.Proj);
            cameraUBO.ViewProj = camera.VkProjection * camera.View;
            cameraUBO.ViewProjInv = glm.inverse(cameraUBO.ViewProj);
            cameraUBO.CameraPos = camera.Node.Position;
            cameraUBO.CameraDir = camera.Node.WorldDirection;
            cameraUBO.NearClip = camera.NearClip;
            cameraUBO.FarClip = camera.FarClip;

            ubGlobal.SetData(ref cameraUBO);
            ubGlobal.Flush();


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

            if (env)
            {
                lightUBO.AmbientColor = env.AmbientColor;
                lightUBO.SunlightColor = env.SunlightColor;
                lightUBO.SunlightDir = env.SunlightDir;
            }
            else
            {

                lightUBO.AmbientColor = new Color4(0.15f, 0.15f, 0.25f, 1.0f);
                lightUBO.SunlightColor = new Color4(0.5f);
                lightUBO.SunlightDir = new vec3(-1, -1, 1);
                lightUBO.SunlightDir.Normalize();

                for (int i = 0; i < 8; i++)
                {
                    lightUBO.lightColor[i] = Color4.White;
                    lightUBO.lightVec[i] = glm.normalize(lightVec[i]);
                }

            }

            ubLight.SetData(ref lightUBO);
        }

        void DrawOverlay(GraphicsSubpass pass, RenderContext rc, CommandBuffer cmdBuffer)
        {
            if(DrawDebug)
                OnDrawDebug(cmdBuffer);

            this.SendGlobalEvent(new DrawEvent { rendrPass = pass.FrameGraphPass.RenderPass, renderContext = rc, cmd = cmdBuffer });
        }

        void OnDrawDebug(CommandBuffer cmdBuffer)
        {
            if (Scene == null)
            {
                return;
            }

            var debug = Scene.GetComponent<DebugRenderer>();
            if (debug == null)
            {
                return;
            }

            debug.Render(this, cmdBuffer);


        }
    }


}
