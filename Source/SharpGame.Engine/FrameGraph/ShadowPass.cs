using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public struct Cascade
    {
        public Framebuffer frameBuffer;
        public DescriptorSet descriptorSet;
        public ImageView view;

        public float splitDepth;
        public mat4 viewProjMatrix;

    }

    public class ShadowPass : FrameGraphPass
    {
        const int SHADOW_MAP_CASCADE_COUNT = 4;
        const uint SHADOWMAP_DIM = 2048;

        static RenderTexture depthRT;
        public static RenderTexture DepthRT
        {
            get
            {
                if (depthRT == null)
                {
                    var depthFormat = Device.GetSupportedDepthFormat();
                    depthRT = new RenderTexture(SHADOWMAP_DIM, SHADOWMAP_DIM, SHADOW_MAP_CASCADE_COUNT, depthFormat,
                        VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.Sampled, //ImageAspectFlags.Depth,
                        VkSampleCountFlags.Count1/*, ImageLayout.DepthStencilReadOnlyOptimal*/);
                    depthRT.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
                    depthRT.UpdateDescriptor();
                }

                return depthRT;
            }
        }

        Cascade[] cascades = new Cascade[SHADOW_MAP_CASCADE_COUNT];

        SharedBuffer ubShadow;

        float cascadeSplitLambda = 0.95f;

        Shader depthShader;

        DescriptorSet vsSet;

        FastList<SourceBatch>[] casters = new FastList<SourceBatch>[]
        {
            new FastList<SourceBatch>(), new FastList<SourceBatch>(), new FastList<SourceBatch>()
        };
        FrustumOctreeQuery shadowCasterQuery = new FrustumOctreeQuery();

        ulong passID = Pass.GetID(Pass.Shadow);
        public ShadowPass() : base(SubmitQueue.EarlyGraphics)
        {
            var depthFormat = Device.GetSupportedDepthFormat();

            for (uint i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                cascades[i].view = ImageView.Create(DepthRT.image, ImageViewType.Image2D, depthFormat, VkImageAspectFlags.Depth, 0, 1, i, 1);
            }

            ubShadow = new SharedBuffer(VkBufferUsageFlags.UniformBuffer, (uint)(SHADOW_MAP_CASCADE_COUNT * Utilities.SizeOf<mat4>()));

            depthShader = Resources.Instance.Load<Shader>("shaders/shadow.shader");

            vsSet = new DescriptorSet(depthShader.Main.GetResourceLayout(0), ubShadow, FrameGraph.TransformBuffer);

        }

        protected override void CreateRenderPass()
        {
            var depthFormat = Device.GetSupportedDepthFormat();

            AttachmentDescription[] attachments =
            {
                new AttachmentDescription(depthFormat, finalLayout :ImageLayout.ShaderReadOnlyOptimal /*ImageLayout.DepthStencilReadOnlyOptimal*/)
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
                    srcSubpass = Vulkan.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.FragmentShader,
                    dstStageMask = PipelineStageFlags.EarlyFragmentTests,
                    srcAccessMask = AccessFlags.ShaderRead,
                    dstAccessMask = AccessFlags.DepthStencilAttachmentWrite,
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = Vulkan.SubpassExternal,
                    srcStageMask = PipelineStageFlags.LateFragmentTests,
                    dstStageMask = PipelineStageFlags.FragmentShader,
                    srcAccessMask =  AccessFlags.DepthStencilAttachmentWrite,
                    dstAccessMask = AccessFlags.ShaderRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            RenderPass = new RenderPass(attachments, subpassDescription, dependencies);
        }

        protected override void CreateRenderTargets()
        {
            for (uint i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                cascades[i].frameBuffer = SharpGame.Framebuffer.Create(RenderPass, SHADOWMAP_DIM, SHADOWMAP_DIM, 1, new[] { cascades[i].view });
            }

        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            var view = View;
            if(view.Camera == null)
            {
                return;
            }

            UpdateCascades(view);
            UpdateUniformBuffers(view);

            casters[0].Clear();
            casters[1].Clear();
            casters[2].Clear();

            shadowCasterQuery.Init(view.Camera, Drawable.DRAWABLE_ANY, view.ViewMask);

            view.Scene.GetDrawables(shadowCasterQuery, (drawable) =>
            {
                if(drawable.CastShadows)
                {
                    foreach (SourceBatch batch in drawable.Batches)
                    {
                        casters[batch.material.blendType].Add(batch);

                        if(batch.frameNum != view.Frame.frameNumber)
                        {
                            batch.frameNum = view.Frame.frameNumber;
                            batch.offset = FrameGraph.GetTransform(batch.worldTransform, (uint)batch.numWorldTransforms);
                        }
                    }
                }
            });

            ClearValue[] clearDepth = { new ClearDepthStencilValue(1.0f, 0) };

            if (RenderPass == null)
            {
                CreateRenderPass();
                CreateRenderTargets();

            }

            //todo:multi thread
            for (int i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                VkViewport viewport = new VkViewport(0, 0, SHADOWMAP_DIM, SHADOWMAP_DIM, 0.0f, 1.0f);
                VkRect2D renderArea = new VkRect2D(0, 0, SHADOWMAP_DIM, SHADOWMAP_DIM);

                BeginRenderPass(cmd, cascades[i].frameBuffer, renderArea, clearDepth);

                cmd.SetViewport(viewport);
                cmd.SetScissor(renderArea);

                uint cascade = (uint)i;

                Span<ConstBlock> consts = stackalloc ConstBlock[]
                {
                    new ConstBlock(ShaderStage.Vertex, 0, 4, Utilities.AsPointer(ref cascade))
                };

                foreach (var batch in casters[0])
                {
                    DrawBatch(cmd, passID, batch, consts, vsSet);
                }

                EndRenderPass(cmd);
            }

        }

        void DrawBatch(CommandBuffer cb, ulong passID, SourceBatch batch, Span<ConstBlock> pushConsts,
            DescriptorSet resourceSet)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }

            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(RenderPass, 0, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet, batch.offset);

            foreach (ConstBlock constBlock in pushConsts)
            {
                cb.PushConstants(pass.PipelineLayout, constBlock.range.stageFlags, constBlock.range.offset, constBlock.range.size, constBlock.data);
            }

            batch.material.Bind(pass.passIndex, cb);
            batch.geometry.Draw(cb);
        }

        /*
            Calculate frustum split depths and matrices for the shadow map cascades
            Based on https://johanmedestrom.wordpress.com/2016/03/18/opengl-cascaded-shadow-maps/
        */
        void UpdateCascades(RenderView view)
        {
            var camera = view.Camera;

            vec3 lightDir = view.LightParam.SunlightDir;
            Span<float> cascadeSplits = stackalloc float[SHADOW_MAP_CASCADE_COUNT];
            
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
                float p = (i + 1) / (float)(SHADOW_MAP_CASCADE_COUNT);
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

                mat4 lightViewMatrix = glm.lookAt(frustumCenter - lightDir * -minExtents.z, frustumCenter, glm.vec3(0.0f, 1.0f, 0.0f));
                mat4 lightOrthoMatrix = glm.ortho(minExtents.x, maxExtents.x, minExtents.y, maxExtents.y, 0.0f, maxExtents.z - minExtents.z);
                // Store split distance and matrix in cascade
                cascades[i].splitDepth = (camera.NearClip + splitDist * clipRange);
                cascades[i].viewProjMatrix = lightOrthoMatrix * lightViewMatrix;

                view.LightParam.SetLightMatrices(i, ref cascades[i].viewProjMatrix);

                lastSplitDist = cascadeSplits[i];
            }
        }

        void UpdateUniformBuffers(RenderView view)
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
