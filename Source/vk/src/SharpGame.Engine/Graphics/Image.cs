using System;
using System.Collections.Generic;
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

    public struct ImageCreateInfo
    {
        public ImageCreateFlags flags;
        public ImageType imageType;
        public Format format;
        public Extent3D extent;
        public uint mipLevels;
        public uint arrayLayers;
        public VkSampleCountFlags samples;
        public VkImageTiling tiling;
        public ImageUsageFlags usage;
        public VkSharingMode sharingMode;
        //public uint queueFamilyIndexCount;
        //public uint* pQueueFamilyIndices;
        public VkImageLayout initialLayout;

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

    }
}
