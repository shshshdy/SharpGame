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
    public struct VertexBinding
    {
        public uint binding;
        public uint stride;
        public VertexInputRate inputRate;

        public VertexBinding(uint binding, uint stride, VertexInputRate inputRate)
        {
            this.binding = binding;
            this.stride = stride;
            this.inputRate = inputRate;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexAttribute
    {
        public uint location;
        public uint binding;
        public Format format;
        public uint offset;

        public VertexAttribute(uint bind, uint loc, Format fmt, uint offset)
        {
            binding = bind;
            location = loc;
            format = fmt;
            this.offset = offset;
        }
        
    }

    public enum VertexComponent : byte
    {
        Position,
        Texcoord,
        Normal,
        Tangent,
        Bitangent,
        BlendWeights,
        BlendIndices,
        Color,

        Int1,
        Int2,
        Int3,
        Int4,

        Float1,
        Float2,
        Float3,
        Float4,
    }

    public class VertexLayout : IEnumerable<VertexAttribute>
    {
        public VertexBinding[] Bindings { get; private set; }
        public FastList<VertexAttribute> attributes { get; private set; }

        bool needUpdate = true;
        public VertexLayout()
        {
        }

        public VertexLayout(VertexComponent[] vertexComponents)
        {
            attributes = new FastList<VertexAttribute>(vertexComponents.Length);
            uint offset = 0;
            for(uint i = 0; i < vertexComponents.Length; i++)
            {
                var fmt = GetFormat(vertexComponents[i]);
                attributes.Add(new VertexAttribute
                {
                    location = i,
                    binding = 0,
                    format = fmt,
                    offset = offset
                });

                offset += GetFormatSize(fmt);
            }

            needUpdate = true;
        }

        public VertexLayout(VertexAttribute[] attributes, VertexBinding[] bindings = null)
        {
            this.attributes = new FastList<VertexAttribute>(attributes);
            this.Bindings = bindings;
            needUpdate = (bindings== null);
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

            Bindings = new[] { new VertexBinding(0, offset + size, VertexInputRate.Vertex) };
        }

        Format GetFormat(VertexComponent vc)
        {
            switch (vc)
            {
                case VertexComponent.Position:
                case VertexComponent.Normal:
                    return Format.R32g32b32Sfloat;
                case VertexComponent.Texcoord:
                    return Format.R32g32Sfloat;
                case VertexComponent.Tangent:
                    return Format.R32g32b32Sfloat;
                case VertexComponent.Bitangent:
                    return Format.R32g32b32Sfloat;
                case VertexComponent.Color:
                    return Format.R8g8b8a8Unorm;
                case VertexComponent.BlendIndices:
                    return Format.R8g8b8a8Uint;
                case VertexComponent.BlendWeights:
                    return Format.R32g32b32a32Sfloat;

                case VertexComponent.Int1:
                    return Format.R32Sint;
                case VertexComponent.Int2:
                    return Format.R32g32Sint;
                case VertexComponent.Int3:
                    return Format.R32g32b32Sint;
                case VertexComponent.Int4:
                    return Format.R32g32b32a32Sint;

                case VertexComponent.Float1:
                    return Format.R32Sfloat;
                case VertexComponent.Float2:
                    return Format.R32g32Sfloat;
                case VertexComponent.Float3:
                    return Format.R32g32b32Sfloat;
                case VertexComponent.Float4:
                    return Format.R32g32b32a32Sfloat;
            }

            return Format.Undefined;
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
            native.vertexBindingDescriptionCount = (uint)Bindings.Length;
            native.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)Utilities.AsPointer(ref Bindings[0]);
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

        public void Add(VertexAttribute binding)
        {
            if(attributes == null)
            {
                attributes = new FastList<VertexAttribute>();
            }

            attributes.Add(binding);
            needUpdate = true;
        }

        public IEnumerator<VertexAttribute> GetEnumerator()
        {
            return ((IEnumerable<VertexAttribute>)attributes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<VertexAttribute>)attributes).GetEnumerator();
        }
    }


}
