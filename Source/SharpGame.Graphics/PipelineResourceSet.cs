using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    public struct ConstBlock
    {
        public PushConstantRange range;
        public IntPtr data;

        public ConstBlock(VkShaderStageFlags shaderStage, int offset, int size, IntPtr data)
        {
            range = new PushConstantRange(shaderStage, offset, size);
            this.data = data;
        }
    }

    public unsafe class InlineUniformBlock : IBindableResource, IDisposable
    {
        public ShaderResourceInfo resourceInfo;
        public IntPtr data;
        public uint size;

        public string Name => resourceInfo.name;

        public VkWriteDescriptorSetInlineUniformBlockEXT* inlineUniformBlockEXT;
        public DescriptorSet descriptorSet;

        public unsafe InlineUniformBlock(ShaderResourceInfo resourceInfo)
        {
            this.resourceInfo = resourceInfo;
            this.size = resourceInfo.size;
            this.data = Utilities.Alloc((int)this.size);

            inlineUniformBlockEXT = (VkWriteDescriptorSetInlineUniformBlockEXT*)Utilities.Alloc<VkWriteDescriptorSetInlineUniformBlockEXT>();
            inlineUniformBlockEXT->sType = VkStructureType.WriteDescriptorSetInlineUniformBlockEXT;
            inlineUniformBlockEXT->pNext = null;
            inlineUniformBlockEXT->pData = (void*)this.data;
            inlineUniformBlockEXT->dataSize = this.size;            
        }

        public void MarkDirty()
        {
            //descriptorSet.MarkDirty(resourceInfo.binding);
            descriptorSet.Bind(resourceInfo.binding, this);
        }

        public void Dispose()
        {
            Utilities.Free(data);
            Utilities.Free((IntPtr)inlineUniformBlockEXT);
        }
    }

    public class PipelineResourceSet : DisposeBase
    {
        [IgnoreDataMember]
        public DescriptorSet[] ResourceSet { get; private set; }

        public IntPtr pushConstBuffer;
        public int minPushConstRange = 1000;
        public int maxPushConstRange = 0;

        List<InlineUniformBlock> inlineUniformBlocks = null;

        PipelineLayout pipelineLayout;
        public int FirstSet { get; } = 0;
        public PipelineResourceSet(int firstSet)
        {
            FirstSet = firstSet;
            pushConstBuffer = Utilities.Alloc(Device.MaxPushConstantsSize);
        }

        public PipelineResourceSet(PipelineLayout pipelineLayout)
        {
            Init(pipelineLayout);
        }

        public void Init(PipelineLayout pipelineLayout)
        {
            this.pipelineLayout = pipelineLayout;

            int count = pipelineLayout.ResourceLayout.Length - FirstSet;
            ResourceSet = new DescriptorSet[count];
            if (count <= 0)
            {
                return;
            }

            for(int i = 0; i < count; i++)
            {                  
                ResourceSet[i] = new DescriptorSet(pipelineLayout.ResourceLayout[i + FirstSet]);
            
                var resLayout = pipelineLayout.ResourceLayout[i + FirstSet];
                foreach (var binding in resLayout.Bindings)
                {
                    if (binding.IsInlineUniformBlock && binding.resourceInfo != null)
                    {
                        if(inlineUniformBlocks == null)
                        {
                            inlineUniformBlocks = new List<InlineUniformBlock>();
                        }

                        var inlineUniformBlock = new InlineUniformBlock(binding.resourceInfo);
                        inlineUniformBlock.descriptorSet = ResourceSet[i];
                        ResourceSet[i].Bind(binding.binding, inlineUniformBlock);
                        inlineUniformBlocks.Add(inlineUniformBlock);
                    }

                }
            }

            if (pipelineLayout.PushConstantNames != null)
            {
                for (int i = 0; i < pipelineLayout.PushConstantNames.Count; i++)
                {
                    var constName = pipelineLayout.PushConstantNames[i];
                    if(constName.StartsWith("g_"))
                    {
                        continue;
                    }

                    var pushConst = pipelineLayout.PushConstant[i];
                    if (pushConst.offset + pushConst.size > Device.MaxPushConstantsSize)
                    {
                        Log.Error("PushConst out of range" + constName);
                        continue;
                    }

                    if (pushConst.offset < minPushConstRange)
                    {
                        minPushConstRange = pushConst.offset;
                    }

                    if (pushConst.offset + pushConst.size > maxPushConstRange)
                    {
                        maxPushConstRange = pushConst.offset + pushConst.size;
                    }

                }

            }

        }

        public InlineUniformBlock GetInlineUniformBlock(string name)
        {
            if (inlineUniformBlocks != null)
            {
                foreach (var block in inlineUniformBlocks)
                {
                    if (block.Name == name)
                    {
                        return block;
                    }
                }
            }

            return null;
        }

        public IntPtr GetInlineUniformMember(string name, out InlineUniformBlock inlineUniformBlock)
        {
            if (inlineUniformBlocks != null)
            {
                foreach (var block in inlineUniformBlocks)
                {
                    foreach (var member in block.resourceInfo.members)
                    {
                        if (member.name == name)
                        {
                            inlineUniformBlock = block;
                            return block.data + member.offset;
                        }
                    }
                }
            }
            inlineUniformBlock = null;
            return IntPtr.Zero;
        }

        public IntPtr GetPushConst(string name)
        {
            if (pipelineLayout.PushConstantNames != null)
            {
                for (int i = 0; i < pipelineLayout.PushConstantNames.Count; i++)
                {
                    var constName = pipelineLayout.PushConstantNames[i];
                    if (constName == name)
                    {
                        var pushConstantRange = pipelineLayout.PushConstant[i];
                        return pushConstBuffer + pushConstantRange.offset;
                    }
                }
            }

            return IntPtr.Zero;
        }

        public void SetResourceSet(uint set, params IBindableResource[] res)
        {
            if (set < 0 || set >= ResourceSet.Length)
            {
                return;
            }

            ResourceSet[set].Bind(res);
        }

        public void SetResource(uint set, uint binding, IBindableResource res)
        {
            if (set < 0 || set >= ResourceSet.Length)
            {
                return;
            }

            ResourceSet[set].BindResource(binding, res);
        }

        public void UpdateResourceSet(StringID name, IBindableResource tex)
        {
            foreach (var rs in ResourceSet)
            {
                foreach (var binding in rs.resourceLayout.Bindings)
                {
                    if (binding.name == name)
                    {
                        rs.BindResource(binding.binding, tex);
                        rs.UpdateSets();
                        return;
                    }
                }
            }
        }

        public void UpdateAllSets()
        {
            foreach (var rs in ResourceSet)
            {                      
                rs.UpdateSets();                    
            }
        }

        public void BindGraphicsResourceSet(CommandBuffer cmd)
        {
            foreach (var rs in ResourceSet)
            {
                rs.UpdateSets();
                if (rs.Updated)
                {
                    cmd.BindGraphicsResourceSet(pipelineLayout, rs.Set, rs);
                }
            }
        }

        public void PushConstants(CommandBuffer cmd)
        {
            int size = maxPushConstRange - minPushConstRange;
            if (size > 0)
            {
                VkShaderStageFlags shaderStage = VkShaderStageFlags.None;
                int minRange = minPushConstRange;
                int currentSize = 0;
                for (int i = 0; i < pipelineLayout.PushConstant.Length; i++)
                {
                    if (i == 0)
                    {
                        shaderStage = pipelineLayout.PushConstant[0].stageFlags;
                    }

                    currentSize += pipelineLayout.PushConstant[i].size;

                    if ((pipelineLayout.PushConstant[i].stageFlags != shaderStage) || (i == pipelineLayout.PushConstant.Length - 1))
                    {
                        cmd.PushConstants(pipelineLayout, shaderStage, minRange, currentSize, pushConstBuffer + minRange);

                        shaderStage = pipelineLayout.PushConstant[i].stageFlags;
                        minRange += currentSize;
                        currentSize = 0;
                    }
                }

            }
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);

            Utilities.Free(pushConstBuffer);
        }
    }
}
