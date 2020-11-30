using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public class Sampler : HandleBase<VkSampler>, IBindableResource
    {
        public static Sampler Default;
        public static Sampler ClampToEdge;

        static Sampler()
        {
            Default = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.Repeat, true);
            ClampToEdge = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.ClampToEdge, false);
        }

        public Sampler(VkFilter filter, VkSamplerMipmapMode mipmapMode,
            VkSamplerAddressMode addressMode, bool anisotropyEnable, VkBorderColor borderColor = VkBorderColor.FloatOpaqueWhite)
        {
            // Create sampler
            var samplerCreateInfo = new VkSamplerCreateInfo
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
            handle = Device.CreateSampler(ref samplerCreateInfo);
        }

        public Sampler(ref VkSamplerCreateInfo samplerCreateInfo)
        {
            handle = Device.CreateSampler(ref samplerCreateInfo);
        }


    }


}
