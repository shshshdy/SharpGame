using System;
using System.Runtime.InteropServices;
using Vulkan;

namespace SharpGame
{
    using static VulkanNative;

    public unsafe static class VulkanUtil
    {
        public static void CheckResult(VkResult result)
        {
            if (result != VkResult.Success)
            {
                if(VkResult.ErrorValidationFailedEXT != result)
                {
                    Log.Error(result.ToString());
                    throw new InvalidOperationException("Call failed.");

                }
            }
        }

        public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
        {
            return (&memoryProperties.memoryTypes_0)[index];
        }

        public static IntPtr GetProcAddr(this VkInstance instance, string name)
        {
            int byteCount = UTF8String.GetMaxByteCount(name);
            var dstPtr = stackalloc byte[byteCount];
            UTF8String.ToPointer(name, dstPtr, byteCount);
            var addr = VulkanNative.vkGetInstanceProcAddr(instance, dstPtr);
            return addr;
        }

        public unsafe static TDelegate GetProc<TDelegate>(this VkInstance instance, string name) where TDelegate : class
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            IntPtr ptr = GetProcAddr(instance, name);
            TDelegate proc = ptr != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr) : null;

            return proc;
        }

        public static IntPtr GetProcAddr(this VkDevice device, string name)
        {
            int byteCount = UTF8String.GetMaxByteCount(name);
            var dstPtr = stackalloc byte[byteCount];
            UTF8String.ToPointer(name, dstPtr, byteCount);
            var addr = VulkanNative.vkGetDeviceProcAddr(device, dstPtr);
            return addr;
        }

        public unsafe static TDelegate GetProc<TDelegate>(this VkDevice device, string name) where TDelegate : class
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            IntPtr ptr = GetProcAddr(device, name);
            TDelegate proc = ptr != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr) : null;
            return proc;
        }

    }
}
