using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


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
        public VkFormat format;
        public uint offset;

        public VertexAttribute(uint bind, uint loc, VkFormat fmt, uint offset)
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
        public FastList<VertexAttribute> Attributes { get; private set; }

        bool needUpdate = true;
        public VertexLayout()
        {
        }

        public VertexLayout(VertexComponent[] vertexComponents, VertexComponent[] instanceComponents = null)
        {
            Attributes = new FastList<VertexAttribute>(vertexComponents.Length);
            uint offset = 0;
            for(uint i = 0; i < vertexComponents.Length; i++)
            {
                var fmt = GetFormat(vertexComponents[i]);
                Attributes.Add(new VertexAttribute
                {
                    location = i,
                    binding = 0,
                    format = fmt,
                    offset = offset
                });

                offset += GetFormatSize(fmt);
            }

            var vertexBinding = new VertexBinding(0, offset, VertexInputRate.Vertex);

            if(instanceComponents != null)
            {
                offset = 0;
                for (uint i = 0; i < instanceComponents.Length; i++)
                {
                    var fmt = GetFormat(instanceComponents[i]);
                    Attributes.Add(new VertexAttribute
                    {
                        location = (uint)vertexComponents.Length + i,
                        binding = 1,
                        format = fmt,
                        offset = offset
                    });

                    offset += GetFormatSize(fmt);
                }

                var instanceBinding = new VertexBinding(1, offset, VertexInputRate.Instance);

                Bindings = new[] { vertexBinding, instanceBinding };
            }
            else
            {

                Bindings = new[] { vertexBinding };
            }

            needUpdate = false;
        }

        public VertexLayout(VertexAttribute[] attributes, VertexBinding[] bindings = null)
        {
            this.Attributes = new FastList<VertexAttribute>(attributes);
            this.Bindings = bindings;
            needUpdate = (bindings== null);
        }

        void Update()
        {
            uint offset = 0;
            uint size = 0;
            foreach(var attr in Attributes)
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

        VkFormat GetFormat(VertexComponent vc)
        {
            switch (vc)
            {
                case VertexComponent.Position:
                case VertexComponent.Normal:
                    return VkFormat.R32G32B32SFloat;
                case VertexComponent.Texcoord:
                    return VkFormat.R32G32SFloat;
                case VertexComponent.Tangent:
                    return VkFormat.R32G32B32SFloat;
                case VertexComponent.Bitangent:
                    return VkFormat.R32G32B32SFloat;
                case VertexComponent.Color:
                    return VkFormat.R8G8B8A8UNorm;
                case VertexComponent.BlendIndices:
                    return VkFormat.R8G8B8A8UInt;
                case VertexComponent.BlendWeights:
                    return VkFormat.R32G32B32A32SFloat;

                case VertexComponent.Int1:
                    return VkFormat.R32SInt;
                case VertexComponent.Int2:
                    return VkFormat.R32G32SInt;
                case VertexComponent.Int3:
                    return VkFormat.R32G32B32SInt;
                case VertexComponent.Int4:
                    return VkFormat.R32G32B32A32SInt;

                case VertexComponent.Float1:
                    return VkFormat.R32SFloat;
                case VertexComponent.Float2:
                    return VkFormat.R32G32SFloat;
                case VertexComponent.Float3:
                    return VkFormat.R32G32B32SFloat;
                case VertexComponent.Float4:
                    return VkFormat.R32G32B32A32SFloat;
            }

            return VkFormat.Undefined;
        }
       
        uint GetFormatSize(VkFormat format)
        {
            switch(format)
            {
                case VkFormat.R8G8B8A8UNorm:
                case VkFormat.R16G16UNorm:
                case VkFormat.R16G16SNorm:
                case VkFormat.R32UInt:
                case VkFormat.R32SInt:
                case VkFormat.R32SFloat:
                case VkFormat.R8G8B8A8UInt:
                case VkFormat.R8G8B8A8SInt:
                    return 4;

                case VkFormat.R16G16B16A16UNorm:
                case VkFormat.R16G16B16A16SFloat:
                case VkFormat.R32G32UInt:
                case VkFormat.R32G32SInt:
                case VkFormat.R32G32SFloat:
                    return 8;
                case VkFormat.R32G32B32SFloat:
                    return 12;
                case VkFormat.R32G32B32A32SFloat:
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

            native = new VkPipelineVertexInputStateCreateInfo
            {
                sType = VkStructureType.PipelineVertexInputStateCreateInfo
            };
            native.vertexBindingDescriptionCount = (uint)Bindings.Length;
            native.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)Utilities.AsPointer(ref Bindings[0]);
            native.vertexAttributeDescriptionCount = (uint)Attributes.Count;
            native.pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)Utilities.AsPointer(ref Attributes.Front());           
        }

        public void Print()
        {
            Log.Info("vertex attribute : ");

            foreach (var attr in Attributes)
            {
                Log.Info("{{{0}, {1}, {2}}}", attr.location, attr.format, attr.offset);
            }
        }

        public void Add(VertexAttribute binding)
        {
            if(Attributes == null)
            {
                Attributes = new FastList<VertexAttribute>();
            }

            Attributes.Add(binding);
            needUpdate = true;
        }

        public IEnumerator<VertexAttribute> GetEnumerator()
        {
            return ((IEnumerable<VertexAttribute>)Attributes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<VertexAttribute>)Attributes).GetEnumerator();
        }
    }


}
