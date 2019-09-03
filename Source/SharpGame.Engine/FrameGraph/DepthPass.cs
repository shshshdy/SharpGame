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

    public class DepthPass : ScenePass
    {
        const int SHADOW_MAP_CASCADE_COUNT = 4;
        const uint SHADOWMAP_DIM = 2048;

        RenderTarget depthRT;
        Cascade[] cascades = new Cascade[SHADOW_MAP_CASCADE_COUNT];

        float cascadeSplitLambda = 0.95f;
        public DepthPass() : base(Pass.Depth)
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

                Span<vec3> frustumCorners = stackalloc [] {
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
                for (int j = 0; j < 8; i++)
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

                lastSplitDist = cascadeSplits[i];
            }
        }

        void updateLight()
        {
        /*
            float angle = glm::radians(timer * 360.0f);
            float radius = 20.0f;
            lightPos = vec3(cos(angle) * radius, -radius, sin(angle) * radius);*/
        }

        void updateUniformBuffers()
        {
        /*
            for (uint32_t i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                depthPass.ubo.cascadeViewProjMat[i] = cascades[i].viewProjMatrix;
            }
            memcpy(depthPass.uniformBuffer.mapped, &depthPass.ubo, sizeof(depthPass.ubo));

            uboVS.projection = camera.matrices.perspective;
            uboVS.view = camera.matrices.view;
            uboVS.model = glm::mat4(1.0f);

            uboVS.lightDir = normalize(-lightPos);

            memcpy(uniformBuffers.VS.mapped, &uboVS, sizeof(uboVS));

            for (uint32_t i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                uboFS.cascadeSplits[i] = cascades[i].splitDepth;
                uboFS.cascadeViewProjMat[i] = cascades[i].viewProjMatrix;
            }
            uboFS.inverseViewMat = glm::inverse(camera.matrices.view);
            uboFS.lightDir = normalize(-lightPos);
            uboFS.colorCascades = colorCascades;
            memcpy(uniformBuffers.FS.mapped, &uboFS, sizeof(uboFS));*/
        }
    }
}
