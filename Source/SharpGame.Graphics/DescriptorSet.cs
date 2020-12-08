using System;
using System.Collections.Generic;
using System.Text;



namespace SharpGame
{
    using global::System.Runtime.Serialization;
    using System.Data.Common;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;

    public class DescriptorSet : IDisposable
    {
        public int Set => resourceLayout.Set;
        public bool Updated { get; private set; } = false;

        internal DescriptorSetLayout resourceLayout;

        [IgnoreDataMember]
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;

        internal VkDescriptorPool descriptorPool;
        internal FixedArray3<VkDescriptorSet> descriptorSet;
   
        internal VkWriteDescriptorSet[][] writeDescriptorSets = new VkWriteDescriptorSet[Swapchain.IMAGE_COUNT][];
        internal bool[][] needUpdated = new bool[Swapchain.IMAGE_COUNT][];

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
                needUpdated[i] = new bool[resLayout.NumBindings];
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
                needUpdated[i] = new bool[resLayout.NumBindings];
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

            Graphics.DescriptorPoolManager.Free(descriptorPool, ref resourceLayout.descriptorResourceCounts, Swapchain.IMAGE_COUNT);
        }

        public void Bind(params IBindableResource[] bindables)
        {
            System.Diagnostics.Debug.Assert(bindables.Length == writeDescriptorSets[0].Length);

            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img] = new VkWriteDescriptorSet[writeDescriptorSets[img].Length];
                for (uint i = 0; i < writeDescriptorSets[img].Length; i++)
                {
                    Bind(i, bindables[i]);
                }
            }

            UpdateSets();
        }

        public DescriptorSet Bind(uint dstBinding, ref VkDescriptorImageInfo imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref imageInfo, 1);
                needUpdated[img][dstBinding] = true;
            }
            return this;
        }

        public DescriptorSet Bind(uint dstBinding, Span<VkDescriptorImageInfo> imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref imageInfo[0], (uint)imageInfo.Length); 
                needUpdated[img][dstBinding] = true;
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, SharedBuffer buffer)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref buffer[img].descriptor, 1);
                needUpdated[img][dstBinding] = true;
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, RenderTexture rt)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref rt.attachmentViews[img].descriptor, 1);
                needUpdated[img][dstBinding] = true;
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, ref VkDescriptorBufferInfo bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref bufferInfo, 1);
                needUpdated[img][dstBinding] = true;
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, Span<VkDescriptorBufferInfo> bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref bufferInfo[0], (uint)bufferInfo.Length);
                needUpdated[img][dstBinding] = true;
            }

            return this;
        }

        public DescriptorSet Bind(uint dstBinding, ref VkDescriptorBufferInfo bufferInfo, BufferView bufferView)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;

            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref bufferInfo, ref bufferView.HandleRef);
                needUpdated[img][dstBinding] = true;
            }

            return this;
        }

        public DescriptorSet BindTexel(uint dstBinding, SharedBuffer buffer)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;

            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType, ref buffer.Buffer.descriptor, ref buffer.Buffer.view.HandleRef);
                needUpdated[img][dstBinding] = true;
            }

            return this;
        }

        public unsafe DescriptorSet Bind(uint dstBinding, InlineUniformBlock inlineUniformBlock)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                writeDescriptorSets[img][dstBinding] = new VkWriteDescriptorSet(dstBinding, descriptorSet[img], descriptorType,
                    inlineUniformBlock.inlineUniformBlockEXT);
                needUpdated[img][dstBinding] = true;
            }

            return this;
        } 

        public DescriptorSet Bind(uint dstBinding, IBindableResource bindable)
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

        public void MarkDirty(uint binding)
        {
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                needUpdated[img][binding] = true;
            }
        }

        public void UpdateSets()
        {
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                UpdateSets(img);
            }
        }

        public void UpdateSets(uint binding)
        {
            for (int img = 0; img < Swapchain.IMAGE_COUNT; img++)
            {
                Device.UpdateDescriptorSets(1, ref writeDescriptorSets[img][binding], 0, IntPtr.Zero);
            }
        }

        public void UpdateSets(int img)
        {
            uint index = 0;
            uint count = 0;
               
            for (uint i = 0; i < writeDescriptorSets[img].Length; i++)
            {
                if (needUpdated[img][i])
                {
                    count++;
                    needUpdated[img][i] = false;
                }
                else{

                    if(count > 0)
                    {
                        Device.UpdateDescriptorSets(count, ref writeDescriptorSets[img][index], 0, IntPtr.Zero);
                        Updated = true;
                    }
                    count = 0;
                    index = i + 1;
                }
            }

            if (count > 0)
            {
                Device.UpdateDescriptorSets(count, ref writeDescriptorSets[img][index], 0, IntPtr.Zero);
                Updated = true;
            }

        }

    }


}
