using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    public enum LightType
    {
        Directional = 0,
        Point,
        Spot
    };

    [DataContract]
    public class Light : Component
    {  
        public LightType LightType { get; set; }

        [DataMember(Order = 1)]
        public Color Color { get; set; }


        public virtual void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
        }
    }
}
