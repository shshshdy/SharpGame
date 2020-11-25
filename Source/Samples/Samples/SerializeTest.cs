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
                var colorMap = Resources.Load<Texture>("textures/StoneDiffuse.png");
                var mat = new Material("Shaders/Basic.shader");
                mat.SetTexture("DiffMap", colorMap);

                {
                    var bytes = JsonSerializer.Serialize(mat, StandardResolver.ExcludeNull);
                    var text = JsonSerializer.PrettyPrint(bytes);
                    System.IO.File.WriteAllText("test.material", text);
                }

                {
                    var mat1 = Resources.Load<Material>("materials/Stone.material");
                    var bytes = MessagePack.MessagePackSerializer.Serialize(mat1, MessagePack.Resolvers.ContractlessStandardResolver.Options);

                    System.IO.File.WriteAllText("test1.material", MessagePack.MessagePackSerializer.ConvertToJson(bytes));
                    System.IO.File.WriteAllBytes("test2.material", bytes);
                }


            }

        }
    }
}
