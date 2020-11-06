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

                MipmapData[] mipmaps = new MipmapData[image.MipMaps.Length + 1];
                mipmaps[0] = new MipmapData((uint)image.DataLen, newData.AsSpan(0, image.DataLen).ToArray(), (uint)image.Width, (uint)image.Height);
                for (int i = 1; i < mipmaps.Length; i++)
                {
                    var mip = image.MipMaps[i - 1];
                    uint imageSize = (uint)mip.DataLen;
                    mipmaps[i] = new MipmapData(imageSize, newData.AsSpan(mip.DataOffset, mip.DataLen).ToArray(), (uint)mip.Width, (uint)mip.Height);
                }

                Format fmt = Format.R8g8b8a8Unorm;
                switch (image.Format)
                {
                    case Pfim.ImageFormat.Rgb8:
                        fmt = Format.R4g4UnormPack8;
                        break;
                    case Pfim.ImageFormat.R5g5b5:
                        //fmt = Format.R5g5b5UnormPack16;
                        break;
                    case Pfim.ImageFormat.R5g6b5:
                        fmt = Format.R5g6b5UnormPack16;
                        break;
                    case Pfim.ImageFormat.R5g5b5a1:
                        fmt = Format.R5g5b5a1UnormPack16;
                        break;
                    case Pfim.ImageFormat.Rgba16:
                        fmt = Format.R4g4b4a4UnormPack16;
                        break;
                    case Pfim.ImageFormat.Rgb24:
                        fmt = Format.R8g8b8Unorm;
                        break;
                    case Pfim.ImageFormat.Rgba32:
                        fmt = Format.B8g8r8a8Unorm;
                        break;
                }

                ImageData face = new ImageData((uint)image.Width, (uint)image.Height, (uint)mipmaps.Length, mipmaps);
                Texture tex = new Texture
                {
                    width = (uint)image.Width,
                    height = (uint)image.Height,
                    mipLevels = (uint)mipmaps.Length,
                    depth = 1,
                    format = fmt,
                    imageUsageFlags = ImageUsageFlags.Sampled,
                    imageLayout = ImageLayout.ShaderReadOnlyOptimal,
                };

                tex.SetImageData(new[] { face });
                return tex;
            }
        }

    }

    public class KtxTextureReader : ResourceReader<Texture>
    {
        public Format Format { get; set; } = Format.Bc3UnormBlock;
        public SamplerAddressMode SamplerAddressMode { get; set; } = SamplerAddressMode.Repeat;

        public KtxTextureReader() : base(".ktx")
        {
        }

        protected override bool OnLoad(Texture tex, File stream)
        {
            KtxFile texFile = KtxFile.Load(stream, false);

            Format fmt = Format;
            if(stream.Name.IndexOf("bc3_unorm", StringComparison.OrdinalIgnoreCase) != -1)
            {
                fmt = Format.Bc3UnormBlock;
            }
            else if(stream.Name.IndexOf("etc2_unorm", StringComparison.OrdinalIgnoreCase) != -1)
            {
                fmt = Format.Etc2R8g8b8a8UnormBlock;
            }
            else if (stream.Name.IndexOf("astc_8x8_unorm", StringComparison.OrdinalIgnoreCase) != -1)
            {
                fmt = Format.Astc8x8UnormBlock;
            }
            else if (stream.Name.IndexOf("rgba", StringComparison.OrdinalIgnoreCase) != -1)
            {
                fmt = Format.R8g8b8a8Unorm;
            }

            tex.format = fmt;
            tex.samplerAddressMode = SamplerAddressMode;

            if(texFile.Faces.Length == 6)
            {
                tex.imageCreateFlags = ImageCreateFlags.CubeCompatible;
            }

            tex.SetImageData(texFile.Faces);

            return tex;
        }

    }
}
