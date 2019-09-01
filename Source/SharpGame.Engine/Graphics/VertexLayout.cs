using System;
using System.Collections;
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
        public VertexInputBinding[] bindings;
        public VertexInputAttribute[] attributes;

        public VertexLayout()
        {
        }

        public VertexLayout(params VertexInputAttribute[] attributes)
        {
            this.attributes = attributes;
            Update();
        }

        public VertexLayout(VertexInputBinding[] bindings, VertexInputAttribute[] attributes)
        {
            this.bindings = bindings;
            this.attributes = attributes;
        }

        void Update()
        {
            uint offset = 0;
            uint size = 0;
            foreach(var attr in attributes)
            {
                if(attr.binding != 0)
                {
                    continue;
                }

                if(attr.offset >= offset)
                {
                    offset = attr.offset;
                    size = GetFormatSize(attr.format);
                }
            }

            bindings = new[] { new VertexInputBinding(0, offset + size, VertexInputRate.Vertex) };
        }

        uint GetFormatSize(Format format)
        {
            switch(format)
            {
                case Format.R8g8b8a8Unorm:
                case Format.R16g16Unorm:
                case Format.R16g16Snorm:
                case Format.R32Uint:
                case Format.R32Sint:
                case Format.R32Sfloat:
                case Format.R8g8b8a8Uint:
                case Format.R8g8b8a8Sint:
                    return 4;

                case Format.R16g16b16a16Unorm:
                case Format.R16g16b16a16Sfloat:
                case Format.R32g32Uint:
                case Format.R32g32Sint:
                case Format.R32g32Sfloat:
                    return 8;
                case Format.R32g32b32Sfloat:
                    return 12;
                case Format.R32g32b32a32Sfloat:
                    return 16;
            }

            return 0;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public unsafe void ToNative(out VkPipelineVertexInputStateCreateInfo native)
        {
            native = VkPipelineVertexInputStateCreateInfo.New();
            native.vertexBindingDescriptionCount = (uint)bindings.Length;
            native.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)Utilities.AllocToPointer(bindings);
            native.vertexAttributeDescriptionCount = (uint)attributes.Length;
            native.pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)Utilities.AllocToPointer(attributes);           
        }

        public void Print()
        {
            Log.Info("vertex attribute : ");

            foreach (var attr in attributes)
            {
                Log.Info("{{{0}, {1}, {2}}}", attr.location, attr.format, attr.offset);
            }
        }

    }


}
