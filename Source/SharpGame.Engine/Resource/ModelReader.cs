using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ModelReader : ResourceReader<Model>
    {
        protected override bool OnLoad(Model model, File stream)
        {
            String fileID = stream.ReadFileID();
            if(fileID != "UMDL" && fileID != "UMD2")
            {
                Log.Error("Invalid model file");
                return false;
            }

            return true;
        }
    }
}
