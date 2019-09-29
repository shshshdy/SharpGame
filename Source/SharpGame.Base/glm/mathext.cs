using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public partial struct vec2
    {
        public static implicit operator System.Numerics.Vector2(vec2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }

        public static implicit operator vec2(System.Numerics.Vector2 value)
        {
            return new vec2(value.X, value.Y);
        }

    }

    public partial struct vec3
    {

        public static implicit operator System.Numerics.Vector3(vec3 value)
        {
            return new System.Numerics.Vector3(value.X, value.Y, value.Z);
        }

        public static implicit operator vec3(System.Numerics.Vector3 value)
        {
            return new vec3(value.X, value.Y, value.Z);
        }

    }

    public partial struct vec4
    {
        public static implicit operator System.Numerics.Vector4(vec4 value)
        {
            return new System.Numerics.Vector4(value.x, value.y, value.z, value.w);
        }

        public static implicit operator vec4(System.Numerics.Vector4 value)
        {
            return new vec4(value.X, value.Y, value.Z, value.W);
        }
    }

    public partial struct mat4
    {
    }

}
