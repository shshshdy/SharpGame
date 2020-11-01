using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;


namespace SharpGame
{
    using global::System.Runtime.Serialization;
    using System.Data.Common;
    using System.Runtime.CompilerServices;

    public class ResourceSet : IDisposable
    {
        public int Set => resourceLayout.Set;
        public bool Updated { get; private set; } = false;

        internal ResourceLayout resourceLayout;

        [IgnoreDataMember]
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;

        internal VkDescriptorSet descriptorSet;
        internal VkDescriptorPool descriptorPool;
   
        internal WriteDescriptorSet[] writeDescriptorSets;
        internal bool[] updated;

        NativeList<DescriptorImageInfo> descriptorImageInfos = new NativeList<DescriptorImageInfo>();
        NativeList<DescriptorBufferInfo> descriptorBufferInfos = new NativeList<DescriptorBufferInfo>();

        public ResourceSet(ResourceLayout resLayout)
        {
            resLayout.Build();

            descriptorPool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            resourceLayout = resLayout;

            unsafe
            {
                VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
                descriptorSetAllocateInfo.descriptorPool = descriptorPool;
                descriptorSetAllocateInfo.pSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref resLayout.DescriptorSetLayout);
                descriptorSetAllocateInfo.descriptorSetCount = 1;

                VulkanNative.vkAllocateDescriptorSets(Graphics.device, ref descriptorSetAllocateInfo, out descriptorSet);
            }

            writeDescriptorSets = new WriteDescriptorSet[resLayout.NumBindings];
            updated = new bool[resLayout.NumBindings];
        }

        public ResourceSet(ResourceLayout resLayout, params IBindableResource[] bindables)
        {
            resLayout.Build();

            descriptorPool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            resourceLayout = resLayout;

            unsafe
            {
                VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
                descriptorSetAllocateInfo.descriptorPool = descriptorPool;
                descriptorSetAllocateInfo.pSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref resLayout.DescriptorSetLayout);
                descriptorSetAllocateInfo.descriptorSetCount = 1;

                VulkanNative.vkAllocateDescriptorSets(Graphics.device, ref descriptorSetAllocateInfo, out descriptorSet);
            }

            System.Diagnostics.Debug.Assert(bindables.Length == resLayout.NumBindings);

            writeDescriptorSets = new WriteDescriptorSet[resLayout.NumBindings];
            updated = new bool[resLayout.NumBindings];

            for(uint i = 0; i < resLayout.NumBindings; i++)
            {
                Bind(i, bindables[i]);
            }


