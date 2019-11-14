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

    [Flags]
    public enum VertexMask
    {
        Position = 0x1,
        Texcoord1 = 0x2,
        Normal = 0x4,
        Tangent = 0x8,
        BlendWeights = 0x10,
        BlendIndices = 0x20,
        Color = 0x40,
        Texcoord2 = 0x80,
        InstanceMatrix1 = 0x100,
        InstanceMatrix2 = 0x200,
        InstanceMatrix3 = 0x400,
        InstanceMatrix4 = 0x800,
    }

    public class VertexLayout : IEnumerable<VertexInputAttribute>
    {
        public VertexInputBinding[] bindings;
        public FastList<VertexInputAttribute> attributes;
        bool needUpdate = true;
        public VertexLayout()
        {
        }

        public VertexLayout(params VertexInputAttribute[] attributes)
        {
            this.attributes = new FastList<VertexInputAttribute>(attributes);            
        }

        public VertexLayout(VertexInputBinding[] bindings, VertexInputAttribute[] attributes)
        {
            this.bindings = bindings;
            this.attributes = new FastList<VertexInputAttribute>(attributes);
            needUpdate = false;
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
                default:
                    Log.Error("Error format size : " + format);
                    break;
            }

            return 0;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public unsafe void ToNative(out VkPipelineVertexInputStateCreateInfo native)
        {
            if(needUpdate)
            {
                Update();
                needUpdate = false;
            }

            native = VkPipelineVertexInputStateCreateInfo.New();
            native.vertexBindingDescriptionCount = (uint)bindings.Length;
            native.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)Utilities.AsPointer(ref bindings[0]);
            native.vertexAttributeDescriptionCount = (uint)attributes.Count;
            native.pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)Utilities.AsPointer(ref attributes.Front());           
        }

        public void Print()
        {
            Log.Info("vertex attribute : ");

            foreach (var attr in attributes)
            {
                Log.Info("{{{0}, {1}, {2}}}", attr.location, attr.format, attr.offset);
            }
        }

        public void Add(VertexInputAttribute binding)
        {

            needUpdate = true;
        }

        public IEnumerator<VertexInputAttribute> GetEnumerator()
        {
            return ((IEnumerable<VertexInputAttribute>)attributes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<VertexInputAttribute>)attributes).GetEnumerator();
        }
    }


}
