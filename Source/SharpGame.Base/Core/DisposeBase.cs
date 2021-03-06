﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class DisposeBase : IDisposable
    {
        private bool disposed;

        [System.Runtime.Serialization.IgnoreDataMember]
        public bool IsDisposed => disposed;

        public void Dispose()
        {
            Dispose(true);
            // Tell the garbage collector not to call the finalizer
            // since all the cleanup will already be done.
            GC.SuppressFinalize(this);
        }

        // If disposing is true, it was called explicitly and we should dispose managed objects.
        // If disposing is false, it was called by the finalizer and managed objects should not be disposed.
        protected virtual void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                }

                Destroy(disposing);

                disposed = true;
            }
        }

        protected virtual void Destroy(bool disposing)
        {
        }

        ~DisposeBase()
        {
            Dispose(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(DisposeBase obj)
        {
            return obj != null && !obj.disposed;
        }
    }

    public class HandleBase<T> : DisposeBase where T : IDisposable
    {
        protected T handle;

        public static implicit operator T(HandleBase<T> cmd) => cmd.handle;

        protected override void Destroy(bool disposing)
        {
            handle?.Dispose();
        }
    }
}