            UpdateSets();
        }

        public void Dispose()
        {
            VulkanNative.vkFreeDescriptorSets(Graphics.device, descriptorPool, 1, ref descriptorSet);
            Graphics.DescriptorPoolManager.Free(descriptorPool, ref resourceLayout.descriptorResourceCounts);
        }

        public void Bind(params IBindableResource[] bindables)
        {
            System.Diagnostics.Debug.Assert(bindables.Length == writeDescriptorSets.Length);

            writeDescriptorSets = new WriteDescriptorSet[writeDescriptorSets.Length];

            for (uint i = 0; i < writeDescriptorSets.Length; i++)
            {
                Bind(i, bindables[i]);
            }

            UpdateSets();
        }

        public ResourceSet Bind(uint dstBinding, ref DescriptorImageInfo imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                               descriptorType, ref imageInfo, 1);
            updated[dstBinding] = false;
            return this;
        }

        public ResourceSet Bind(uint dstBinding, Span<DescriptorImageInfo> imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                               descriptorType, ref imageInfo[0], (uint)imageInfo.Length);

            updated[dstBinding] = false;
            return this;
        }

        public ResourceSet Bind(uint dstBinding, ref DescriptorBufferInfo bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                               descriptorType, ref bufferInfo, 1);
            updated[dstBinding] = false;
            return this;
        }

        public ResourceSet Bind(uint dstBinding, Span<DescriptorBufferInfo> bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                               descriptorType, ref bufferInfo[0], (uint)bufferInfo.Length);
            updated[dstBinding] = false;
            return this;
        }

        public ResourceSet Bind(uint dstBinding, ref DescriptorBufferInfo bufferInfo, BufferView bufferView)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                               descriptorType, ref bufferInfo, bufferView);
            updated[dstBinding] = false;
            return this;
        }

        public ResourceSet Bind(uint dstBinding, IBindableResource bindable)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            switch(descriptorType)
            {
                case DescriptorType.Sampler:
                    break;
                case DescriptorType.InputAttachment:
                case DescriptorType.CombinedImageSampler:
                    {
                        if(bindable is Texture texture)
                        {
                            Bind(dstBinding, ref texture.descriptor);
                        }
                        else if(bindable is ImageView textureView)
                        {
                            Bind(dstBinding, ref textureView.Descriptor);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }

                    }
                    break;
                case DescriptorType.SampledImage:
                    break;
                case DescriptorType.StorageImage:
                    {
                        var texture = bindable as Texture;
                        Bind(dstBinding, ref texture.descriptor);
//                         writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
//                             descriptorType, ref texture.descriptor, 1);                         
                    }
                    break;

                case DescriptorType.UniformTexelBuffer:
                case DescriptorType.StorageTexelBuffer:
                    {
                        var buffer = bindable as Buffer;
                        Bind(dstBinding, ref buffer.descriptor, buffer.view);
//                         writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
//                             descriptorType, ref buffer.descriptor, buffer.view);
                    }
                    break;

                case DescriptorType.UniformBuffer:
                case DescriptorType.StorageBuffer:
                case DescriptorType.UniformBufferDynamic:
                case DescriptorType.StorageBufferDynamic:
                    {
                        var buffer = bindable as Buffer;
                        Bind(dstBinding, ref buffer.descriptor);
//                         writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
//                             descriptorType, ref buffer.descriptor, 1);
                    }
                    
                    break;
                default:
                    Debug.Assert(false);
                    break;
                   
            }
            return this;
        }

        public void UpdateSets()
        {
            uint index = 0;
            uint count = 0;
            for(uint i = 0; i < writeDescriptorSets.Length; i++)
            {
                if (!updated[i])
                {
                    count++;
                    updated[i] = true;
                }
                else{

                    if(count > 0)
                    {
                        Device.UpdateDescriptorSets(count, ref writeDescriptorSets[index].native, 0, IntPtr.Zero);
                    }
                    count = 0;
                    index = i + 1;
                }
            }

            if (count > 0)
            {
                Device.UpdateDescriptorSets(count, ref writeDescriptorSets[index].native, 0, IntPtr.Zero);
            }

            //Device.UpdateDescriptorSets((uint)writeDescriptorSets.Length, ref writeDescriptorSets[0].native, 0, IntPtr.Zero);
            Updated = true;
        }

    }

    public struct DescriptorImageInfo
    {
        internal VkDescriptorImageInfo native;
        public DescriptorImageInfo(Sampler sampler, ImageView imageView, ImageLayout imageLayout)
        {
            native = new VkDescriptorImageInfo
            {
                sampler = sampler? sampler.handle : VkSampler.Null,
                imageView = imageView.handle,
                imageLayout = (VkImageLayout)imageLayout,
            };
        }

        public static bool operator==(in DescriptorImageInfo left, in DescriptorImageInfo right)
        {
            return left.native.sampler == right.native.sampler
                && left.native.imageView == right.native.imageView
                && left.native.imageLayout == right.native.imageLayout;
        }

        public static bool operator !=(in DescriptorImageInfo left, in DescriptorImageInfo right)
        {
            return !(left == right);
        }
    }

    public struct DescriptorBufferInfo
    {
        internal VkBuffer buffer;
        public ulong offset;
        public ulong range;

        public DescriptorBufferInfo(Buffer buffer, ulong offset, ulong range)
        {
            this.buffer = buffer.buffer;
            this.offset = offset;
            this.range = range;
        }

        public static bool operator ==(in DescriptorBufferInfo left, in DescriptorBufferInfo right)
        {
            return left.buffer == right.buffer
                && left.offset == right.offset
                && left.range == right.range;
        }

        public static bool operator !=(in DescriptorBufferInfo left, in DescriptorBufferInfo right)
        {
            return !(left == right);
        }
    }

    public struct WriteDescriptorSet
    {
        internal VkWriteDescriptorSet native;
        public unsafe WriteDescriptorSet(uint binding,
            VkDescriptorSet dstSet,
            DescriptorType type,
            ref DescriptorBufferInfo bufferInfo,
            uint descriptorCount = 1)
        {
            native = VkWriteDescriptorSet.New();
            native.dstSet = dstSet;
            native.descriptorType = (VkDescriptorType)type;
            native.dstBinding = binding;
            native.pBufferInfo = (VkDescriptorBufferInfo*)Unsafe.AsPointer(ref bufferInfo);
            native.descriptorCount = descriptorCount;
        }

        public unsafe WriteDescriptorSet(
            uint binding,
            VkDescriptorSet dstSet,
            DescriptorType type,
            ref DescriptorImageInfo imageInfo,
            uint descriptorCount = 1)
        {
            native = VkWriteDescriptorSet.New();
            native.dstSet = dstSet;
            native.descriptorType = (VkDescriptorType)type;
            native.dstBinding = binding;
            native.pImageInfo = (VkDescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
            native.descriptorCount = descriptorCount;
        }

        public unsafe WriteDescriptorSet(uint binding,
            VkDescriptorSet dstSet,
            DescriptorType type,
            ref DescriptorBufferInfo bufferInfo,
            BufferView bufferView)
        {
            native = VkWriteDescriptorSet.New();
            native.dstSet = dstSet;
            native.descriptorType = (VkDescriptorType)type;
            native.dstBinding = binding;
            native.descriptorCount = 1;
            native.pBufferInfo = (VkDescriptorBufferInfo*)Unsafe.AsPointer(ref bufferInfo);            
            native.pTexelBufferView = (VkBufferView*)Unsafe.AsPointer(ref bufferView.view);
            
        }
    }

}
