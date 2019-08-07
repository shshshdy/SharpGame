using Hocon;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 5)]
    public class SerializeTest : Sample
    {
       public override void Init()
        {
            base.Init();

            {
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
