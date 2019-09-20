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
    public class Light : Drawable
    {  
        public LightType LightType { get; set; }

        [DataMember(Order = 1)]
        public Color4 Color { get; set; }
        public Color4 EffectiveColor
{
            get
            {
                return new Color4((Color3)Color * brightness_, 1.0f);
            }

}
        /// Specular intensity.
        float specularIntensity_;
        /// Brightness multiplier.
        float brightness_ = 1.0f;

        public float Range { get; set; }
        
        /// Spotlight field of view.
        float fov_;
        /// Spotlight aspect ratio.
        float aspectRatio_;
        /// Fade start distance.
        float fadeDistance_;

        public Light()
        {
            drawableFlags_ = DRAWABLE_LIGHT;
        }

        public override void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
            Color color = (Color)EffectiveColor;

            if (debug && IsEnabledEffective())
            {
                switch (LightType)
                {
                    case LightType.Directional:
                        {
                            vec3 start = node_.WorldPosition;
                            vec3 end = start + node_.WorldDirection * 10.0f;
                            for (int i = -1; i < 2; ++i)
                            {
                                for (int j = -1; j < 2; ++j)
                                {
                                    vec3 offset = vec3.Up * (5.0f * i) + vec3.Right * (5.0f * j);
                                    debug.AddSphere(new BoundingSphere(start + offset, 0.1f), color, depthTest);
                                    debug.AddLine(start + offset, end + offset, color, depthTest);
                                }
                            }
                        }
                        break;

                    case LightType.Spot:
                        //debug->AddFrustum(GetFrustum(), color, depthTest);
                        break;

                    case LightType.Point:
                        debug.AddSphere(new BoundingSphere(node_.WorldPosition, Range), color, depthTest);
                        break;
                }
            }
        }
    }
}
