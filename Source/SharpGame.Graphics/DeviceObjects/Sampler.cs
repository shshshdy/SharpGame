using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public class Sampler : DisposeBase, IBindableResource
    {
        public VkSampler handle;

        public Sampler(ref VkSamplerCreateInfo samplerCreateInfo)
        {
            handle = Device.CreateSampler(ref samplerCreateInfo);
        }

        protected override void Destroy(bool disposing)
        {
            Device.Destroy(handle);

            base.Destroy(disposing);
        }

        public static Sampler Default;
        public static Sampler ClampToEdge;

        public static void Init()
        {
            Default = Create(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.Repeat, true);
            ClampToEdge = Create(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.ClampToEdge, false);
        }

        public static Sampler Create(VkFilter filter, VkSamplerMipmapMode mipmapMode,
            VkSamplerAddressMode addressMode, bool anisotropyEnable, VkBorderColor borderColor = VkBorderColor.FloatOpaqueWhite)
        {
            // Create sampler
            VkSamplerCreateInfo sampler = new VkSamplerCreateInfo
            {
                sType = VkStructureType.SamplerCreateInfo,
                magFilter = filter,
                minFilter = filter,
                mipmapMode = mipmapMode,
                addressModeU = addressMode,
                addressModeV = addressMode,
                addressModeW = addressMode,
                mipLodBias = 0.0f,
                compareOp = VkCompareOp.Never,
                minLod = 0.0f,
                maxLod = 1.0f,// float.MaxValue,
                borderColor = borderColor,
                maxAnisotropy = anisotropyEnable ? Device.Properties.limits.maxSamplerAnisotropy : 1,
                anisotropyEnable = anisotropyEnable
            };
            return new Sampler(ref sampler);
        }
    }


}
