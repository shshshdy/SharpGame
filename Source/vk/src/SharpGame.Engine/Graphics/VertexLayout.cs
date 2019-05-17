using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public enum VertexInputRate
    {
        Vertex = 0,
        Instance = 1
    }

    public struct VertexInputBinding
    {
        public uint binding;
        public uint stride;
        public VertexInputRate inputRate;

        public VertexInputBinding(uint binding, uint stride, VertexInputRate inputRate)
        {
            this.binding = binding;
            this.stride = stride;
            this.inputRate = inputRate;
        }
    }

    public struct VertexInputAttribute
    {
        public uint location;
        public uint binding;
        public Format format;
        public uint offset;

        public VertexInputAttribute(uint location, uint binding, Format format, uint offset)
        {
            this.location = location;
            this.binding = binding;
            this.format = format;
            this.offset = offset;
        }
        
    }

    public struct VertexLayout
    {
        public VertexInputBinding[] vertexInputBindings;
        public VertexInputAttribute[] vertexInputAttributes;

        public VertexLayout(VertexInputBinding[] vertexInputBindings, VertexInputAttribute[] vertexInputAttributes)
        {
            this.vertexInputBindings = vertexInputBindings;
            this.vertexInputAttributes = vertexInputAttributes;
        }

        internal unsafe void ToNative(out Vulkan.VkPipelineVertexInputStateCreateInfo native)
        {
            native = Vulkan.VkPipelineVertexInputStateCreateInfo.New();
            native.vertexBindingDescriptionCount = (uint)vertexInputBindings.Length;
            native.pVertexBindingDescriptions = (Vulkan.VkVertexInputBindingDescription*)Unsafe.AsPointer(ref vertexInputBindings[0]);
            native.vertexAttributeDescriptionCount = (uint)vertexInputAttributes.Length;
            native.pVertexAttributeDescriptions = (Vulkan.VkVertexInputAttributeDescription*)Unsafe.AsPointer(ref vertexInputAttributes[0]);
        }
    }


}
