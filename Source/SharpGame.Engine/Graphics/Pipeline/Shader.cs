using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Vulkan;

namespace SharpGame
{
    using System.Collections;
    using static Builder;

    [DataContract]
    public class Shader : Resource, IEnumerable<Pass>
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<ShaderParameter> Properties { get; set; }

        [DataMember]
        public List<Pass> Passes { get; set; } = new List<Pass>();

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
            Passes.Add(pass);
        }

        [IgnoreDataMember]
        public Pass Main
        {
            get
            {
                return GetPass(0);
            }

            set
            {
                if(value.passID != 0)
                {
                    Log.Warn("Not a main pass.");
                    return;
                }

                for (int i = 0; i < Passes.Count; i++)
                {
                    if(Passes[i].passID == 0)
                    {
                        Passes[i].Dispose();
                        Passes[i] = value;
                    }
                }

                Add(value);
            }
        }

        public Pass GetPass(ulong id)
        {
            foreach (var pass in Passes)
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
            foreach(var pass in Passes)
            {
                if(pass.Name == name)
                {
                    return pass;
                }
            }

            return null;
        }

        protected override void OnBuild()
        {
            foreach (var pass in Passes)
            {
                pass.Build();
            }
        }

        protected override void Destroy()
        {
            foreach (var pass in Passes)
            {
                pass.Dispose();
            }

            Passes.Clear();

            base.Destroy();
        }

        public IEnumerator<Pass> GetEnumerator()
        {
            return ((IEnumerable<Pass>)Passes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Pass>)Passes).GetEnumerator();
        }
    }

    

}
