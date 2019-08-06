﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    public class SharpTextureReader : ResourceReader<Texture2D>
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

            using(var stream = FileSystem.GetFile(name))
            using (var imageSharpTexture
                 = new ImageSharp.ImageSharpTexture(stream))
                return imageSharpTexture.CreateDeviceTexture();
        }

    }

    public class KtxTexture2DReader : ResourceReader<Texture2D>
    {
        public KtxTexture2DReader() : base(".ktx")
        {
        }

        public override Resource Load(string name)
        {
            if(!MatchExtension(name))
            {
                return null;
            }

            var resource = new Texture2D();
            resource.LoadFromFile(name, Format.Bc3UnormBlock);
            return resource;
        }

    }

    public class KtxTextureCubeReader : ResourceReader<Texture2D>
    {
        public KtxTextureCubeReader() : base(".ktx")
        {
        }

        public override Resource Load(string name)
        {
            if (!MatchExtension(name))
            {
                return null;
            }

            var resource = new Texture2D();
            resource.LoadFromFile(name, Format.Bc3UnormBlock);
            return resource;
        }

    }
}
