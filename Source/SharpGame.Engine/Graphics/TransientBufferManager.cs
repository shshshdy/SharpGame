using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct TransientBuffer
    {
        public DeviceBuffer buffer;
        public int offset;
        public int size;
    }

    public class TransientBufferManager
    {
        DeviceBuffer[] buffer = new DeviceBuffer[2];
        public TransientBufferManager(int size)
        {
            buffer[0] = new DeviceBuffer();
            buffer[1] = new DeviceBuffer();
        }
    }
}
