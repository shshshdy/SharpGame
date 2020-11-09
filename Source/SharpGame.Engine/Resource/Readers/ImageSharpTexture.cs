using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vulkan;

namespace SharpGame.ImageSharp
{
    using Image = SixLabors.ImageSharp.Image;

    public class ImageSharpTexture : IDisposable
    {
        /// <summary>
        /// An array of images, each a single element in the mipmap chain.
        /// The first element is the largest, most detailed level, and each subsequent element
        /// is half its size, down to 1x1 pixel.
        /// </summary>
        public Image<Rgba32>[] Images { get; }

        /// <summary>
        /// The width of the largest image in the chain.
        /// </summary>
        public int Width => Images[0].Width;

        /// <summary>
        /// The height of the largest image in the chain.
        /// </summary>
        public int Height => Images[0].Height;

        /// <summary>
        /// The pixel format of all images.
        /// </summary>
        public Format Format { get; }

        /// <summary>
        /// The size of each pixel, in bytes.
        /// </summary>
        public uint PixelSizeInBytes => sizeof(byte) * 4;

        /// <summary>
        /// The number of levels in the mipmap chain. This is equal to the length of the Images array.
        /// </summary>
        public int MipLevels => Images.Length;

        public ImageSharpTexture(string path) : this(Image.Load<Rgba32>(path), true) { }
        public ImageSharpTexture(string path, bool mipmap) : this(Image.Load<Rgba32>(path), mipmap) { }
        public ImageSharpTexture(string path, bool mipmap, bool srgb) : this(Image.Load<Rgba32>(path), mipmap, srgb) { }
        public ImageSharpTexture(Stream stream) : this(Image.Load<Rgba32>(stream), true) { }
        public ImageSharpTexture(Stream stream, bool mipmap) : this(Image.Load<Rgba32>(stream), mipmap) { }
        public ImageSharpTexture(Stream stream, bool mipmap, bool srgb) : this(Image.Load<Rgba32>(stream), mipmap, srgb) { }
        public ImageSharpTexture(Image<Rgba32> image, bool mipmap = true) : this(image, mipmap, false) { }
        public ImageSharpTexture(Image<Rgba32> image, bool mipmap, bool srgb)
        {
            Format = srgb ? Format.R8g8b8a8Srgb : Format.R8g8b8a8Unorm;
            if (mipmap)
            {
                Images = MipmapHelper.GenerateMipmaps(image);
            }
            else
            {
                Images = new Image<Rgba32>[] { image };
            }
        }
  
        public unsafe Texture CreateDeviceTexture()
        {
            MipmapLevel[] mipmaps = new MipmapLevel[Images.Length];
            for(int i = 0; i < this.Images.Length; i++)
            {
                var image = Images[i];
                uint imageSize = (uint)(image.Width * image.Height * Unsafe.SizeOf<Rgba32>());
                mipmaps[i] = new MipmapLevel(imageSize, MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray(),
                    (uint)image.Width, (uint)image.Height, 1);
            }

            Texture tex = new Texture
            {
                width = (uint)Width,
                height = (uint)Height,
                mipLevels = (uint)MipLevels,
                depth = 1,
                format = Format,
                imageUsageFlags = ImageUsageFlags.Sampled,
                imageLayout = ImageLayout.ShaderReadOnlyOptimal,
            };

            tex.SetImageData(mipmaps);

            return tex;

        }

        public void Dispose()
        {
            foreach(var img in Images)
            {
                img.Dispose();
            }
        }

    }
}
