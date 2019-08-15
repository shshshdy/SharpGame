using Hocon;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Resolvers;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 5)]
    public class SerializeTest : Sample
    {
       public override void Init()
        {
            base.Init();

            {
                var colorMap = Resources.Load<Texture2D>("textures/StoneDiffuse.png");
                var mat = new Material("Shaders/Basic.shader");
                mat.SetTexture("DiffMap", colorMap);

                {
                    var bytes = JsonSerializer.Serialize(mat, StandardResolver.ExcludeNull);
                    var text = JsonSerializer.PrettyPrint(bytes);
                    System.IO.File.WriteAllText("test.material", text);
                }

                var mat1 = Resources.Load<Material>("materials/Stone.material");

                {
                    var bytes = MessagePack.MessagePackSerializer.Serialize(mat, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

                    System.IO.File.WriteAllText("test1.material", MessagePack.MessagePackSerializer.ToJson(bytes));
                    System.IO.File.WriteAllBytes("test2.material", bytes);
                }

                using (File file = FileSystem.Instance.GetFile("Shaders/Common/UniformsVS.glsl"))
                {
                    LayoutParser layoutParser = new LayoutParser(file);
                    var layouts = layoutParser.Parse();
                }

                /*
                var file = FileSystem.Instance.GetFile("Shaders/Textured.shader");
                //Shader shader = HoconSerializer.Deserialize<Shader>(file);
                AstParser ast = new AstParser();
                ast.Parse(file.ReadAllText());
                ast.Print();

                var shader = Resources.Load<Shader>("Shaders/Textured.shader");*/
            }

        }
    }
}
