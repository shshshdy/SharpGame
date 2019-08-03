using Delegates;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public interface ISerializer
    {
        bool IsReading { get; }
        bool IsWriting { get; }
        bool StartDocument(String fileName);
		void EndDocument();
        bool StartObject(uint size);
        void EndObject();
        object CreateObject();
        bool StartProperty(String key);
        void EndProperty();
        bool StartArray(ref uint size);
        void SetElement(uint index);
        void EndArray();
        void VisitProperty<T>(string propertyName, ref T value);
        void VisitProperty(string propertyName, ref object value);
    }


}
