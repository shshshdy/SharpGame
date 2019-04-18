using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public abstract class DeviceObject : Object
    {
        static List<DeviceObject> deviceObjects = new List<DeviceObject>();

        public static void RecreateAll()
        {
            foreach(var obj in deviceObjects)
            {
                obj.Recreate();
            }
        }          
        
        public static void DisposeAll()
        {
            foreach (var obj in deviceObjects)
            {
                obj.Dispose();
            }
        }

        public DeviceObject()
        {
            deviceObjects.Add(this);
        }

        protected abstract void Recreate();

    }
}
