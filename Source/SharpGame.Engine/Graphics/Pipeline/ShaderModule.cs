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
        TessellationControl = 2,
        TessellationEvaluation = 4,
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

    public struct InputBlock
    {
        public string name;
        public int size;
        public List<BlockMember> members;
        public int set;
        public int binding;
        public bool isTextureBlock;
    }

    public class ShaderReflection
    {
        public InputBlock pushConstants;
        public List<InputBlock> descriptorSets;

        public List<uint> dynamicSets;
        public List<uint> globalSets;
        public List<uint> staticSets;

        public uint dynamicSetSize;
        public uint staticSetSize;
        public uint numDynamicUniforms;
        public uint numDynamicTextures;
        public uint numStaticUniforms;
        public uint numStaticTextures;
    }

    public class ShaderModule : Resource
    {
        [DataMember]
        public ShaderStage Stage { get; set; }
        [DataMember]
        public string FuncName { get; set; }
        [DataMember]
        public byte[] Code { get; set; }

        [DataMember]
        public ShaderReflection ShaderReflection { get; set; }

        internal VkShaderModule shaderModule;

        public ShaderModule()
        {
        }

        public ShaderModule(ShaderStage stage, string fileName, string funcName = "main")
        {
            Stage = stage;
            FileName = fileName;
            FuncName = funcName;
                        
            using (File stream = ResourceCache.Instance.Open(fileName))
            {
                Code = stream.ReadAllBytes();
            }

            Build();
        }
        
        public override bool Load(File stream)
        {
            Code = stream.ReadAllBytes();

            Build();

            return true;
        }

        protected override void OnBuild()
        {
            if (Code == null)
            {
                using (File stream = ResourceCache.Instance.Open(FileName))
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
