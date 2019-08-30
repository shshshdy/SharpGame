using SharpGame.Sdl2;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;
using static SharpGame.Sdl2.Sdl2Native;
using System.Collections.Generic;

namespace SharpGame
{
    public struct SwapChainBuffer
    {
        public Image Image;
        public ImageView View;
    }

    public unsafe class Swapchain
    {
        public VkSurfaceKHR Surface { get; private set; }
        public uint QueueNodeIndex { get; private set; } = uint.MaxValue;
        public Format ColorFormat { get; private set; }
        public VkColorSpaceKHR ColorSpace { get; private set; }
        public VkSwapchainKHR swapchain;
        public int ImageCount { get; private set; }
        public NativeList<VkImage> Images { get; set; } = new NativeList<VkImage>();
        public SwapChainBuffer[] Buffers { get; set; }
        
        public unsafe Swapchain(IntPtr sdlWindow)
        {
            SDL_version version;
            SDL_GetVersion(&version);
            SDL_SysWMinfo sysWmInfo;
            sysWmInfo.version = version;
            int result = SDL_GetWMWindowInfo(sdlWindow, &sysWmInfo);
            if (result == 0)
            {
                throw new InvalidOperationException("Couldn't retrieve SDL window info.");
            }

            VkResult err;
            if (sysWmInfo.subsystem == SysWMType.Windows)
            {
                Win32WindowInfo win32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
                // Create the os-specific Surface
                VkWin32SurfaceCreateInfoKHR surfaceCreateInfo = VkWin32SurfaceCreateInfoKHR.New();
                var processHandle = Process.GetCurrentProcess().SafeHandle.DangerousGetHandle();
                surfaceCreateInfo.hinstance = processHandle;
                surfaceCreateInfo.hwnd = win32Info.Sdl2Window;
                VkSurfaceKHR surface;
                err = vkCreateWin32SurfaceKHR(Device.VkInstance, &surfaceCreateInfo, null, &surface);
                Surface = surface;
            }
            else if (sysWmInfo.subsystem == SysWMType.X11)
            {
                X11WindowInfo x11Info = Unsafe.Read<X11WindowInfo>(&sysWmInfo.info);
                VkXlibSurfaceCreateInfoKHR surfaceCreateInfo = VkXlibSurfaceCreateInfoKHR.New();
                surfaceCreateInfo.dpy = (Vulkan.Xlib.Display*)x11Info.display;
                surfaceCreateInfo.window = new Vulkan.Xlib.Window { Value = x11Info.Sdl2Window };
                VkSurfaceKHR surface;
                err = vkCreateXlibSurfaceKHR(Device.VkInstance, &surfaceCreateInfo, null, out surface);
                Surface = surface;
            }
            else
            {
                throw new NotImplementedException($"SDL backend not implemented: {sysWmInfo.subsystem}.");
            }

            // Get available queue family properties
            uint queueCount;
            vkGetPhysicalDeviceQueueFamilyProperties(Device.PhysicalDevice, &queueCount, null);
            Debug.Assert(queueCount >= 1);

            using (NativeList<VkQueueFamilyProperties> queueProps = new NativeList<VkQueueFamilyProperties>(queueCount))
            {
                vkGetPhysicalDeviceQueueFamilyProperties(Device.PhysicalDevice, &queueCount, (VkQueueFamilyProperties*)queueProps.Data.ToPointer());
                queueProps.Count = queueCount;

                // Iterate over each queue to learn whether it supports presenting:
                // Find a queue with present support
                // Will be used to present the swap chain Images to the windowing system
                VkBool32* supportsPresent = stackalloc VkBool32[(int)queueCount];

                for (uint i = 0; i < queueCount; i++)
                {
                    vkGetPhysicalDeviceSurfaceSupportKHR(Device.PhysicalDevice, i, Surface, &supportsPresent[i]);
                }

                // Search for a graphics and a present queue in the array of queue
                // families, try to find one that supports both
                uint graphicsQueueNodeIndex = uint.MaxValue;
                uint presentQueueNodeIndex = uint.MaxValue;
                for (uint i = 0; i < queueCount; i++)
                {
                    if ((queueProps[i].queueFlags & VkQueueFlags.Graphics) != 0)
                    {
                        if (graphicsQueueNodeIndex == uint.MaxValue)
                        {
                            graphicsQueueNodeIndex = i;
                        }

                        if (supportsPresent[i] == True)
                        {
                            graphicsQueueNodeIndex = i;
                            presentQueueNodeIndex = i;
                            break;
                        }
                    }
                }

                if (presentQueueNodeIndex == uint.MaxValue)
                {
                    // If there's no queue that supports both present and graphics
                    // try to find a separate present queue
                    for (uint i = 0; i < queueCount; ++i)
                    {
                        if (supportsPresent[i] == True)
                        {
                            presentQueueNodeIndex = i;
                            break;
                        }
                    }
                }

                // Exit if either a graphics or a presenting queue hasn't been found
                if (graphicsQueueNodeIndex == uint.MaxValue || presentQueueNodeIndex == uint.MaxValue)
                {
                    throw new InvalidOperationException("Could not find a graphics and/or presenting queue!");
                }

                // todo : Add support for separate graphics and presenting queue
                if (graphicsQueueNodeIndex != presentQueueNodeIndex)
                {
                    throw new InvalidOperationException("Separate graphics and presenting queues are not supported yet!");
                }

                QueueNodeIndex = graphicsQueueNodeIndex;

                // Get list of supported Surface formats
                uint formatCount;
                err = vkGetPhysicalDeviceSurfaceFormatsKHR(Device.PhysicalDevice, Surface, &formatCount, null);
                Debug.Assert(err == VkResult.Success);
                Debug.Assert(formatCount > 0);

                using (NativeList<VkSurfaceFormatKHR> surfaceFormats = new NativeList<VkSurfaceFormatKHR>(formatCount))
                {
                    err = vkGetPhysicalDeviceSurfaceFormatsKHR(Device.PhysicalDevice, Surface, &formatCount, (VkSurfaceFormatKHR*)surfaceFormats.Data.ToPointer());
                    surfaceFormats.Count = formatCount;
                    Debug.Assert(err == VkResult.Success);

                    // If the Surface format list only includes one entry with VK_FORMAT_UNDEFINED,
                    // there is no preferered format, so we assume VK_FORMAT_B8G8R8A8_UNORM
                    if ((formatCount == 1) && (surfaceFormats[0].format == VkFormat.Undefined))
                    {
                        ColorFormat = Format.B8g8r8a8Unorm;
                        ColorSpace = surfaceFormats[0].colorSpace;
                    }
                    else
                    {
                        // iterate over the list of available Surface format and
                        // check for the presence of VK_FORMAT_B8G8R8A8_UNORM
                        bool found_B8G8R8A8_UNORM = false;
                        foreach (var surfaceFormat in surfaceFormats)
                        {
                            if (surfaceFormat.format == VkFormat.B8g8r8a8Unorm)
                            {
                                ColorFormat = (Format)surfaceFormat.format;
                                ColorSpace = surfaceFormat.colorSpace;
                                found_B8G8R8A8_UNORM = true;
                                break;
                            }
                        }

                        // in case VK_FORMAT_B8G8R8A8_UNORM is not available
                        // select the first available color format
                        if (!found_B8G8R8A8_UNORM)
                        {
                            ColorFormat = (Format)surfaceFormats[0].format;
                            ColorSpace = surfaceFormats[0].colorSpace;
                        }
                    }
                }
            }
        }

