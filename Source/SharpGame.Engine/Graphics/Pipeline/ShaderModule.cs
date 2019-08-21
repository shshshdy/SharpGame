using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace SharpGame
{
    [Flags]
    public enum ShaderStage
    {
        None = 0,
        Vertex = 1,
        TessControl = 2,
        TessEvaluation = 4,
        Geometry = 8,
        Fragment = 16,
        AllGraphics = 31,
        Compute = 32,
        All = int.MaxValue
    }

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
        public DescriptorType descriptorType;
        public uint descriptorCount;
    }

    public class ShaderReflection
    {
        public List<BlockMember> pushConstants;
        public List<UniformBlock> descriptorSets;
        public List<SpecializationConst> specializationConsts;
    }

    public class ShaderModule : Object
    {
        [DataMember]
        public ShaderStage Stage { get; set; }
        [DataMember]
        public string FuncName { get; set; }
        [DataMember]
        public byte[] Code { get; set; }

        [DataMember]
        public ShaderReflection ShaderReflection { get; set; }

        internal string FileName {get;set; }

        public SpecializationInfo SpecializationInfo { get; set; }

        internal VkShaderModule shaderModule;

        public ShaderModule()
        {
        }

        public ShaderModule(ShaderStage stage, string fileName, string funcName = "main")
        {
            Stage = stage;
            FileName = fileName;
            FuncName = funcName;
                        
            using (File stream = FileSystem.Instance.GetFile(fileName))
            {
                Code = stream.ReadAllBytes();
            }

            Build();
        }

        public ShaderModule(ShaderStage stage, byte[] code, string funcName = "main")
        {
            Stage = stage;
            Code = code;
            FuncName = funcName;        

            Build();
        }

        public bool Build()
        {
            if (Code == null)
            {
                using (File stream = FileSystem.Instance.GetFile(FileName))
                {
                    Code = stream.ReadAllBytes();
                }
            }

            unsafe
            {
                var sm = VkShaderModuleCreateInfo.New();
                ulong shaderSize = (ulong)Code.Length;
                fixed (byte* scPtr = Code)
                {
                    sm.pCode = (uint*)scPtr;
                    sm.codeSize = new UIntPtr(shaderSize);
                }

                shaderModule = Device.CreateShaderModule(ref sm);
            }

            return shaderModule != null;
        }

        protected override void Destroy()
        {
            if(shaderModule != 0)
            {
                Device.Destroy(shaderModule);
                shaderModule = 0;
            }

            Code = null;

            base.Destroy();
        }

    }
}
