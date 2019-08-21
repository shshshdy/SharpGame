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

        public override Resource Load(string name)
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
        public KtxTextureReader() : base(".ktx")
        {
        }

        public override Resource Load(string name)
        {
            if (!MatchExtension(name))
            {
                return null;
            }

            var resource = new Texture();
            resource.LoadFromFile(name, Format.Bc3UnormBlock, true);
            return resource;
        }

    }
}
