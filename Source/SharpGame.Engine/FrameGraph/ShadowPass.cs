using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public struct Cascade
    {
        public Framebuffer frameBuffer;
        public ResourceSet descriptorSet;
        public ImageView view;

        public float splitDepth;
        public mat4 viewProjMatrix;

    }

    public class ShadowPass : GraphicsPass
    {
        const int SHADOW_MAP_CASCADE_COUNT = 4;
        const uint SHADOWMAP_DIM = 2048;

        RenderTarget depthRT;
        Cascade[] cascades = new Cascade[SHADOW_MAP_CASCADE_COUNT];

        DoubleBuffer ubShadow;

        float cascadeSplitLambda = 0.95f;

        Shader depthShader;

        ResourceSet[] vsSet = new ResourceSet[2];

        FastList<SourceBatch> casters = new FastList<SourceBatch>();

        ResourceSet VSSet => vsSet[Graphics.Instance.WorkContext];
        public ShadowPass() : base(Pass.Depth)
        {
            var depthFormat = Device.GetSupportedDepthFormat();

            AttachmentDescription[] attachments =
            {
                new AttachmentDescription(depthFormat, finalLayout : ImageLayout.DepthStencilReadOnlyOptimal)
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,
                    
                    pDepthStencilAttachment = new []
                    {
                        new AttachmentReference(0, ImageLayout.DepthStencilAttachmentOptimal)
                    },
                }
            };

            // Subpass dependencies for layout transitions
            SubpassDependency[] dependencies =
            {
                new SubpassDependency
                {
                    srcSubpass = VulkanNative.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = AccessFlags.MemoryRead,
                    dstAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            renderPass = new RenderPass(ref renderPassInfo);

            depthRT = new RenderTarget(SHADOWMAP_DIM, SHADOWMAP_DIM, SHADOW_MAP_CASCADE_COUNT, depthFormat, ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Depth);
          
            for (uint i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                cascades[i].view = ImageView.Create(depthRT.image, ImageViewType.Image2D, depthFormat, ImageAspectFlags.Depth, 0, 1, i, 1);
                cascades[i].frameBuffer = Framebuffer.Create(renderPass, SHADOWMAP_DIM, SHADOWMAP_DIM, 1, new[] { cascades[i].view });
                Renderer.Instance.AddDebugImage(cascades[i].view);
            }

            ubShadow = new DoubleBuffer(BufferUsageFlags.UniformBuffer, (uint)(SHADOW_MAP_CASCADE_COUNT * Utilities.SizeOf<mat4>()));

            depthShader = Resources.Instance.Load<Shader>("shaders/shadow.shader");

            vsSet[0] = new ResourceSet(depthShader.Main.GetResourceLayout(0), ubShadow[0]);
            vsSet[1] = new ResourceSet(depthShader.Main.GetResourceLayout(0), ubShadow[1]);

            //ClearDepthStencilValue = new ClearDepthStencilValue(0, 0);
        }


        protected override void DrawImpl(RenderView view)
        {
            updateCascades(view);
            updateUniformBuffers(view);

            casters.Clear();

            view.Scene.GetDrawables(null, (drawable) =>
            {
                if(drawable.CastShadows)
                {
                    foreach (SourceBatch batch in drawable.Batches)
                    {
                        casters.Add(batch);
                        //batch.offset = GetTransform(batch.worldTransform, (uint)batch.numWorldTransforms);
                    }
                }
            });

            //todo:multi thread
            for (int i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                int workContext = Graphics.Instance.nextImage;
                var cmd = cmdBufferPool[workContext].Get();
                cmd.renderPass = renderPass;

                CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
                {
                    framebuffer = cascades[i].frameBuffer,
                    renderPass = renderPass
                };

                cmd.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                    | CommandBufferUsageFlags.SimultaneousUse, ref inherit);

                Viewport viewport = new Viewport(0, 0, (float)SHADOWMAP_DIM, (float)SHADOWMAP_DIM, 0.0f, 1.0f);
                Rect2D scissor = new Rect2D(0, 0, SHADOWMAP_DIM, SHADOWMAP_DIM);

                cmd.SetViewport(ref viewport);
                cmd.SetScissor(scissor);

                foreach (var batch in casters)
                {
                    DrawShadowBatch(cmd, batch, (uint)i, VSSet, null);
                }
            }

        }

        public void DrawShadowBatch(CommandBuffer cb, SourceBatch batch, uint cascade, ResourceSet resourceSet, ResourceSet resourceSet1)
        {
            var shader = depthShader;

            var pass = shader.Main;// shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(renderPass, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.BindGraphicsResourceSet(pass.PipelineLayout, resourceSet.Set, resourceSet);

            if (resourceSet1 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, resourceSet1.Set, resourceSet1);
            }

            cb.PushConstants(pass.PipelineLayout, ShaderStage.Vertex, 0, 64, batch.worldTransform);
            cb.PushConstants(pass.PipelineLayout, ShaderStage.Vertex, 64, ref cascade);

            batch.geometry.Draw(cb);
        }

        public override void Submit(int imageIndex)
        {
            var g = Graphics.Instance;
            CommandBuffer cb = g.RenderCmdBuffer;

            Viewport viewport = new Viewport(0, 0, (float)SHADOWMAP_DIM, (float)SHADOWMAP_DIM, 0.0f, 1.0f);           
            Rect2D scissor = new Rect2D(0, 0, SHADOWMAP_DIM, SHADOWMAP_DIM);

            cb.SetViewport(ref viewport);
            cb.SetScissor(scissor);

            var cmdPool = cmdBufferPool[imageIndex];

            for (int i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                var fb = cascades[i].frameBuffer;
                var renderPassBeginInfo = new RenderPassBeginInfo
                (
                    fb.renderPass, fb,
                    new Rect2D(0, 0, fb.Width, fb.Height),
                    ClearDepthStencilValue
                );

                cb.BeginRenderPass(ref renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);
               
                cb.ExecuteCommand(cmdPool[i]);

                cb.EndRenderPass();
            }

        }

        /*
            Calculate frustum split depths and matrices for the shadow map cascades
            Based on https://johanmedestrom.wordpress.com/2016/03/18/opengl-cascaded-shadow-maps/
        */
        void updateCascades(RenderView view)
        {
            vec3 lightDir = view.LightParam.SunlightDir;
            Span<float> cascadeSplits = stackalloc float[SHADOW_MAP_CASCADE_COUNT];
            var camera = view.Camera;
            float nearClip = camera.NearClip;
            float farClip = camera.FarClip;
            float clipRange = farClip - nearClip;

            float minZ = nearClip;
            float maxZ = nearClip + clipRange;

            float range = maxZ - minZ;
            float ratio = maxZ / minZ;
           
            // Calculate split depths based on view camera furstum
            // Based on method presentd in https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch10.html
            for (int i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                float p = (i + 1) / (SHADOW_MAP_CASCADE_COUNT);
                float log = minZ * (float)Math.Pow(ratio, p);
                float uniform = minZ + range * p;
                float d = cascadeSplitLambda * (log - uniform) + uniform;
                cascadeSplits[i] = (d - nearClip) / clipRange;
            }

            // Calculate orthographic projection matrix for each cascade
            float lastSplitDist = 0.0f;
            for (int i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                float splitDist = cascadeSplits[i];

                Span<vec3> frustumCorners = stackalloc []
                {
                    new vec3(-1.0f,  1.0f, -1.0f),
                    new vec3( 1.0f,  1.0f, -1.0f),
                    new vec3( 1.0f, -1.0f, -1.0f),
                    new vec3(-1.0f, -1.0f, -1.0f),
                    new vec3(-1.0f,  1.0f,  1.0f),
                    new vec3( 1.0f,  1.0f,  1.0f),
                    new vec3( 1.0f, -1.0f,  1.0f),
                    new vec3(-1.0f, -1.0f,  1.0f),
                };

                // Project frustum corners into world space
                mat4 invCam = glm.inverse(camera.Projection * camera.View);
                for (int j = 0; j < 8; j++)
                {
                    vec4 invCorner = invCam * glm.vec4(frustumCorners[j], 1.0f);
                    frustumCorners[j] = (vec3)invCorner / invCorner.w;
                }

                for (int j = 0; j < 4; j++)
                {
                    vec3 dist = frustumCorners[j + 4] - frustumCorners[j];
                    frustumCorners[j + 4] = frustumCorners[j] + (dist * splitDist);
                    frustumCorners[j] = frustumCorners[j] + (dist * lastSplitDist);
                }

                // Get frustum center
                vec3 frustumCenter = vec3.Zero;
                for (int j = 0; j < 8; j++)
                {
                    frustumCenter += frustumCorners[j];
                }
                frustumCenter /= 8.0f;

                float radius = 0.0f;
                for (int j = 0; j < 8; j++)
                {
                    float distance = glm.length(frustumCorners[j] - frustumCenter);
                    radius = Math.Max(radius, distance);
                }
                radius = (float)Math.Ceiling(radius * 16.0f) / 16.0f;

                vec3 maxExtents = glm.vec3(radius);
                vec3 minExtents = -maxExtents;

                //vec3 lightDir = glm.normalize(-lightPos);
                mat4 lightViewMatrix = glm.lookAt(frustumCenter - lightDir * -minExtents.z, frustumCenter, glm.vec3(0.0f, 1.0f, 0.0f));
                mat4 lightOrthoMatrix = glm.ortho(minExtents.x, maxExtents.x, minExtents.y, maxExtents.y, 0.0f, maxExtents.z - minExtents.z);

                // Store split distance and matrix in cascade
                cascades[i].splitDepth = (camera.NearClip + splitDist * clipRange) * -1.0f;
                cascades[i].viewProjMatrix = lightOrthoMatrix * lightViewMatrix;

                view.LightParam.SetLightMatrices(i, ref cascades[i].viewProjMatrix);

                lastSplitDist = cascadeSplits[i];
            }
        }

        void updateUniformBuffers(RenderView view)
        {
            uint offset = 0;
            for (int i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                ubShadow.SetData(ref cascades[i].viewProjMatrix, offset);
                offset += 64;
            }
            ubShadow.Flush();

            for (int i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                view.LightParam.cascadeSplits[i] = cascades[i].splitDepth;
                view.LightParam.SetLightMatrices(i, ref cascades[i].viewProjMatrix);
            }
        }
    }
}
