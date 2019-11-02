using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Vulkan;

namespace SharpGame
{
    using System.Collections;

    public struct ShaderProperty
    {
        public UniformType type;
        public object value;
    }

    [DataContract]
    public class Shader : Resource, IEnumerable<Pass>
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Dictionary<string, ShaderProperty> Properties { get; set; }

        [DataMember]
        public List<Pass> Pass { get; set; } = new List<Pass>();

        [IgnoreDataMember]
        public ulong passFlags = 0;

        public Shader()
        {
        }

        public Shader(string name)
        {
            Name = name;
        }

        public Shader(params Pass[] passes)
        {
            foreach(var pass in passes)
            {
                Add(pass);
            }
        }

        public void Add(Pass pass)
        {
            pass.passIndex = Pass.Count;
            Pass.Add(pass);
            passFlags |= pass.passID;
        }

        [IgnoreDataMember]
        public Pass Main
        {
            get
            {
                return GetPass(1);
            }

            set
            {
                if(value.passID != 1)
                {
                    Log.Warn("Not a main pass.");
                    return;
                }

                for (int i = 0; i < Pass.Count; i++)
                {
                    if(Pass[i].passID == 1)
                    {
                        Pass[i].Dispose();
                        Pass[i] = value;
                    }
                }

                Add(value);
            }
        }

        public Pass GetPass(ulong id)
        {
            foreach (var pass in Pass)
            {
                if (pass.passID == id)
                {
                    return pass;
                }
            }

            return null;
        }

        public Pass GetPass(StringID name)
        {
            foreach(var pass in Pass)
            {
                if(pass.Name == name)
                {
                    return pass;
                }
            }

            return null;
        }

        protected override bool OnBuild()
        {
            int idx = 0;
            foreach (var pass in Pass)
            {
                pass.Build();
                pass.passIndex = idx++;
            }

            return true;
        }

        public IEnumerator<Pass> GetEnumerator()
        {
            return ((IEnumerable<Pass>)Pass).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Pass>)Pass).GetEnumerator();
        }

        protected override void Destroy()
        {
            foreach (var pass in Pass)
            {
                pass.Dispose();
            }

            Pass.Clear();

            base.Destroy();
        }

    }

    

}
