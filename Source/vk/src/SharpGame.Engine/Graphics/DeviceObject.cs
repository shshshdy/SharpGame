using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class DeviceObject<T> : DisposeBase where T : IEquatable<T>
    {
        internal T handle;

        protected override void Destroy()
        {
          //  Device.Destroy(handle);
        }

    }
}
