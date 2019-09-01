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
            tex.format = Format;
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
