using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace SharpGame
{
    public struct BlockMember
    {
        public string name;
        public int size;
        public int offset;
    }

    public struct SpecializationConst
    {
        public string name;
        public uint id;
        public uint offset;
        public object value;
    }

    public struct UniformBlock
    {
        public string name;
        public uint size;
        public List<BlockMember> members;
        public int set;
        public uint binding;
        public VkDescriptorType descriptorType;
        public uint descriptorCount;
    }

    public class ShaderReflection
    {
        public List<BlockMember> pushConstants;
        public List<UniformBlock> descriptorSets;
        public List<SpecializationConst> specializationConsts;
    }

    public class ShaderModule : HandleBase<VkShaderModule>
    {
        [DataMember]
        public VkShaderStageFlags Stage { get; set; }

        [DataMember]
        public byte[] Code { get; set; }


        [DataMember]
        public ShaderReflection ShaderReflection { get; set; }

        public SpecializationInfo SpecializationInfo { get; set; }

        public ShaderModule()
        {
        }

        public ShaderModule(VkShaderStageFlags stage, byte[] code)
        {
            Stage = stage;
            Code = code;

            Build();
        }

        public ShaderModule(VkShaderStageFlags stage, IntPtr CodePointer, uint CodeLength)
        {
            Stage = stage;
            unsafe
            {
                var sm = new VkShaderModuleCreateInfo
                {
                    sType = VkStructureType.ShaderModuleCreateInfo,
                    pCode = (uint*)CodePointer,
                    codeSize = new UIntPtr(CodeLength),
                };

                handle = Device.CreateShaderModule(ref sm);
            }
        }

        public bool Build()
        {
            if (Code == null)
            {
                return false;
            }

            unsafe
            {
                ulong shaderSize = (ulong)Code.Length;
                fixed (byte* scPtr = Code)
                {
                    var sm = new VkShaderModuleCreateInfo
                    {
                        sType = VkStructureType.ShaderModuleCreateInfo,
                        pCode = (uint*)scPtr,
                        codeSize = new UIntPtr(shaderSize)
                    };
                    handle = Device.CreateShaderModule(ref sm);
                }

            }

            return handle != null;
        }

        protected override void Destroy(bool disposing)
        {
            Code = null;

            base.Destroy(disposing);
        }

    }
}
