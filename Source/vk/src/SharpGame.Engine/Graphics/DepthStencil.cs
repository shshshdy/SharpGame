using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    public class DepthStencil : DisposeBase
    {
        public VkImage image;
        public VkDeviceMemory mem;
        public VkImageView view;

        public unsafe DepthStencil(uint width, uint height, Format format)
        {
            VkImageCreateInfo imageInfo = VkImageCreateInfo.New();
            imageInfo.imageType = VkImageType.Image2D;
            imageInfo.format = (VkFormat)format;
            imageInfo.extent = new VkExtent3D() { width = width, height = height, depth = 1 };
            imageInfo.mipLevels = 1;
            imageInfo.arrayLayers = 1;
            imageInfo.samples = VkSampleCountFlags.Count1;
            imageInfo.tiling = VkImageTiling.Optimal;
            imageInfo.usage = (VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.TransferSrc);
            imageInfo.flags = 0;

            VkMemoryAllocateInfo mem_alloc = VkMemoryAllocateInfo.New();
            mem_alloc.allocationSize = 0;
            mem_alloc.memoryTypeIndex = 0;

            VkImageViewCreateInfo depthStencilView = VkImageViewCreateInfo.New();
            depthStencilView.viewType = VkImageViewType.Image2D;
            depthStencilView.format = (VkFormat)format;
            depthStencilView.flags = 0;
            depthStencilView.subresourceRange = new VkImageSubresourceRange();
            depthStencilView.subresourceRange.aspectMask = (VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil);
            depthStencilView.subresourceRange.baseMipLevel = 0;
            depthStencilView.subresourceRange.levelCount = 1;
            depthStencilView.subresourceRange.baseArrayLayer = 0;
            depthStencilView.subresourceRange.layerCount = 1;

            image = Device.CreateImage(ref imageInfo);
            vkGetImageMemoryRequirements(Graphics.device, image, out VkMemoryRequirements memReqs);
            mem_alloc.allocationSize = memReqs.size;
            mem_alloc.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

            Util.CheckResult(vkAllocateMemory(Graphics.device, &mem_alloc, null, out mem));
            Util.CheckResult(vkBindImageMemory(Graphics.device, image, mem, 0));

            depthStencilView.image = image;
            view = Device.CreateImageView(ref depthStencilView);

        }

        protected override void Destroy()
        {
            Device.DestroyImageView(view);
            Device.DestroyImage(image);
            Device.FreeMemory(mem);

            base.Destroy();
        }
    }

}
