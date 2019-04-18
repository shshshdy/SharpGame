using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    interface IDeviceObject : IDisposable
    {
        void Recreate();
    }
}
