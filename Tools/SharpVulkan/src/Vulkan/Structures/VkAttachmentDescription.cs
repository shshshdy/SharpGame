﻿// Copyright (c) BobbyBao and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

namespace SharpGame
{
    /// <summary>
    ///  Structure specifying an attachment description.
    /// </summary>
    public partial struct VkAttachmentDescription
    {
        public VkAttachmentDescription(
            VkFormat format,
            VkSampleCountFlags samples = VkSampleCountFlags.Count1,
            VkAttachmentLoadOp loadOp = VkAttachmentLoadOp.Clear,
            VkAttachmentStoreOp storeOp = VkAttachmentStoreOp.Store,
            VkAttachmentLoadOp stencilLoadOp = VkAttachmentLoadOp.DontCare,
            VkAttachmentStoreOp stencilStoreOp = VkAttachmentStoreOp.DontCare,
            VkImageLayout initialLayout = VkImageLayout.Undefined,
            VkImageLayout finalLayout = VkImageLayout.PresentSrcKHR,
            VkAttachmentDescriptionFlags flags = VkAttachmentDescriptionFlags.None)
        {
            this.flags = flags;
            this.format = format;
            this.samples = samples;
            this.loadOp = loadOp;
            this.storeOp = storeOp;
            this.stencilLoadOp = stencilLoadOp;
            this.stencilStoreOp = stencilStoreOp;
            this.initialLayout = initialLayout;
            this.finalLayout = finalLayout;
        }
        
    }
}
