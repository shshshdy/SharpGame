using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public struct BlockMember
    {
        public string name;
        public uint size;
        public uint offset;
    }

    public struct InputBlock
    {
        public string name;
        public uint size;
        public List<BlockMember> members;
        public uint set;
        public uint binding;
        public bool isTextureBlock;
    }

    public class ShaderModule : Resource
    {
        public ShaderStages Stage { get; set; }
        public string FileName { get; set; }
        public string FuncName { get; set; }
        public byte[] Code { get; set; }

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

        internal VulkanCore.ShaderModule shaderModule;

        public ShaderModule()
        {
        }

        public ShaderModule(ShaderStages shaderStages, string fileName, string funcName = "main")
        {
            Stage = shaderStages;
            FileName = fileName;
            FuncName = funcName;
            shaderModule = null;
        }

        public async override void Load(Stream stream)
        {
            var graphics = Get<Graphics>();
            Code = stream.ReadAllBytes();
            shaderModule = graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(Code));
        }

        public async override void Build()
        {
            var graphics = Get<Graphics>();
            var fileSystem = Get<FileSystem>();

            string path = Path.Combine(ResourceCache.ContentRoot, FileName);
            using (Stream stream = fileSystem.Open(path))
            {
                Code = stream.ReadAllBytes();
                shaderModule = graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(Code));
            }
        }

        public override void Dispose()
        {
            shaderModule?.Dispose();
            Code = null;

            base.Dispose();
        }

        public static ShaderModule Load(string path)
        {
            var graphics = Get<Graphics>();
            var fileSystem = Get<FileSystem>();
            const int defaultBufferSize = 4096;

            using (Stream stream = fileSystem.Open(path))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms, defaultBufferSize);

                return new ShaderModule
                {
                    shaderModule = graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(ms.ToArray()))
                };
            }
        }

    }
}
