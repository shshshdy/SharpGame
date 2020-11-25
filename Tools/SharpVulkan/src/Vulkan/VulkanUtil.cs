// Copyright (c) BobbyBao and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SharpGame
{
    public static unsafe class VulkanUtil
    {
        [DebuggerHidden]
        [DebuggerStepThrough]
        public static void CheckResult(this VkResult result)
        {
            if (result != VkResult.Success)
            {
                if (VkResult.ErrorValidationFailedEXT != result)
                    throw new VkException(result);
            }
        }

        public static string GetExtensionName(this VkExtensionProperties properties)
        {
            return Interop.GetString(properties.extensionName);
        }

        public static string GetName(this VkLayerProperties properties)
        {
            return Interop.GetString(properties.layerName);
        }

        public static string GetDeviceName(this VkPhysicalDeviceProperties properties)
        {
            return Interop.GetString(properties.deviceName);
        }

        public static string GetDescription(this VkLayerProperties properties)
        {
            return Interop.GetString(properties.description);
        }

        public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
        {
            return (&memoryProperties.memoryTypes_0)[index];
        }

        public static uint IndexOf(this VkPhysicalDeviceMemoryProperties memoryProperties, int memoryTypeBits, VkMemoryPropertyFlags properties)
        {
            uint count = memoryProperties.memoryTypeCount;
            for (uint i = 0; i < count; i++)
            {
                if ((memoryTypeBits & 1) == 1 &&
                    ((&memoryProperties.memoryTypes_0)[i].propertyFlags & properties) == properties)
                {
                    return i;
                }
                memoryTypeBits >>= 1;
            }

            return uint.MaxValue;
        }

        public static IntPtr GetProcAddr(this VkInstance instance, string name)
        {
            int byteCount = Interop.GetMaxByteCount(name);
            var dstPtr = stackalloc byte[byteCount];
            Interop.StringToPointer(name, dstPtr, byteCount);
            var addr = Vulkan.vkGetInstanceProcAddr(instance, dstPtr);
            return addr;
        }

        public unsafe static TDelegate GetProc<TDelegate>(this VkInstance instance, string name) where TDelegate : class
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            IntPtr ptr = GetProcAddr(instance, name);

            return ptr != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr) : default;

        }

        public static IntPtr GetProcAddr(this VkDevice device, string name)
        {
            int byteCount = Interop.GetMaxByteCount(name);
            var dstPtr = stackalloc byte[byteCount];
            Interop.StringToPointer(name, dstPtr, byteCount);
            var addr = Vulkan.vkGetDeviceProcAddr(device, dstPtr);
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
