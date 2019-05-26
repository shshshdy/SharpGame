using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public enum ImageCreateFlags
    {
        None = 0,
        SparseBinding = 1,
        SparseResidency = 2,
        SparseAliased = 4,
        MutableFormat = 8,
        CubeCompatible = 16,
        _2dArrayCompatibleKHR = 32,
        BindSfrKHX = 64,
        BlockTexelViewCompatibleKHR = 128,
        ExtendedUsageKHR = 256,
        DisjointKHR = 512,
        AliasKHR = 1024,
        SampleLocationsCompatibleDepthEXT = 4096
    }

    public enum ImageType
    {
        Image1D = 0,
        Image2D = 1,
        Image3D = 2
    }

    public enum ImageUsageFlags
    {
        None = 0,
        TransferSrc = 1,
        TransferDst = 2,
        Sampled = 4,
        Storage = 8,
        ColorAttachment = 16,
        DepthStencilAttachment = 32,
        TransientAttachment = 64,
        InputAttachment = 128
    }

    public enum ImageLayout
    {
        Undefined = 0,
        General = 1,
        ColorAttachmentOptimal = 2,
        DepthStencilAttachmentOptimal = 3,
        DepthStencilReadOnlyOptimal = 4,
        ShaderReadOnlyOptimal = 5,
        TransferSrcOptimal = 6,
        TransferDstOptimal = 7,
        Preinitialized = 8,
        PresentSrcKHR = 1000001002,
        SharedPresentKHR = 1000111000,
        DepthReadOnlyStencilAttachmentOptimalKHR = 1000117000,
        DepthAttachmentStencilReadOnlyOptimalKHR = 1000117001
    }

    public class Image : DisposeBase
    {
        internal VkImage handle;

        public Image(ref ImageCreateInfo imageCreateInfo)
        {
            imageCreateInfo.ToNative(out VkImageCreateInfo native);
            handle = Device.CreateImage(ref native);
        }

        protected override void Destroy()
        {
            Device.Destroy(handle);
        }

    }


    public struct ImageCreateInfo
    {
        public ImageCreateFlags flags;
        public ImageType imageType;
        public Format format;
        public Extent3D extent;
        public int mipLevels;
        public int arrayLayers;
        public SampleCountFlags samples;
        public VkImageTiling tiling;
        public ImageUsageFlags usage;
        public VkSharingMode sharingMode;
        public uint[] queueFamilyIndices;
        public ImageLayout initialLayout;
        internal unsafe void ToNative(out VkImageCreateInfo native)
        {
            native = VkImageCreateInfo.New();
            native.flags = (VkImageCreateFlags)flags;
            native.imageType = (VkImageType)imageType;
            native.format = (VkFormat)format;
            native.extent = new VkExtent3D { width = extent.width, height = extent.height, depth = extent.depth };
            native.mipLevels = (uint)mipLevels;
            native.arrayLayers = (uint)arrayLayers;
            native.samples = (VkSampleCountFlags)samples;
            native.tiling = (VkImageTiling)tiling;
            native.usage = (VkImageUsageFlags)usage;
            native.sharingMode = (VkSharingMode)sharingMode;

            if (!queueFamilyIndices.IsNullOrEmpty())
            {
                native.queueFamilyIndexCount = (uint)queueFamilyIndices.Length;
                native.pQueueFamilyIndices = (uint*)Unsafe.AsPointer(ref queueFamilyIndices[0]);
            }

            native.initialLayout = (VkImageLayout)initialLayout;

        }
    }

}
