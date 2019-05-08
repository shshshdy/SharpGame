using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public abstract class GPUObject : Object
    {
        public Graphics Graphics => Get<Graphics>();

        public GPUObject()
        {
            GPUObjects_.Add(this);
        }

        protected override void Destroy()
        {
            GPUObjects_.Remove(this);
        }

        protected abstract void Recreate();

        static List<GPUObject> GPUObjects_ = new List<GPUObject>();

        public static void RecreateAll()
        {
            foreach (var obj in GPUObjects_)
            {
                obj.Recreate();
            }
        }

        public static void DisposeAll()
        {
            while (GPUObjects_.Count > 0)
            {
                var obj = GPUObjects_[0];
                obj.Dispose();
            }
        }

    }
}
