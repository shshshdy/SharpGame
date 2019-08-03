using Hocon;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -5)]
    public class SerializeTest : Sample
    {
        public override void Init()
        {
            base.Init();

            {
                var file = FileSystem.Instance.GetFile("Shaders/Textured.shader");
                Shader shader = HoconSerializer.Deserialize<Shader>(file);
                

            }

            /*
            Task<string> ShaderResolver(HoconCallbackType type, string fileName)
            {
                switch (type)
                {
                    case HoconCallbackType.Resource:
                        return null;// ReadResource(fileName);
                    case HoconCallbackType.File:
                        return Task.FromResult("Include = \"" + FileSystem.ReadAllText(fileName) + "\"");
                    default:
                        return null;
                }
            }

            {
                var file = FileSystem.Instance.GetFile("Shaders/Textured.shader");
                Hocon.HoconRoot root = HoconSerializer.Parse(file, ShaderResolver);
                Console.WriteLine(root.PrettyPrint(4));
            }
            */


        }
    }
}
