#define NEW_UPDATE

namespace SharpGame
{
    using System;
    using System.Runtime.InteropServices;
    using global::System.Runtime.Serialization;
    using System.Data.Common;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct DescriptorInfo
    {
        [FieldOffset(0)]
        public VkDescriptorImageInfo imageInfo;
        [FieldOffset(0)]
        public VkDescriptorBufferInfo bufferInfo;
        [FieldOffset(24)]
        public VkBufferView bufferView;
    }
    
    public class DescriptorSet : IDisposable
    {
        internal DescriptorSetLayout resourceLayout;
        internal VkDescriptorPool descriptorPool;
        internal FixedArray3<VkDescriptorSet> descriptorSet;
        internal VkWriteDescriptorSet[][] writeDescriptorSets = new VkWriteDescriptorSet[Swapchain.IMAGE_COUNT][];
        internal uint[] needUpdated = new uint[Swapchain.IMAGE_COUNT];
        public IBindableResource[] bindedRes;

        [IgnoreDataMember]
        public int Set => resourceLayout.Set;
        [IgnoreDataMember]
        public bool Updated { get; private set; } = false;
        [IgnoreDataMember]
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;

        public DescriptorSet(DescriptorSetLayout resLayout)
        {
            resLayout.Build();

            descriptorPool = Graphics.DescriptorPoolManager.GetPool(ref resLayout.descriptorResourceCounts, Swapchain.IMAGE_COUNT);
            resourceLayout = resLayout;

            unsafe
            {
                var setLayouts = stackalloc VkDescriptorSetLayout[3] { resLayout.Handle, resLayout.Handle, resLayout.Handle };

                var descriptorSetAllocateInfo = new VkDescriptorSetAllocateInfo
                {
                    sType = VkStructureType.DescriptorSetAllocateInfo
                };

                descriptorSetAllocateInfo.descriptorPool = descriptorPool;
                descriptorSetAllocateInfo.pSetLayouts = setLayouts;
                descriptorSetAllocateInfo.descriptorSetCount = Swapchain.IMAGE_COUNT;

                Device.AllocateDescriptorSets(ref descriptorSetAllocateInfo, (VkDescriptorSet*)descriptorSet.Data);
            }

            for (int i = 0; i < Swapchain.IMAGE_COUNT; i++)
            {
                writeDescriptorSets[i] = new VkWriteDescriptorSet[resLayout.NumBindings];
            }
        }

        public DescriptorSet(DescriptorSetLayout resLayout, params IBindableResource[] bindables)
        {
            resLayout.Build();

            descriptorPool = Graphics.DescriptorPoolManager.GetPool(ref resLayout.descriptorResourceCounts, Swapchain.IMAGE_COUNT);
            resourceLayout = resLayout;

            unsafe
            {
                var setLayouts = stackalloc VkDescriptorSetLayout[3] { resLayout.Handle, resLayout.Handle, resLayout.Handle };
                var descriptorSetAllocateInfo = new VkDescriptorSetAllocateInfo
                {
                    sType = VkStructureType.DescriptorSetAllocateInfo
                };
                descriptorSetAllocateInfo.descriptorPool = descriptorPool;
                descriptorSetAllocateInfo.pSetLayouts = setLayouts;
                descriptorSetAllocateInfo.descriptorSetCount = Swapchain.IMAGE_COUNT;

                Device.AllocateDescriptorSets(ref descriptorSetAllocateInfo, (VkDescriptorSet*)descriptorSet.Data);
            }

            System.Diagnostics.Debug.Assert(bindables.Length == resLayout.NumBindings);

            for (int i = 0; i < Swapchain.IMAGE_COUNT; i++)
            {
                writeDescriptorSets[i] = new VkWriteDescriptorSet[resLayout.NumBindings];
            }

            for(uint i = 0; i < resLayout.NumBindings; i++)
            {
                BindResource(i, bindables[i]);
            }

            UpdateSets();
        }

        public void Dispose()
        {
            Device.FreeDescriptorSets(descriptorPool, 3, ref Utilities.As<VkDescriptorSet>(descriptorSet.Data));

            Graphics.DescriptorPoolManager.Free(descriptorPool, ref resourceLayout.descriptorResourceCounts, Swapchain.IMAGE_COUNT);
        }

