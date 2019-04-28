using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    enum LightType
    {
        LIGHT_DIRECTIONAL = 0,
        LIGHT_SPOT,
        LIGHT_POINT
    };

    [DataContract]
    public class Light : Component
    {  
        /// Light type.
        LightType lightType_;
        /// Color.
        Color color_;

        [DataMember(Order = 1)]
        public Color Color
        {
            get
            {
                return color_;
            }

            set
            {
                color_ = value;
            }
        }

        public virtual void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
        }
    }
}
