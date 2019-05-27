using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    public class TextureReader : ResourceReader<Texture>
    {
        public override Resource Load(string name)
        {
            string ext = FileUtil.GetExtension(name);
            switch(ext)
            {
                case ".ktx":
                    return LoadKTX(name);
                   
                case ".tga":
                case ".png":
                case ".jpg":
                case ".gif":
                case ".bmp":
                    return LoadImageSharp(name);
                  
            }

            string cachedAsset = FileUtil.ReplaceExtension(name, ".asset");
            // Attempt to load the resource
            File stream = FileSystem.OpenFile(cachedAsset);
         
            if(stream == null)
            {
                stream = FileSystem.Instance.OpenFile(cachedAsset);
            }

            if(stream == null)
            {
                stream = FileSystem.Instance.OpenFile(name);
            }

            if(stream == null)
                return null;

            var resource = new Texture2D();          
            if(!OnLoad(resource, stream))
            {
                stream.Dispose();
                resource.Dispose();
                return null;                
            }

            stream.Dispose();
            return resource;
        }

        Texture LoadKTX(string name)
        {
            var resource = new Texture2D();
            resource.LoadFromFile(name, Format.Bc3UnormBlock);
            return resource;
        }

        Texture LoadImageSharp(string name)
        {
            using (File stream = FileSystem.OpenFile(name))
            using (ImageSharp.ImageSharpTexture imageSharpTexture
                 = new ImageSharp.ImageSharpTexture(stream))
                return imageSharpTexture.CreateDeviceTexture();
        }


    }
}
