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
        
        public DeviceObject()
        {
            deviceObjects.Add(this);
        }

        public override void Dispose()
        {
            deviceObjects.Remove(this);
        }

        protected abstract void Recreate();

    }
}
