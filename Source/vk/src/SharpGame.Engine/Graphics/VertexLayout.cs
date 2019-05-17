using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public enum VertexInputRate : uint
    {
        Vertex = 0,
        Instance = 1
    }

    [StructLayout(LayoutKind.Sequential)]
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

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexInputAttribute
    {
        public uint location;
        public uint binding;
        public Format format;
        public uint offset;

        public VertexInputAttribute(uint bind, uint loc, Format fmt, uint offset)
        {
            binding = bind;
            location = loc;
            format = fmt;
            this.offset = offset;
        }
        
    }

    public class VertexLayout
    {
        public VertexInputBinding[] vertexInputBindings;
        public VertexInputAttribute[] vertexInputAttributes;

        public VertexLayout()
        {
        }

        public VertexLayout(VertexInputBinding[] vertexInputBindings, VertexInputAttribute[] vertexInputAttributes)
        {
            this.vertexInputBindings = vertexInputBindings;
            this.vertexInputAttributes = vertexInputAttributes;
        }

        public unsafe Vulkan.VkPipelineVertexInputStateCreateInfo ToNative()
        {
            Vulkan.VkPipelineVertexInputStateCreateInfo native = Builder.VertexInputStateCreateInfo();
            native.vertexBindingDescriptionCount = (uint)vertexInputBindings.Length;
            native.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)Utilities.AllocToPointer(vertexInputBindings);
            native.vertexAttributeDescriptionCount = (uint)vertexInputAttributes.Length;
            native.pVertexAttributeDescriptions = (Vulkan.VkVertexInputAttributeDescription*)Utilities.AllocToPointer(vertexInputAttributes);
           
            return native;
        }
    }


}
