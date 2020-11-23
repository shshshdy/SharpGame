using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;


namespace SharpGame
{
    using static Vulkan;

    /// <summary>
    /// Structure specifying a clear color value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ClearColorValue
    {
        /// <summary>
        /// Are the color clear values when the format of the image or attachment is one of the
        /// formats other than signed integer or unsigned integer. Floating point values are
        /// automatically converted to the format of the image, with the clear value being treated as
        /// linear if the image is sRGB.
        /// </summary>
        [FieldOffset(0)] public Color4 Float4;
        /// <summary>
        /// Are the color clear values when the format of the image or attachment is signed integer.
        /// Signed integer values are converted to the format of the image by casting to the smaller
        /// type (with negative 32-bit values mapping to negative values in the smaller type). If the
        /// integer clear value is not representable in the target type (e.g. would overflow in
        /// conversion to that type), the clear value is undefined.
        /// </summary>
        //[FieldOffset(0)] public Int4 Int4;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearColorValue"/> structure.
        /// </summary>
        /// <param name="value">
        /// Are the color clear values when the format of the image or attachment is one of the
        /// formats other than signed integer or unsigned integer. Floating point values are
        /// automatically converted to the format of the image, with the clear value being treated as
        /// linear if the image is sRGB.
        /// </param>
        public ClearColorValue(Color4 value) : this()
        {
            Float4 = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearColorValue"/> structure.
        /// </summary>
        /// <param name="value">
        /// Are the color clear values when the format of the image or attachment is signed integer.
        /// Signed integer values are converted to the format of the image by casting to the smaller
        /// type (with negative 32-bit values mapping to negative values in the smaller type). If the
        /// integer clear value is not representable in the target type (e.g. would overflow in
        /// conversion to that type), the clear value is undefined.
        /// </param>
        //         public ClearColorValue(Int4 value) : this()
        //         {
        //             Int4 = value;
        //         }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearColorValue"/> structure.
        /// </summary>
        /// <param name="r">The red clear value.</param>
        /// <param name="g">The green clear value.</param>
        /// <param name="b">The blue clear value.</param>
        /// <param name="a">The alpha clear value.</param>
        public ClearColorValue(float r, float g, float b, float a = 1.0f) : this()
        {
            Float4 = new Color4(r, g, b, a);
        }
    }

    /// <summary>
    /// Structure specifying a clear depth stencil value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ClearDepthStencilValue
    {
        /// <summary>
        /// The clear value for the depth aspect of the depth/stencil attachment. It is a
        /// floating-point value which is automatically converted to the attachment’s format.
        /// <para>Must be between 0.0 and 1.0, inclusive.</para>
        /// </summary>
        public float Depth;
        /// <summary>
        /// The clear value for the stencil aspect of the depth/stencil attachment. It is a 32-bit
        /// integer value which is converted to the attachment's format by taking the appropriate
        /// number of LSBs.
        /// </summary>
        public int Stencil;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearDepthStencilValue"/> structure.
        /// </summary>
        /// <param name="depth">
        /// The clear value for the depth aspect of the depth/stencil attachment. It is a
        /// floating-point value which is automatically converted to the attachment’s format.
        /// </param>
        /// <param name="stencil">
        /// The clear value for the stencil aspect of the depth/stencil attachment. It is a 32-bit
        /// integer value which is converted to the attachment's format by taking the appropriate
        /// number of LSBs.
        /// </param>
        public ClearDepthStencilValue(float depth, int stencil)
        {
            Depth = depth;
            Stencil = stencil;
        }
    }

    /// <summary>
    /// Structure specifying a clear value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ClearValue
    {
        /// <summary>
        /// Specifies the color image clear values to use when clearing a color image or attachment.
        /// </summary>
        [FieldOffset(0)] public ClearColorValue Color;
        /// <summary>
        /// Specifies the depth and stencil clear values to use when clearing a depth/stencil image
        /// or attachment.
        /// </summary>
        [FieldOffset(0)] public ClearDepthStencilValue DepthStencil;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearValue"/> structure.
        /// </summary>
        /// <param name="color">
        /// Specifies the color image clear values to use when clearing a color image or attachment.
        /// </param>
        public ClearValue(ClearColorValue color) : this()
        {
            Color = color;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearValue"/> structure.
        /// </summary>
        /// <param name="depthStencil">
        /// Specifies the depth and stencil clear values to use when clearing a depth/stencil image
        /// or attachment.
        /// </param>
        public ClearValue(ClearDepthStencilValue depthStencil) : this()
        {
            DepthStencil = depthStencil;
        }

        /// <summary>
        /// Implicitly converts an instance of <see cref="ClearColorValue"/> to an instance of <see cref="ClearValue"/>.
        /// </summary>
        /// <param name="value">Instance to convert.</param>
        public static implicit operator ClearValue(ClearColorValue value) => new ClearValue(value);

        /// <summary>
        /// Implicitly converts an instance of <see cref="ClearDepthStencilValue"/> to an instance of
        /// <see cref="ClearValue"/>.
        /// </summary>
        /// <param name="value">Instance to convert.</param>
        public static implicit operator ClearValue(ClearDepthStencilValue value) => new ClearValue(value);
    }

    /// <summary>
    /// Specify how commands in the first subpass of a render pass are provided.
    /// </summary>
    public enum SubpassContents
    {
        /// <summary>
        /// Specifies that the contents of the subpass will be recorded inline in the primary command
        /// buffer, and secondary command buffers must not be executed within the subpass.
        /// </summary>
        Inline = 0,
        /// <summary>
        /// Specifies that the contents are recorded in secondary command buffers that will be called
        /// from the primary command buffer, and <see cref="CommandBuffer.CmdExecuteCommands"/> is
        /// the only valid command on the command buffer until <see
        /// cref="CommandBuffer.vkCmdNextSubpass"/> or <see cref="CommandBuffer.CmdEndRenderPass"/>.
        /// </summary>
        SecondaryCommandBuffers = 1
    }

    public struct BufferCopy
    {
        public ulong srcOffset;
        public ulong dstOffset;
        public ulong size;
    }

    /// <summary>
    /// Structure specifying an image resolve operation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageResolve
    {
        /// <summary>
        /// Specifies the image subresource of the source image data. Resolve of depth/stencil image
        /// is not supported.
        /// </summary>
        public ImageSubresourceLayers SrcSubresource;
        /// <summary>
        /// Selects the initial <c>X</c>, <c>Y</c>, and <c>Z</c> offsets in texels of the sub-region
        /// of the source image data.
        /// </summary>
        public Offset3D SrcOffset;
        /// <summary>
        /// Specifies the image subresource of the destination image data. Resolve of depth/stencil
        /// image is not supported.
        /// </summary>
        public ImageSubresourceLayers DstSubresource;
        /// <summary>
        /// Selects the initial <c>X</c>, <c>Y</c>, and <c>Z</c> offsets in texels of the sub-region
        /// of the destination image data.
        /// </summary>
        public Offset3D DstOffset;
        /// <summary>
        /// The size in texels of the source image to resolve in width, height and depth.
        /// </summary>
        public Extent3D Extent;
    }

    public struct BufferImageCopy
    {
        public ulong bufferOffset;
        public uint bufferRowLength;
        public uint bufferImageHeight;
        public ImageSubresourceLayers imageSubresource;
        public Offset3D imageOffset;
        public Extent3D imageExtent;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBarrier
    {
        internal VkMemoryBarrier native;
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryBarrier"/> structure.
        /// </summary>
        /// <param name="srcAccessMask">Specifies a source access mask.</param>
        /// <param name="dstAccessMask">Specifies a destination access mask.</param>
        public MemoryBarrier(AccessFlags srcAccessMask, AccessFlags dstAccessMask)
        {
            native = VkMemoryBarrier.New();
            native.srcAccessMask = (VkAccessFlags)srcAccessMask;
            native.dstAccessMask = (VkAccessFlags)dstAccessMask;
        }
    }

    /// <summary>
    /// Structure specifying a buffer memory barrier.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BufferMemoryBarrier
    {
        public Buffer Buffer { set => native.buffer = value.buffer; }

        internal VkBufferMemoryBarrier native;

        public BufferMemoryBarrier(Buffer buffer, AccessFlags srcAccessMask, AccessFlags dstAccessMask, ulong offset = 0, ulong size = WholeSize)
            : this(buffer, srcAccessMask, dstAccessMask, uint.MaxValue, uint.MaxValue, offset, size)
        {
        }

        public BufferMemoryBarrier(Buffer buffer, AccessFlags srcAccessMask, AccessFlags dstAccessMask,
            uint srcQueueFamilyIndex, uint dstQueueFamilyIndex, ulong offset = 0, ulong size = WholeSize)
        {
            native = VkBufferMemoryBarrier.New();
            native.buffer = buffer.buffer;
            native.offset = offset;
            native.size = size;
            native.srcAccessMask = (VkAccessFlags)srcAccessMask;
            native.dstAccessMask = (VkAccessFlags)dstAccessMask;
            native.srcQueueFamilyIndex = srcQueueFamilyIndex;
            native.dstQueueFamilyIndex = dstQueueFamilyIndex;
        }

    }
}
