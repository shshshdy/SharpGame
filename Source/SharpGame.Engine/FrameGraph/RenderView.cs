﻿using System;
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

        public bool DrawDebug { get => FrameGraph.drawDebug && drawDebug; set => drawDebug = value; }
        bool drawDebug = true;
        FrameGraphPass debugPass;

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
        private LightParameter lightParameter = new LightParameter();

        internal Buffer ubFrameInfo;
        public SharedBuffer ubCameraVS;

        internal SharedBuffer ubCameraPS;
        internal Buffer ubLight;

        private DescriptorSetLayout vsResLayout;

        public DescriptorSet Set0 => vsResourceSet;
        DescriptorSet vsResourceSet;

        private DescriptorSetLayout psResLayout;
        public DescriptorSet Set1 => psResourceSet;
        DescriptorSet psResourceSet;

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
            Renderer?.DeviceReset();
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

            ubCameraVS = new SharedBuffer(VkBufferUsageFlags.UniformBuffer, (uint)Utilities.SizeOf<CameraVS>());

            ubLight = Buffer.CreateUniformBuffer<LightParameter>();

            vsResLayout = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.All),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.UniformBufferDynamic, VkShaderStageFlags.Vertex),
            };

            vsResourceSet = new DescriptorSet(vsResLayout, ubCameraVS, FrameGraph.TransformBuffer);

            psResLayout = new DescriptorSetLayout(1)
            {
                //new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment),
            };

            psResourceSet = new DescriptorSet(psResLayout, ubLight, ShadowPass.DepthRT);

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

        public void Render(RenderContext rc)
        {
            Renderer.Draw(rc);

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
            cameraVS.Proj = camera.Projection;
            cameraVS.ProjInv = glm.inverse(cameraVS.Proj);
            cameraVS.ViewProj = camera.VkProjection*camera.View;
            cameraVS.ViewProjInv = glm.inverse(cameraVS.ViewProj);
            cameraVS.CameraPos = camera.Node.Position;

            camera.GetFrustumSize(out var nearVector, out var farVector);

            float nearClip = camera.NearClip;
            float farClip = camera.FarClip;

            cameraVS.NearClip = camera.NearClip;
            cameraVS.FarClip = camera.FarClip;

            ubCameraVS.SetData(ref cameraVS);
            ubCameraVS.Flush();


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
