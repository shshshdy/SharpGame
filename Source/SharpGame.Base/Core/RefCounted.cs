using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace SharpGame
{
    public class RefCounted : IDisposable
    {
        private int counter = 1;

        /// <summary>
        /// Has the component been disposed or not yet.
        /// </summary>
        [System.Runtime.Serialization.IgnoreDataMember]
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Release();
            }
        }

        /// <summary>
        /// Disposes of object resources.
        /// </summary>
        protected virtual void Destroy()
        {
        }

        public int RefCount => counter;

        /// <inheritdoc/>
        public int AddRef()
        {
            var newCounter = Interlocked.Increment(ref counter);
            if (newCounter <= 1) throw new InvalidOperationException("AddReferenceError");
            return newCounter;
        }

        /// <inheritdoc/>
        public int Release()
        {
            var newCounter = Interlocked.Decrement(ref counter);
            if (newCounter == 0)
            {
                Destroy();
                IsDisposed = true;
            }
            else if (newCounter < 0)
            {
                throw new InvalidOperationException("ReleaseReferenceError");
            }
            return newCounter;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(RefCounted obj)
        {
            return obj != null && !obj.IsDisposed;
        }
    }
}
