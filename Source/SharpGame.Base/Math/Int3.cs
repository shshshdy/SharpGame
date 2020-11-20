// Copyright (c) 2018-2022 SharpGame
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Int3 : IEquatable<Int3>
    {
        public int x;

        public int y;

        public int z;

        public static readonly Int3 Zero = new Int3(0, 0, 0);

        public Int3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public int X => x;
        public int Y => y;
        public int Z => z;

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(ref Int3 other)
        {
            return other.x == x && other.y == y && other.z == z;
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(Int3 other)
        {
            return Equals(ref other);
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Int3))
                return false;

            var strongValue = (Int3)obj;
            return Equals(ref strongValue);
        }


        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(Int3 left, Int3 right)
        {
            return left.Equals(ref right);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(Int3 left, Int3 right)
        {
            return !left.Equals(ref right);
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", x, y, z);
        }

        public override int GetHashCode()
        {
            int hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        public static explicit operator Int3(vec3 value)
        {
            return new Int3((int)value.x, (int)value.y, (int)value.z);
        }

        public static implicit operator vec3(Int3 value)
        {
            return new vec3(value.x, value.y, value.z);
        }
        
    }
}
