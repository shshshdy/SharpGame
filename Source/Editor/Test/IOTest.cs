using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;
using Newtonsoft.Json;
using Unique;
using MessagePack.Formatters;
using System.Runtime.InteropServices;

namespace UniqueEditor
{

    [MessagePackFormatter(typeof(TestObj.Formatter))]
    public class TestObj
    {
        [DataMember]
        public Vector3 pos;
        [DataMember]
        public Vector3 rot;
        [DataMember]
        public Vector3 scale;


        public override String ToString()
        {
            return pos.ToString();
        }

        public static TestObj existObject;

        public class Formatter : IMessagePackFormatter<TestObj>
        {

            public int Serialize(ref byte[] bytes, int offset, TestObj value, IFormatterResolver formatterResolver)
            {
                int startOffset = offset;
                offset += formatterResolver.GetFormatterWithVerify<Vector3>().Serialize(ref bytes, offset, value.pos, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<Vector3>().Serialize(ref bytes, offset, value.pos, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<Vector3>().Serialize(ref bytes, offset, value.pos, formatterResolver);
                return offset - startOffset;
            }

            public TestObj Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                if (MessagePackBinary.IsNil(bytes, offset))
                {
                    readSize = 1;
                    return null;
                }
                else
                {
                    TestObj obj = TestObj.existObject != null ? existObject : new TestObj();

                    if(existObject != null)
                        Console.WriteLine("exist Object");
                    else
                        Console.WriteLine("new Object");

                    Deserialize(obj, bytes, offset, formatterResolver, out readSize);
                    return obj;
                }
            }


            public void Deserialize(TestObj obj, byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {                
                var startOffset = offset;
                obj.pos = formatterResolver.GetFormatterWithVerify<Vector3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                obj.rot = formatterResolver.GetFormatterWithVerify<Vector3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                obj.scale = formatterResolver.GetFormatterWithVerify<Vector3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                readSize = offset - startOffset;               
            }
        }

    }


    class IOTest
    {
        public static void Shader()
        {

            Shader shader = new Shader
            {
                Name = "Solid",

                TextureSlot =
                {
                    new TextureSlot
                    {
                        Name = "diffuse",
                    }
                },


                Uniform =
                {
                    new UniformInfo
                    {
                        Name = "diffuseColor",
                        Type = ShaderParamType.COLOR
                    },

                    new UniformInfo
                    {
                        Name = "ambientColor",
                        Type = ShaderParamType.COLOR
                    }
                },

                Pass =
                {
                    new ShaderPass
                    {
                        Name = "main",
                        VertexShader = new ShaderStage
                        {
                            Defines = "SKIN VERTEX_COLOR"
                        },

                        PixelShader = new ShaderStage
                        {
                            Defines = "NORMAL_MAP RELECTION_MAP"
                        }
                    }
                }
    
            };


            string path = "Cache/Shader.bin";
            TestIO<Shader>(shader, path);          

        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct AAAA
        {
           public int size;
           public IntPtr aaa;
            /*
            public bool Equals(ref AAAA other)
            {
                throw new NotImplementedException();
            }*/

            /// <summary>
            /// Tests for equality between two objects.
            /// </summary>
            /// <param name="left">The first value to compare.</param>
            /// <param name="right">The second value to compare.</param>
            /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
            [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
            public static bool operator ==(AAAA left, AAAA right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// Tests for inequality between two objects.
            /// </summary>
            /// <param name="left">The first value to compare.</param>
            /// <param name="right">The second value to compare.</param>
            /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
            [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
            public static bool operator !=(AAAA left, AAAA right)
            {
                return !left.Equals(right);
            }
        }

        public static void Go()
        {
            IntPtr ptr = Utilities.Allocate(100);
            AAAA aaaa = new AAAA { size = 100, aaa = ptr };
            AAAA bbbb = new AAAA { size = 100, aaa = ptr };

            Console.WriteLine((aaaa == bbbb)  ?"true" : " false");

            //TestSize();

            //Console.Read();

            //PesistDatabase();

            TestObj data = new TestObj
            {
                pos = new Vector3(1,2, 3)
            };

            string path = "Cache/TestObj.bin";

            TestIO<TestObj>(data, path);


            Console.ReadLine();
        }


        public struct AA
        {
            public float f;
            public string str;
        }

        static unsafe void TestSize()
        {
            Console.WriteLine("size : " + Unsafe.SizeOf<AA>());


            Console.WriteLine("size of string: " + Unsafe.SizeOf<string>());
            Console.WriteLine("size of object: " + Unsafe.SizeOf<object>());
            Console.WriteLine("size of List<int>: " + Unsafe.SizeOf<List<int>>());
            Console.WriteLine("size of int[]: " + Unsafe.SizeOf<int[]>());
            Console.WriteLine("size of Dictionary<string, object>: " + Unsafe.SizeOf<Dictionary<string, object>>());

            {
                AA[] aa = new AA[2];
                aa[0].str = "test";
                aa[1].f = 100.0f;
                Console.WriteLine(aa[0].str);
                Console.WriteLine(aa[1].f);
            }
            {
                AA[][] aaa = null;// new AA[2][];
                Array.Resize(ref aaa, 2);
                Array.Resize(ref aaa[0], 2);

                AA[] aa = aaa[0];
                aa[0].str = "test";
                aa[1].f = 100.0f;
                Console.WriteLine(aa[0].str);
                Console.WriteLine(aa[1].f);
            }
        }


        public static void PesistDatabase()
        {
            PesistDatabase data = new UniqueEditor.PesistDatabase();
            for(int i = 0; i < 10; i++)
            {
                data.Add(AssetType.Dir, "Test" + i, Guid.NewGuid());
            }

            
            string path = "Cache/Pesist.bin";
            TestIO<PesistDatabase>(data, path);

            
        }

        public static void TestIO<T>(T obj, string path) where T : class
        {
            Console.WriteLine("Test object serializer : " + typeof(T).ToString());

            byte[] bytes = MessagePackSerializer.Serialize<T>(obj);

            System.IO.File.WriteAllBytes(path, bytes);

            T newObj = MessagePackSerializer.Deserialize<T>(System.IO.File.ReadAllBytes(path));

            Console.WriteLine(JsonConvert.SerializeObject(newObj));
            
            if(newObj is TestObj)
            {
                TestObj o = newObj as TestObj;
                o.pos = new Vector3(111, 222, 333);

                byte[] newBytes = MessagePackSerializer.Serialize<T>(newObj);

                var formatter = MessagePackSerializer.DefaultResolver.GetFormatterWithVerify<TestObj>();

                //TestObj.existObject = obj as TestObj;
                //TestObj t = formatter.Deserialize(newBytes, 0, MessagePackSerializer.DefaultResolver, out int readSize);
                ((TestObj.Formatter)formatter).Deserialize(obj as TestObj, newBytes, 0, MessagePackSerializer.DefaultResolver, out int readSize);
                Console.WriteLine(JsonConvert.SerializeObject(obj));
                //Console.WriteLine(JsonConvert.SerializeObject(t));
            }

        }
    }
}
