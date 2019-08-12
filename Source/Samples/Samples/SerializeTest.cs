using Hocon;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

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
                byte[] bytes = JsonSerializer.Serialize(mat);
                var text = JsonSerializer.PrettyPrint(bytes);
                System.IO.File.WriteAllText("test.material", text);

                using (File file = FileSystem.Instance.GetFile("Shaders/GLSL/UniformsVS.glsl"))
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
