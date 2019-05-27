using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ObjModelReader : ResourceReader<Texture>
    {
        public ObjModelReader() : base(".obj")
        {
        }

        public override Resource Load(string name)
        {
            if (!MatchExtension(name))
            {
                return null;
            }

            File stream = FileSystem.GetFile(name);

            var objParser = new ObjParser();
            ObjFile objFile = objParser.Parse(stream);

            return null;
        }

    }


}
