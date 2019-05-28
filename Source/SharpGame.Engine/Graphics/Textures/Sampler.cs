using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Sampler : DisposeBase, IBindableResource
    {
        internal VkSampler handle;
        public Sampler(ref SamplerCreateInfo samplerCreateInfo)
        {
            samplerCreateInfo.ToNative(out VkSamplerCreateInfo vkSamplerCreate);
            handle = Device.CreateSampler(ref vkSamplerCreate);
        }

        protected override void Destroy()
        {
            Device.Destroy(handle);

            base.Destroy();
        }
    }


    public struct SamplerCreateInfo
    {
        public BorderColor borderColor;
        public float maxLod;
        public float minLod;
        public CompareOp compareOp;
        public bool compareEnable;
        public float maxAnisotropy;
        public bool anisotropyEnable;
        public bool unnormalizedCoordinates;
        public float mipLodBias;
        public SamplerAddressMode addressModeV;
        public SamplerAddressMode addressModeU;
        public SamplerMipmapMode mipmapMode;
        public Filter minFilter;
        public Filter magFilter;
        public uint flags;
        public SamplerAddressMode addressModeW;

        public void ToNative(out VkSamplerCreateInfo native)
        {
            native = VkSamplerCreateInfo.New();
            native.maxLod = maxLod;
            native.minLod = minLod;
            native.compareOp = (VkCompareOp)compareOp;
            native.compareEnable = compareEnable;
            native.maxAnisotropy = maxAnisotropy;
            native.anisotropyEnable = anisotropyEnable;
            native.unnormalizedCoordinates = unnormalizedCoordinates;
            native.mipLodBias = mipLodBias;
            native.addressModeV = (VkSamplerAddressMode)addressModeV;
            native.addressModeU = (VkSamplerAddressMode)addressModeU;
            native.mipmapMode = (VkSamplerMipmapMode)mipmapMode;
            native.minFilter = (VkFilter)minFilter;
            native.magFilter = (VkFilter)magFilter;
            native.flags = flags;
            native.addressModeW = (VkSamplerAddressMode)addressModeW;
        }
    }

}
