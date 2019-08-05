using Hocon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{
    public struct Config
    {
        public static Hocon.HoconRoot Parse(Stream stream, HoconIncludeCallbackAsync includeCallback = null)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                var text = sr.ReadToEnd();
                return Hocon.Parser.Parse(text, includeCallback);

            }
        }


    }
}
