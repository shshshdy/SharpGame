using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;


namespace SharpGame
{
    using global::System.Runtime.Serialization;
    using System.Data.Common;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;

    public class ResourceSet : IDisposable
    {
        public int Set => resourceLayout.Set;
        public bool Updated { get; private set; } = false;

        internal ResourceLayout resourceLayout;

        [IgnoreDataMember]
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;

        internal VkDescriptorPool descriptorPool;
        internal FixedArray3<VkDescriptorSet> descriptorSet;
   
        internal WriteDescriptorSet[][] writeDescriptorSets = new WriteDescriptorSet[Swapchain.IMAGE_COUNT][];
        internal bool[][] updated = new bool[Swapchain.IMAGE_COUNT][];

        public ResourceSet(ResourceLayout resLayout)
        {
            resLayout.Build();

            descriptorPool = Graphics.DescriptorPoolManager.GetPool(ref resLayout.descriptorResourceCounts, Swapchain.IMAGE_COUNT);
            resourceLayout = resLayout;

            unsafe
            {
                var setLayouts = stackalloc VkDescriptorSetLayout[3] { resLayout.DescriptorSetLayout, resLayout.DescriptorSetLayout, resLayout.DescriptorSetLayout };

                var descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
                descriptorSetAllocateInfo.descriptorPool = descriptorPool;
                descriptorSetAllocateInfo.pSetLayouts = setLayouts;
                descriptorSetAllocateInfo.descriptorSetCount = Swapchain.IMAGE_COUNT;

                Device.AllocateDescriptorSets(ref descriptorSetAllocateInfo, (VkDescriptorSet*)descriptorSet.Data);
            }

            for (int i = 0; i < Swapchain.IMAGE_COUNT; i++)
            {
                writeDescriptorSets[i] = new WriteDescriptorSet[resLayout.NumBindings];
                updated[i] = new bool[resLayout.NumBindings];
            }
        }

        public ResourceSet(ResourceLayout resLayout, params IBindableResource[] bindables)
        {
            resLayout.Build();

            descriptorPool = Graphics.DescriptorPoolManager.GetPool(ref resLayout.descriptorResourceCounts, Swapchain.IMAGE_COUNT);
            resourceLayout = resLayout;

            unsafe
            {
                var setLayouts = stackalloc VkDescriptorSetLayout[3] { resLayout.DescriptorSetLayout, resLayout.DescriptorSetLayout, resLayout.DescriptorSetLayout };
                var descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
                descriptorSetAllocateInfo.descriptorPool = descriptorPool;
                descriptorSetAllocateInfo.pSetLayouts = setLayouts;
                descriptorSetAllocateInfo.descriptorSetCount = Swapchain.IMAGE_COUNT;

                Device.AllocateDescriptorSets(ref descriptorSetAllocateInfo, (VkDescriptorSet*)descriptorSet.Data);
            }

            System.Diagnostics.Debug.Assert(bindables.Length == resLayout.NumBindings);

            for (int i = 0; i < Swapchain.IMAGE_COUNT; i++)
            {
                writeDescriptorSets[i] = new WriteDescriptorSet[resLayout.NumBindings];
                updated[i] = new bool[resLayout.NumBindings];
            }

            for(uint i = 0; i < resLayout.NumBindings; i++)
            {
                Bind(i, bindables[i]);
            }


            UpdateSets();
        }

        public void Dispose()
        {
            Device.FreeDescriptorSets(descriptorPool, 3, ref Utilities.As<VkDescriptorSet>(descriptorSet.Data));

            Graphics.DescriptorPoolManager.Free(descriptorPool, ref resourceLayout.descriptorResourceCounts);
        }

        public void Bind(params IBindableResource[] bindables)
        {
            System.Diagnostics.Debug.Assert(bindables.Length == writeDescriptorSets[0].Length);

            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img] = new WriteDescriptorSet[writeDescriptorSets[img].Length];
                for (uint i = 0; i < writeDescriptorSets[img].Length; i++)
                {
                    Bind(i, bindables[i]);
                }
            }

            UpdateSets();
        }

        public ResourceSet Bind(uint dstBinding, ref DescriptorImageInfo imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet[img],
                               descriptorType, ref imageInfo, 1);
                updated[img][dstBinding] = false;
            }
            return this;
        }

        public ResourceSet Bind(uint dstBinding, Span<DescriptorImageInfo> imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet[img],
                               descriptorType, ref imageInfo[0], (uint)imageInfo.Length); 
                updated[img][dstBinding] = false;
            }

            return this;
        }

        public ResourceSet Bind(uint dstBinding, SharedBuffer buffer)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet[img],
                               descriptorType, ref buffer[img].descriptor, 1);
                updated[img][dstBinding] = false;
            }

            return this;
        }

        public ResourceSet Bind(uint dstBinding, ref VkDescriptorBufferInfo bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet[img],
                               descriptorType, ref bufferInfo, 1);
                updated[img][dstBinding] = false;
            }

            return this;
        }

        public ResourceSet Bind(uint dstBinding, Span<VkDescriptorBufferInfo> bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet[img],
                               descriptorType, ref bufferInfo[0], (uint)bufferInfo.Length);
                updated[img][dstBinding] = false;
            }

            return this;
        }

        public ResourceSet Bind(uint dstBinding, ref VkDescriptorBufferInfo bufferInfo, BufferView bufferView)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;

            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet[img],
                               descriptorType, ref bufferInfo, bufferView);
                updated[img][dstBinding] = false;
            }

            return this;
        }

        public ResourceSet BindTexel(uint dstBinding, SharedBuffer buffer)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;

            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet[img],
                               descriptorType, ref buffer.Buffer.descriptor, buffer.Buffer.view);
                updated[img][dstBinding] = false;
            }

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
                    }
                    break;

                case DescriptorType.UniformTexelBuffer:
                case DescriptorType.StorageTexelBuffer:
                    {
                        if (bindable is Buffer buffer)
                            Bind(dstBinding, ref buffer.descriptor, buffer.view);
                        else if (bindable is SharedBuffer sharedBuffer)
                            BindTexel(dstBinding, sharedBuffer);
                    }
                    break;

                case DescriptorType.UniformBuffer:
                case DescriptorType.StorageBuffer:
                case DescriptorType.UniformBufferDynamic:
                case DescriptorType.StorageBufferDynamic:
                    {
                        if (bindable is Buffer buffer)
                        {
                            Bind(dstBinding, ref buffer.descriptor);
                        }
                        else if (bindable is SharedBuffer sharedBuffer)
                        {
                            Bind(dstBinding, sharedBuffer);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
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
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                UpdateSets(img);
            }
        }
        
        public void UpdateSets(int img)
        {
            uint index = 0;
            uint count = 0;
               
            for (uint i = 0; i < writeDescriptorSets[img].Length; i++)
            {
                if (!updated[img][i])
                {
                    count++;
                    updated[img][i] = true;
                }
                else{

                    if(count > 0)
                    {
                        Device.UpdateDescriptorSets(count, ref writeDescriptorSets[img][index].native, 0, IntPtr.Zero);
                    }
                    count = 0;
                    index = i + 1;
                }
            }

            if (count > 0)
            {
                Device.UpdateDescriptorSets(count, ref writeDescriptorSets[img][index].native, 0, IntPtr.Zero);
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
    }

    public struct WriteDescriptorSet
    {
        internal VkWriteDescriptorSet native;
        public unsafe WriteDescriptorSet(uint binding,
            VkDescriptorSet dstSet,
            DescriptorType type,
            ref VkDescriptorBufferInfo bufferInfo,
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
            ref VkDescriptorBufferInfo bufferInfo,
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
