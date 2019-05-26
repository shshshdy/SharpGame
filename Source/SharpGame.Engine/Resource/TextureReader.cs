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
            String cachedAsset = FileUtil.ReplaceExtension(name, ".ktx");
            // Attempt to load the resource
            File stream = FileSystem.Instance.OpenFile(cachedAsset);
         
            if(stream == null)
            {
                string exeFileName = "tools\\texturec.exe";
                StringBuilder args = new StringBuilder();

                string sourceFile = FileSystem.Instance.GetResourceFileName(name);
                string destFile = "cache/" + cachedAsset;
                args.Append(" -f ").Append(sourceFile);

                args.Append(" -o ").Append(destFile);

                //args.Append(" -t ").Append("");

                Process process = new Process();
                try
                {
                    System.IO.Directory.CreateDirectory(FileUtil.GetPath(destFile));

                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = exeFileName;
                    process.StartInfo.Arguments = args.ToString();
                    process.StartInfo.CreateNoWindow = false;
                    process.Start();
                    process.WaitForExit();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                stream = FileSystem.Instance.OpenFile(cachedAsset);
            }

            if(stream == null)
            {
                stream = FileSystem.Instance.OpenFile(name);
            }

            if(stream == null)
                return null;

            var resource = Activator.CreateInstance<Texture>();

            if(!resource)
            {
                Log.Error("Could not load unknown resource type " + ResourceType.ToString());
                return null;
            }
           
            if(!OnLoad(resource, stream))
            {
                stream.Dispose();
                resource.Dispose();
                return null;
                
            }

            stream.Dispose();
            return resource;
        }
        

    }
}
