using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct GeometryInstance
    {
        public fixed byte type[4];
        public ushort mask;
        public ushort mtx_num;
        public Material material;
        public Geometry geometry;
        public IntPtr mtx;
        
    }


}
