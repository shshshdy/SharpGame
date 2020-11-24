﻿// Copyright (c) BobbyBao and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

namespace SharpGame
{
    /// <summary>
    /// Structure specifying the parameters of an image memory barrier.
    /// </summary>
    public partial struct VkImageMemoryBarrier
    {
        public unsafe VkImageMemoryBarrier(
            VkImage image,
            VkImageSubresourceRange subresourceRange,
            VkAccessFlags srcAccessMask,
            VkAccessFlags dstAccessMask,
            VkImageLayout oldLayout,
            VkImageLayout newLayout,
            uint srcQueueFamilyIndex = Vulkan.QueueFamilyIgnored,
            uint dstQueueFamilyIndex = Vulkan.QueueFamilyIgnored,
            void* pNext = default)
        {
            sType = VkStructureType.ImageMemoryBarrier;
            this.pNext = pNext;
            this.srcAccessMask = srcAccessMask;
            this.dstAccessMask = dstAccessMask;
            this.oldLayout = oldLayout;
            this.newLayout = newLayout;
            this.srcQueueFamilyIndex = srcQueueFamilyIndex;
            this.dstQueueFamilyIndex = dstQueueFamilyIndex;
            this.image = image;
            this.subresourceRange = subresourceRange;
        }

        public unsafe VkImageMemoryBarrier(VkImage image, VkAccessFlags srcAccessMask, VkAccessFlags dstAccessMask, VkImageLayout oldLayout, VkImageLayout newLayout,
            VkImageAspectFlags aspectMask = VkImageAspectFlags.Color, uint baseMipLevel = 0, uint levelCount = uint.MaxValue)
        {
            this.sType = VkStructureType.ImageMemoryBarrier;
            this.pNext = null;
            this.srcAccessMask = (VkAccessFlags)srcAccessMask;
            this.dstAccessMask = (VkAccessFlags)dstAccessMask;
            this.oldLayout = (VkImageLayout)oldLayout;
            this.newLayout = (VkImageLayout)newLayout;
            this.srcQueueFamilyIndex = uint.MaxValue;
            this.dstQueueFamilyIndex = uint.MaxValue;
            this.image = image;
            this.subresourceRange.aspectMask = (VkImageAspectFlags)aspectMask;
            this.subresourceRange.baseMipLevel = baseMipLevel;
            this.subresourceRange.baseArrayLayer = 0;
            this.subresourceRange.levelCount = levelCount;
            this.subresourceRange.layerCount = uint.MaxValue;
        }

        public unsafe VkImageMemoryBarrier(
            VkImage image,
            VkImageLayout oldLayout,
            VkImageLayout newLayout,
            VkImageSubresourceRange subresourceRange,
            void* pNext = default)
        {
            sType = VkStructureType.ImageMemoryBarrier;
            this.pNext = pNext;
            srcAccessMask = 0;
            dstAccessMask = 0;

            // Source layouts (old)
            // Source access mask controls actions that have to be finished on the old layout
            // before it will be transitioned to the new layout
            switch (oldLayout)
            {
                case VkImageLayout.Undefined:
                    // Image layout is undefined (or does not matter)
                    // Only valid as initial layout
                    // No flags required, listed only for completeness
                    srcAccessMask = 0;
                    break;

                case VkImageLayout.Preinitialized:
                    // Image is preinitialized
                    // Only valid as initial layout for linear images, preserves memory contents
                    // Make sure host writes have been finished
                    srcAccessMask = VkAccessFlags.HostWrite;
                    break;

                case VkImageLayout.ColorAttachmentOptimal:
                    // Image is a color attachment
                    // Make sure any writes to the color buffer have been finished
                    srcAccessMask = VkAccessFlags.ColorAttachmentWrite;
                    break;

                case VkImageLayout.DepthStencilAttachmentOptimal:
                    // Image is a depth/stencil attachment
                    // Make sure any writes to the depth/stencil buffer have been finished
                    srcAccessMask = VkAccessFlags.DepthStencilAttachmentWrite;
                    break;

                case VkImageLayout.TransferSrcOptimal:
                    // Image is a transfer source 
                    // Make sure any reads from the image have been finished
                    srcAccessMask = VkAccessFlags.TransferRead;
                    break;

                case VkImageLayout.TransferDstOptimal:
                    // Image is a transfer destination
                    // Make sure any writes to the image have been finished
                    srcAccessMask = VkAccessFlags.TransferWrite;
                    break;

                case VkImageLayout.ShaderReadOnlyOptimal:
                    // Image is read by a shader
                    // Make sure any shader reads from the image have been finished
                    srcAccessMask = VkAccessFlags.ShaderWrite;
                    break;
                default:
                    // Other source layouts aren't handled (yet)
                    break;
            }

            // Target layouts (new)
            // Destination access mask controls the dependency for the new image layout
            switch (newLayout)
            {
                case VkImageLayout.TransferDstOptimal:
                    // Image will be used as a transfer destination
                    // Make sure any writes to the image have been finished
                    dstAccessMask = VkAccessFlags.TransferWrite;
                    break;

                case VkImageLayout.TransferSrcOptimal:
                    // Image will be used as a transfer source
                    // Make sure any reads from the image have been finished
                    dstAccessMask = VkAccessFlags.TransferRead;
                    break;

                case VkImageLayout.ColorAttachmentOptimal:
                    // Image will be used as a color attachment
                    // Make sure any writes to the color buffer have been finished
                    dstAccessMask = VkAccessFlags.ColorAttachmentWrite;
                    break;

                case VkImageLayout.DepthStencilAttachmentOptimal:
                    // Image layout will be used as a depth/stencil attachment
                    // Make sure any writes to depth/stencil buffer have been finished
                    dstAccessMask |= VkAccessFlags.DepthStencilAttachmentWrite;
                    break;

                case VkImageLayout.ShaderReadOnlyOptimal:
                    // Image will be read in a shader (sampler, input attachment)
                    // Make sure any writes to the image have been finished
                    if (srcAccessMask == 0)
                    {
                        srcAccessMask = VkAccessFlags.HostWrite | VkAccessFlags.TransferWrite;
                    }
                    dstAccessMask = VkAccessFlags.ShaderRead;
                    break;
                default:
                    // Other source layouts aren't handled (yet)
                    break;
            }

            this.oldLayout = oldLayout;
            this.newLayout = newLayout;
            srcQueueFamilyIndex = Vulkan.QueueFamilyIgnored;
            dstQueueFamilyIndex = Vulkan.QueueFamilyIgnored;
            this.image = image;
            this.subresourceRange = subresourceRange;
        }

        public unsafe VkImageMemoryBarrier(
            VkImage image, VkImageAspectFlags aspectMask,
            VkImageLayout oldLayout, VkImageLayout newLayout,
            void* pNext = default)
            : this(image, oldLayout, newLayout, new VkImageSubresourceRange(aspectMask, 0, 1, 0, 1), pNext)
        {
            
        }
    }
}
