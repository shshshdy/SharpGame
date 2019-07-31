using Hocon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{
    public class HoconSerializer
    {
        public static T Deserialize<T>(Stream stream) where T : new()
        {
            T obj = new T();

            MetaInfo metaInfo = MetaInfo.GetMetaInfo(typeof(T));
            Hocon.HoconValue value = Parse(stream).Value;

            Load(obj, metaInfo, value);

            return obj;
        }

        public static Hocon.HoconRoot Parse(Stream stream, HoconIncludeCallbackAsync includeCallback = null)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                var text = sr.ReadToEnd();
                return Hocon.Parser.Parse(text, includeCallback);

            }
        }

        static void Load(object obj, MetaInfo metaInfo, Hocon.HoconValue value)
        {
            //metaInfo.get
        }
    }
}