        /** 
        * Create the swapchain and get it's Images with given width and height
        * 
        * @param width Pointer to the width of the swapchain (may be adjusted to fit the requirements of the swapchain)
        * @param height Pointer to the height of the swapchain (may be adjusted to fit the requirements of the swapchain)
        * @param vsync (Optional) Can be used to force vsync'd rendering (by using VK_PRESENT_MODE_FIFO_KHR as presentation mode)
        */
        public void Create(uint* width, uint* height, bool vsync = false)
        {
            VkResult err;
            VkSwapchainKHR oldSwapchain = swapchain;

            // Get physical Device Surface properties and formats
            VkSurfaceCapabilitiesKHR surfCaps;
            err = vkGetPhysicalDeviceSurfaceCapabilitiesKHR(Device.PhysicalDevice, Surface, &surfCaps);
            Debug.Assert(err == VkResult.Success);

            // Get available present modes
            uint presentModeCount;
            err = vkGetPhysicalDeviceSurfacePresentModesKHR(Device.PhysicalDevice, Surface, &presentModeCount, null);
            Debug.Assert(err == VkResult.Success);
            Debug.Assert(presentModeCount > 0);

            using (NativeList<VkPresentModeKHR> presentModes = new NativeList<VkPresentModeKHR>(presentModeCount))
            {
                err = vkGetPhysicalDeviceSurfacePresentModesKHR(Device.PhysicalDevice, Surface, &presentModeCount, (VkPresentModeKHR*)presentModes.Data);
                Debug.Assert(err == VkResult.Success);
                presentModes.Count = presentModeCount;

                VkExtent2D swapchainExtent;
                // If width (and height) equals the special value 0xFFFFFFFF, the size of the Surface will be set by the swapchain
                if (surfCaps.currentExtent.width == unchecked((uint)-1))
                {
                    // If the Surface size is undefined, the size is set to
                    // the size of the Images requested.
                    swapchainExtent.width = *width;
                    swapchainExtent.height = *height;
                }
                else
                {
                    // If the Surface size is defined, the swap chain size must match
                    swapchainExtent = surfCaps.currentExtent;
                    *width = surfCaps.currentExtent.width;
                    *height = surfCaps.currentExtent.height;
                }


                // Select a present mode for the swapchain

                // The VK_PRESENT_MODE_FIFO_KHR mode must always be present as per spec
                // This mode waits for the vertical blank ("v-sync")
                VkPresentModeKHR swapchainPresentMode = VkPresentModeKHR.FifoKHR;

                // If v-sync is not requested, try to find a mailbox mode
                // It's the lowest latency non-tearing present mode available
                if (!vsync)
                {
                    for (uint i = 0; i < presentModeCount; i++)
                    {
                        if (presentModes[i] == VkPresentModeKHR.MailboxKHR)
                        {
                            swapchainPresentMode = VkPresentModeKHR.MailboxKHR;
                            break;
                        }
                        if ((swapchainPresentMode != VkPresentModeKHR.MailboxKHR) && (presentModes[i] == VkPresentModeKHR.ImmediateKHR))
                        {
                            swapchainPresentMode = VkPresentModeKHR.ImmediateKHR;
                        }
                    }
                }

                // Determine the number of Images
                uint desiredNumberOfSwapchainImages = 3;// surfCaps.minImageCount + 1;
                if ((surfCaps.maxImageCount > 0) && (desiredNumberOfSwapchainImages > surfCaps.maxImageCount))
                {
                    desiredNumberOfSwapchainImages = surfCaps.maxImageCount;
                }

                // Find the transformation of the Surface
                VkSurfaceTransformFlagsKHR preTransform;
                if ((surfCaps.supportedTransforms & VkSurfaceTransformFlagsKHR.IdentityKHR) != 0)
                {
                    // We prefer a non-rotated transform
                    preTransform = VkSurfaceTransformFlagsKHR.IdentityKHR;
                }
                else
                {
                    preTransform = surfCaps.currentTransform;
                }

                VkSwapchainCreateInfoKHR swapchainCI = VkSwapchainCreateInfoKHR.New();
                swapchainCI.pNext = null;
                swapchainCI.surface = Surface;
                swapchainCI.minImageCount = desiredNumberOfSwapchainImages;
                swapchainCI.imageFormat = (VkFormat)ColorFormat;
                swapchainCI.imageColorSpace = ColorSpace;
                swapchainCI.imageExtent = new VkExtent2D() { width = swapchainExtent.width, height = swapchainExtent.height };
                swapchainCI.imageUsage = VkImageUsageFlags.ColorAttachment;
                swapchainCI.preTransform = preTransform;
                swapchainCI.imageArrayLayers = 1;
                swapchainCI.imageSharingMode = VkSharingMode.Exclusive;
                swapchainCI.queueFamilyIndexCount = 0;
                swapchainCI.pQueueFamilyIndices = null;
                swapchainCI.presentMode = swapchainPresentMode;
                swapchainCI.oldSwapchain = oldSwapchain;
                // Setting clipped to VK_TRUE allows the implementation to discard rendering outside of the Surface area
                swapchainCI.clipped = True;
                swapchainCI.compositeAlpha = VkCompositeAlphaFlagsKHR.OpaqueKHR;

                // Set additional usage flag for blitting from the swapchain Images if supported
                VkFormatProperties formatProps;
                Device.GetPhysicalDeviceFormatProperties((VkFormat)ColorFormat, out formatProps);
                if ((formatProps.optimalTilingFeatures & VkFormatFeatureFlags.BlitDst) != 0)
                {
                    swapchainCI.imageUsage |= VkImageUsageFlags.TransferSrc;
                }

                swapchain = Device.CreateSwapchainKHR(ref swapchainCI);
                
                // If an existing swap chain is re-created, destroy the old swap chain
                // This also cleans up all the presentable Images
                if (oldSwapchain.Handle != 0)
                {
                    for (uint i = 0; i < ImageCount; i++)
                    {
                        Buffers[i].View.Dispose();
                    }

                    Device.DestroySwapchainKHR(oldSwapchain);
                }

                uint imageCount;
                Device.GetSwapchainImagesKHR(swapchain, &imageCount, null);

                ImageCount = (int)imageCount;
                // Get the swap chain Images
                Images.Resize(imageCount);

                Device.GetSwapchainImagesKHR(swapchain, &imageCount, (VkImage*)Images.Data.ToPointer());

                Images.Count = imageCount;                
                // Get the swap chain Buffers containing the image and imageview
                Buffers = new SwapChainBuffer[(int)imageCount];
                for (int i = 0; i < imageCount; i++)
                {                  
                    var img = new Image(Images[i]);
                    Buffers[i].Image = img;
                    Buffers[i].View = ImageView.Create(img, ImageViewType.Image2D, ColorFormat, ImageAspectFlags.Color, 0, 1);
                }
            }
        }

        public void AcquireNextImage(Semaphore presentCompleteSemaphore, ref uint imageIndex)
        {
            // By setting timeout to UINT64_MAX we will always wait until the next image has been acquired or an actual error is thrown
            // With that we don't have to handle VK_NOT_READY
            Device.AcquireNextImageKHR(swapchain, ulong.MaxValue, presentCompleteSemaphore.native, new VkFence(), ref imageIndex);
        }

        public void QueuePresent(Queue queue, uint imageIndex, Semaphore waitSemaphore = null)
        {
            VkPresentInfoKHR presentInfo = VkPresentInfoKHR.New();
            presentInfo.pNext = null;
            presentInfo.swapchainCount = 1;
            var sc = swapchain;
            presentInfo.pSwapchains = &sc;
            presentInfo.pImageIndices = &imageIndex;
            // Check if a wait semaphore has been specified to wait for before presenting the image
            if (waitSemaphore != null)
            {
                presentInfo.pWaitSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphore.native);
                presentInfo.waitSemaphoreCount = 1;
            }

            VulkanUtil.CheckResult(vkQueuePresentKHR(queue.native, &presentInfo));
        }
    }

}
