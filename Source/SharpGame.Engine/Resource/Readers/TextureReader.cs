using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    public class SharpTextureReader : ResourceReader<Texture>
    {
        public SharpTextureReader() : base(".tga|.png|.jpg|.gif|.bmp")
        {
        }

        public override Resource LoadResource(string name)
        {
            if (!MatchExtension(name))
            {
                return null;
            }

            using (var stream = FileSystem.GetFile(name))
            {
                if(stream == null)
                {
                    return null;
                }

                using (var imageSharpTexture
                     = new ImageSharp.ImageSharpTexture(stream, true, false))
                    return imageSharpTexture.CreateDeviceTexture();
            }
        }

    }

    public class DDSTextureReader : ResourceReader<Texture>
    {
        public DDSTextureReader() : base(".dds")
        {
        }

        public override Resource LoadResource(string name)
        {
            if (!MatchExtension(name))
            {
                return null;
            }

            using (var stream = FileSystem.GetFile(name))
            {
                if (stream == null)
                {
                    return null;
                }

                byte[] newData;
                Pfim.IImage image = Pfim.Pfim.FromStream(stream);
                if(image.Compressed)
                {
                    image.Decompress();
                }

                var tightStride = image.Width * image.BitsPerPixel / 8;
                if (image.Stride != tightStride)
                {
                    newData = new byte[image.Height * tightStride];
                    for (int i = 0; i < image.Height; i++)
                    {
                        System.Buffer.BlockCopy(image.Data, i * image.Stride, newData, i * tightStride, tightStride);
                    }
                }
                else
                {
                    newData = image.Data;
                }

                MipmapLevel[] mipmaps = new MipmapLevel[image.MipMaps.Length + 1];
                mipmaps[0] = new MipmapLevel((uint)image.DataLen, newData.AsSpan(0, image.DataLen).ToArray(),
                    (uint)image.Width, (uint)image.Height, 1);
                for (int i = 1; i < mipmaps.Length; i++)
                {
                    var mip = image.MipMaps[i - 1];
                    uint imageSize = (uint)mip.DataLen;
                    mipmaps[i] = new MipmapLevel(imageSize, newData.AsSpan(mip.DataOffset, mip.DataLen).ToArray(), (uint)mip.Width, (uint)mip.Height, 1);
                }

                VkFormat fmt = VkFormat.R8G8B8A8UNorm;
                switch (image.Format)
                {
                    case Pfim.ImageFormat.Rgb8:
                        fmt = VkFormat.R4G4UNormPack8;
                        break;
                    case Pfim.ImageFormat.R5g5b5:
                        //fmt = VkFormat.R5g5b5UnormPack16;
                        break;
                    case Pfim.ImageFormat.R5g6b5:
                        fmt = VkFormat.R5G6B5UNormPack16;
                        break;
                    case Pfim.ImageFormat.R5g5b5a1:
                        fmt = VkFormat.R5G5B5A1UNormPack16;
                        break;
                    case Pfim.ImageFormat.Rgba16:
                        fmt = VkFormat.R4G4B4A4UNormPack16;
                        break;
                    case Pfim.ImageFormat.Rgb24:
                        fmt = VkFormat.R8G8B8UNorm;
                        break;
                    case Pfim.ImageFormat.Rgba32:
                        fmt = VkFormat.B8G8R8A8UNorm;
                        break;
                }

                Texture tex = new Texture
                {
                    extent = new VkExtent3D((uint)image.Width, (uint)image.Height, 1),                  
                    mipLevels = (uint)mipmaps.Length,
                    format = fmt,
                    imageUsageFlags = VkImageUsageFlags.Sampled,
                    imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
                };

                tex.SetImageData(mipmaps);
                return tex;
            }
        }

    }

    public class KtxTextureReader : ResourceReader<Texture>
    {
        public VkFormat VkFormat { get; set; } = VkFormat.BC3UNormBlock;
        public VkSamplerAddressMode SamplerAddressMode { get; set; } = VkSamplerAddressMode.Repeat;

        public KtxTextureReader() : base(".ktx")
        {
        }

        protected override bool OnLoad(Texture tex, File stream)
        {
            KtxFile texFile = KtxFile.Load(stream, false);
            Debug.Assert(!texFile.Header.SwapEndian);

            VkFormat fmt = VkFormat;
            if(stream.Name.IndexOf("bc3_unorm", StringComparison.OrdinalIgnoreCase) != -1)
            {
                fmt = VkFormat.BC3UNormBlock;
            }
            else if(stream.Name.IndexOf("etc2_unorm", StringComparison.OrdinalIgnoreCase) != -1)
            {
                fmt = VkFormat.ETC2R8G8B8A8UNormBlock;
            }
            else if (stream.Name.IndexOf("astc_8x8_unorm", StringComparison.OrdinalIgnoreCase) != -1)
            {
                fmt = VkFormat.ASTC8x8UNormBlock;
            }
            else if (stream.Name.IndexOf("rgba", StringComparison.OrdinalIgnoreCase) != -1)
            {
                fmt = VkFormat.R8G8B8A8UNorm;
            }

            tex.format = fmt;
            tex.samplerAddressMode = SamplerAddressMode;

            if(texFile.Header.IsCubeMap)
            {
                tex.imageCreateFlags = VkImageCreateFlags.CubeCompatible;
            }

            tex.SetImageData(texFile.Mipmaps);

            return tex;
        }

    }
}
