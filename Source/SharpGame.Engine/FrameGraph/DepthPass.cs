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
        public Matrix viewProjMatrix;

    }

    public class DepthPass : ScenePass
    {
        const uint SHADOW_MAP_CASCADE_COUNT = 4;
        const uint SHADOWMAP_DIM = 2048;

        RenderTarget depthRT;
        Cascade[] cascades = new Cascade[SHADOW_MAP_CASCADE_COUNT];

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
        void updateCascades()
        {/*
            float cascadeSplits[SHADOW_MAP_CASCADE_COUNT];

            float nearClip = camera.getNearClip();
            float farClip = camera.getFarClip();
            float clipRange = farClip - nearClip;

            float minZ = nearClip;
            float maxZ = nearClip + clipRange;

            float range = maxZ - minZ;
            float ratio = maxZ / minZ;

            // Calculate split depths based on view camera furstum
            // Based on method presentd in https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch10.html
            for (uint32_t i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                float p = (i + 1) / static_cast<float>(SHADOW_MAP_CASCADE_COUNT);
                float log = minZ * std::pow(ratio, p);
                float uniform = minZ + range * p;
                float d = cascadeSplitLambda * (log - uniform) + uniform;
                cascadeSplits[i] = (d - nearClip) / clipRange;
            }

            // Calculate orthographic projection matrix for each cascade
            float lastSplitDist = 0.0;
            for (uint32_t i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                float splitDist = cascadeSplits[i];

                glm::vec3 frustumCorners[8] = {
                glm::vec3(-1.0f,  1.0f, -1.0f),
                glm::vec3( 1.0f,  1.0f, -1.0f),
                glm::vec3( 1.0f, -1.0f, -1.0f),
                glm::vec3(-1.0f, -1.0f, -1.0f),
                glm::vec3(-1.0f,  1.0f,  1.0f),
                glm::vec3( 1.0f,  1.0f,  1.0f),
                glm::vec3( 1.0f, -1.0f,  1.0f),
                glm::vec3(-1.0f, -1.0f,  1.0f),
            };

                // Project frustum corners into world space
                glm::mat4 invCam = glm::inverse(camera.matrices.perspective * camera.matrices.view);
                for (uint32_t i = 0; i < 8; i++)
                {
                    glm::vec4 invCorner = invCam * glm::vec4(frustumCorners[i], 1.0f);
                    frustumCorners[i] = invCorner / invCorner.w;
                }

                for (uint32_t i = 0; i < 4; i++)
                {
                    glm::vec3 dist = frustumCorners[i + 4] - frustumCorners[i];
                    frustumCorners[i + 4] = frustumCorners[i] + (dist * splitDist);
                    frustumCorners[i] = frustumCorners[i] + (dist * lastSplitDist);
                }

                // Get frustum center
                glm::vec3 frustumCenter = glm::vec3(0.0f);
                for (uint32_t i = 0; i < 8; i++)
                {
                    frustumCenter += frustumCorners[i];
                }
                frustumCenter /= 8.0f;

                float radius = 0.0f;
                for (uint32_t i = 0; i < 8; i++)
                {
                    float distance = glm::length(frustumCorners[i] - frustumCenter);
                    radius = glm::max(radius, distance);
                }
                radius = std::ceil(radius * 16.0f) / 16.0f;

                glm::vec3 maxExtents = glm::vec3(radius);
                glm::vec3 minExtents = -maxExtents;

                glm::vec3 lightDir = normalize(-lightPos);
                glm::mat4 lightViewMatrix = glm::lookAt(frustumCenter - lightDir * -minExtents.z, frustumCenter, glm::vec3(0.0f, 1.0f, 0.0f));
                glm::mat4 lightOrthoMatrix = glm::ortho(minExtents.x, maxExtents.x, minExtents.y, maxExtents.y, 0.0f, maxExtents.z - minExtents.z);

                // Store split distance and matrix in cascade
                cascades[i].splitDepth = (camera.getNearClip() + splitDist * clipRange) * -1.0f;
                cascades[i].viewProjMatrix = lightOrthoMatrix * lightViewMatrix;

                lastSplitDist = cascadeSplits[i];
            }*/
        }

        void updateLight()
        {
        /*
            float angle = glm::radians(timer * 360.0f);
            float radius = 20.0f;
            lightPos = glm::vec3(cos(angle) * radius, -radius, sin(angle) * radius);*/
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
