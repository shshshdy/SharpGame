﻿// Copyright (c) BobbyBao and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

namespace SharpGame
{
    /// <summary>
    /// Structure specifying an image subresource layers.
    /// </summary>
    public partial struct VkImageSubresourceLayers
    {
        public VkImageSubresourceLayers(
            VkImageAspectFlags aspectMask,
            uint mipLevel,
            uint baseArrayLayer, uint layerCount)
        {
            this.aspectMask = aspectMask;
            this.mipLevel = mipLevel;
            this.baseArrayLayer = baseArrayLayer;
            this.layerCount = layerCount;
        }
    }
}
