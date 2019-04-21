using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public abstract class GPUObject : Object
    {
        static List<GPUObject> GPUObjects_ = new List<GPUObject>();

        public static void RecreateAll()
        {
            foreach(var obj in GPUObjects_)
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

        public GPUObject()
        {
            GPUObjects_.Add(this);
        }

        public override void Dispose()
        {
            GPUObjects_.Remove(this);
        }

        protected abstract void Recreate();

    }
}
