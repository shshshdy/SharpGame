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

        public ConstBlock(ShaderStage shaderStage, int offset, int size, IntPtr data)
        {
            range = new PushConstantRange(shaderStage, offset, size);
            this.data = data;
        }
    }

    public class PipelineResourceSet : DisposeBase
    {
        [IgnoreDataMember]
        public List<ResourceSet> ResourceSet { get; set; } = new List<ResourceSet>();

        public IntPtr pushConstBuffer;
        public int minPushConstRange = 1000;
        public int maxPushConstRange = 0;

        PipelineLayout pipelineLayout;

        public PipelineResourceSet()
        {
            pushConstBuffer = Utilities.Alloc(Device.MaxPushConstantsSize);
        }

        public void Init(PipelineLayout pipelineLayout)
        {
            this.pipelineLayout = pipelineLayout;

            foreach (var layout in pipelineLayout.ResourceLayout)
            {
                if (layout.DefaultResourcSet == DefaultResourcSet.None)
                    ResourceSet.Add(new ResourceSet(layout));
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

        public void SetResourceSet(int index, params IBindableResource[] res)
        {
            if (index < 0 || index >= ResourceSet.Count)
            {
                return;
            }

            ResourceSet[index].Bind(res);
        }

        public void UpdateResourceSet(StringID name, IBindableResource tex)
        {
            foreach (var rs in ResourceSet)
            {
                foreach (var binding in rs.resourceLayout.Bindings)
                {
                    if (binding.name == name)
                    {
                        rs.Bind(binding.binding, tex);
                        rs.UpdateSets();
                        return;
                    }
                }
            }
        }

        public void BindGraphicsResourceSet(CommandBuffer cmd)
        {
            foreach (var rs in ResourceSet)
            {
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
                ShaderStage shaderStage = ShaderStage.None;
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
