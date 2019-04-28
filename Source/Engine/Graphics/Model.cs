using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{
    public class Model : Resource
    {
        public Graphics[] VertexBuffers { get; set; }
        public Graphics[] IndexBuffers { get; set; }

        public List<List<Geometry>> geometries_ = new List<List<Geometry>>();


        public async override void Load(Stream stream)
        {
        }

        protected override void OnBuild()
        {

        }
    }
}
