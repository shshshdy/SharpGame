using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace SharpGame.Samples
{
    // Virtual texture page as a part of the partially resident texture
    // Contains memory bindings, offsets and status information
    public struct VirtualTexturePage : IDisposable
    {
        public VkOffset3D offset;
        public VkExtent3D extent;
        public VkSparseImageMemoryBind imageMemoryBind;                            // Sparse image memory bind for this page
        public ulong size;                                                  // Page (memory) size in bytes
        public uint mipLevel;                                                  // Mip level that this page belongs to
        public uint layer;                                                     // Array layer that this page belongs to
        public uint index;

        public bool resident()
        {
            return (imageMemoryBind.memory != VkDeviceMemory.Null);
        }

        public void allocate(uint memoryTypeIndex)
        {
            if (imageMemoryBind.memory != VkDeviceMemory.Null)
            {
                return;
            }

            imageMemoryBind = new VkSparseImageMemoryBind();

            var allocInfo = VkMemoryAllocateInfo.New();
            allocInfo.allocationSize = size;
            allocInfo.memoryTypeIndex = memoryTypeIndex;
            imageMemoryBind.memory = Device.AllocateMemory(ref allocInfo);

            // Sparse image memory binding
            imageMemoryBind.subresource = new VkImageSubresource
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = mipLevel,
                arrayLayer = layer,
            };

            imageMemoryBind.extent = extent;
            imageMemoryBind.offset = offset;
        }

        public void Dispose()
        {
            if (imageMemoryBind.memory != VkDeviceMemory.Null)
            {
                Device.FreeMemory(imageMemoryBind.memory);
                imageMemoryBind.memory = VkDeviceMemory.Null;
            }
        }
    }

    // Virtual texture object containing all pages
    public class VirtualTexture : IDisposable
    {
        Image image;                                                      // Texture image handle
        BindSparseInfo bindSparseInfo;                                    // Sparse queue binding information
        NativeList<VirtualTexturePage> pages = new NativeList<VirtualTexturePage>();                              // Contains all virtual pages of the texture
        NativeList<VkSparseImageMemoryBind> sparseImageMemoryBinds = new NativeList<VkSparseImageMemoryBind>();   // Sparse image memory bindings of all memory-backed virtual tables
        NativeList<VkSparseMemoryBind> opaqueMemoryBinds = new NativeList<VkSparseMemoryBind>();                  // Sparse ópaque memory bindings for the mip tail (if present)
        SparseImageMemoryBindInfo[] imageMemoryBindInfo;                    // Sparse image memory bind info
        SparseImageOpaqueMemoryBindInfo[] opaqueMemoryBindInfo;             // Sparse image opaque memory bind info (mip tail)
        uint mipTailStart;                                              // First mip level in mip tail
        VkSparseImageMemoryRequirements sparseImageMemoryRequirements; 
        uint memoryTypeIndex;                                           

        // @todo: comment
        public struct MipTailInfo
        {
            public bool singleMipTail;
            public bool alingedMipSize;
        }

        public MipTailInfo mipTailInfo;

        public ref VirtualTexturePage addPage(VkOffset3D offset, VkExtent3D extent, ulong size, uint mipLevel, uint layer)
        {
            VirtualTexturePage newPage = new VirtualTexturePage();
            newPage.offset = offset;
            newPage.extent = extent;
            newPage.size = size;
            newPage.mipLevel = mipLevel;
            newPage.layer = layer;
            newPage.index = (uint)pages.Count;
            newPage.imageMemoryBind = new VkSparseImageMemoryBind();
            newPage.imageMemoryBind.offset = offset;
            newPage.imageMemoryBind.extent = extent;
            pages.Add(newPage);
            return ref pages.Back();
            
        }

        public void updateSparseBindInfo()
        {
            // Update list of memory-backed sparse image memory binds
            sparseImageMemoryBinds.Clear();
            foreach (var page in pages)
            {
                sparseImageMemoryBinds.Add(page.imageMemoryBind);
            }

            // Image memory binds
            imageMemoryBindInfo = new[] { new SparseImageMemoryBindInfo(image, sparseImageMemoryBinds) };

            // Opaque image memory binds for the mip tail
            opaqueMemoryBindInfo = new[] { new SparseImageOpaqueMemoryBindInfo(image, opaqueMemoryBinds) };

            bindSparseInfo = new BindSparseInfo(null, null, opaqueMemoryBindInfo, imageMemoryBindInfo, null);
        }

        public void Dispose()
        {
            foreach (var page in pages)
            {
                page.Dispose();
            }

            foreach (var bind in opaqueMemoryBinds)
            {
                Device.FreeMemory(bind.memory);
            }
        }
    }

    [SampleDesc(sortOrder = 6)]
    public class VirtualTextureSample : Sample
    {
    }
}