        public void Bind(params IBindableResource[] bindables)
        {
            System.Diagnostics.Debug.Assert(bindables.Length == writeDescriptorSets[0].Length);

            for (uint i = 0; i < bindables.Length; i++)
            {
                BindResource(i, bindables[i]);
            }

            UpdateSets();
        }
                  
        public DescriptorSet Bind(uint dstBinding, ref VkDescriptorImageInfo imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref imageInfo, 1));
            }
            return this;
        }

        public DescriptorSet Bind(uint dstBinding, Span<VkDescriptorImageInfo> imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref imageInfo[0], (uint)imageInfo.Length));
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, SharedBuffer buffer)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref buffer[img].descriptor, 1));
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, RenderTexture rt)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref rt.attachmentViews[img].descriptor, 1));
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, ref VkDescriptorBufferInfo bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {     
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref bufferInfo, 1));
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, Span<VkDescriptorBufferInfo> bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            { 
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref bufferInfo[0], (uint)bufferInfo.Length));
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, ref VkDescriptorBufferInfo bufferInfo, BufferView bufferView)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;

            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref bufferInfo, ref bufferView.HandleRef));
            }

            return this;
        }

        public DescriptorSet BindTexel(uint dstBinding, SharedBuffer buffer)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;

            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref buffer.Buffer.descriptor, ref buffer.Buffer.view.HandleRef));
            }

            return this;
        }

        void AddWriteDescriptorSet(int img, in VkWriteDescriptorSet writeDescriptorSet)
        {
            for(uint i = 0; i < needUpdated[img]; i++)
            {
                if(writeDescriptorSets[img][i].dstBinding == writeDescriptorSet.dstBinding)
                {
                    writeDescriptorSets[img][i] = writeDescriptorSet;
                    return;
                }
            }

            writeDescriptorSets[img][needUpdated[img]++] = writeDescriptorSet;
        }

        public unsafe DescriptorSet Bind(uint dstBinding, InlineUniformBlock inlineUniformBlock)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                AddWriteDescriptorSet(img, new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType,
                    inlineUniformBlock.inlineUniformBlockEXT));
            }

            return this;
        } 

        public DescriptorSet BindResource(uint dstBinding, IBindableResource bindable)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            switch(descriptorType)
            {
                case VkDescriptorType.Sampler:
                    break;
                case VkDescriptorType.InputAttachment:
                case VkDescriptorType.CombinedImageSampler:
                    {
                        if (bindable is RenderTexture rt)
                        {
                            Bind(dstBinding, rt);
                        }
                        else if (bindable is Texture texture)
                        {
                            Bind(dstBinding, ref texture.descriptor);
                        }
                        else if(bindable is ImageView textureView)
                        {
                            Bind(dstBinding, ref textureView.descriptor);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }

                    }
                    break;
                case VkDescriptorType.SampledImage:
                    break;
                case VkDescriptorType.StorageImage:
                    {
                        var texture = bindable as Texture;
                        Bind(dstBinding, ref texture.descriptor);                       
                    }
                    break;

                case VkDescriptorType.UniformTexelBuffer:
                case VkDescriptorType.StorageTexelBuffer:
                    {
                        if (bindable is Buffer buffer)
                            Bind(dstBinding, ref buffer.descriptor, buffer.view);
                        else if (bindable is SharedBuffer sharedBuffer)
                            BindTexel(dstBinding, sharedBuffer);
                    }
                    break;

                case VkDescriptorType.UniformBuffer:
                case VkDescriptorType.StorageBuffer:
                case VkDescriptorType.UniformBufferDynamic:
                case VkDescriptorType.StorageBufferDynamic:
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
                case VkDescriptorType.InlineUniformBlockEXT:

                    if (bindable is InlineUniformBlock iub)
                    {
                        Bind(dstBinding, iub);
                    }
                    else
                    {
                        Debug.Assert(false);
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
            if(needUpdated[img] > 0)
            {
                Device.UpdateDescriptorSets(needUpdated[img], ref writeDescriptorSets[img][0], 0, IntPtr.Zero);
                needUpdated[img] = 0;
                Updated = true;
            }
        }


    }


}
