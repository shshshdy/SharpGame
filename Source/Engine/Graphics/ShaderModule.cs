﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame
{
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
        public ShaderStages Stage { get; set; }
        [DataMember]
        public string FuncName { get; set; }
        [DataMember]
        public byte[] Code { get; set; }

        [DataMember]
        public ShaderReflection ShaderReflection { get; set; }

        internal VulkanCore.ShaderModule shaderModule;

        public ShaderModule()
        {
        }

        public ShaderModule(ShaderStages stage, string fileName, string funcName = "main")
        {
            Stage = stage;
            FileName = fileName;
            FuncName = funcName;
            shaderModule = null;

            var res = Get<ResourceCache>();
            
            using (File stream = res.Open(fileName))
            {
                Code = stream.ReadAllBytes();
            }

            Build();
        }
        
        public async override Task<bool> Load(File stream)
        {
            var graphics = Get<Graphics>();
            Code = stream.ReadAllBytes();

            Build();

            return true;
        }

        protected override void OnBuild()
        {
            var graphics = Get<Graphics>();
            var fileSystem = Get<FileSystem>();
            var resourceCache = Get<ResourceCache>();

            if (Code == null)
            {
                using (File stream = resourceCache.Open(FileName))
                {
                    Code = stream.ReadAllBytes();
                }
            }

            shaderModule = Graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(Code));
        }

        protected override void Destroy()
        {
            shaderModule?.Dispose();
            Code = null;

            base.Dispose();
        }

    }
}
