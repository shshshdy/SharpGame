using MessagePack;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    [DataContract]
    public class Serializable : System.Object, IMessagePackSerializationCallbackReceiver
    {
        public bool IsResource { get; set; }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
           
        }
        
    }
}
