using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Sampler : DisposeBase
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
        public VkBorderColor borderColor;
        public float maxLod;
        public float minLod;
        public VkCompareOp compareOp;
        public VkBool32 compareEnable;
        public float maxAnisotropy;
        public VkBool32 anisotropyEnable;
        public VkBool32 unnormalizedCoordinates;
        public float mipLodBias;
        public VkSamplerAddressMode addressModeV;
        public VkSamplerAddressMode addressModeU;
        public VkSamplerMipmapMode mipmapMode;
        public VkFilter minFilter;
        public VkFilter magFilter;
        public uint flags;
        public VkSamplerAddressMode addressModeW;

        public void ToNative(out VkSamplerCreateInfo native)
        {
            native = VkSamplerCreateInfo.New();
            native.maxLod = maxLod;
            native.minLod = minLod;
            native.compareOp = compareOp;
            native.compareEnable = compareEnable;
            native.maxAnisotropy = maxAnisotropy;
            native.anisotropyEnable = anisotropyEnable;
            native.unnormalizedCoordinates = unnormalizedCoordinates;
            native.mipLodBias = mipLodBias;
            native.addressModeV = addressModeV;
            native.addressModeU = addressModeU;
            native.mipmapMode = mipmapMode;
            native.minFilter = minFilter;
            native.magFilter = magFilter;
            native.flags = flags;
            native.addressModeW = addressModeW;
        }
    }

}
