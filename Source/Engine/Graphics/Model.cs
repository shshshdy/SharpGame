using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{
    public class Model : Resource
    {
        public List<Geometry> geometries_;


        public async override void Load(Stream stream)
        {
        }

        protected override void OnBuild()
        {

        }
    }
}
