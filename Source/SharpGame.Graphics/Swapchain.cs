﻿using SharpGame.Sdl2;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace SharpGame
{
    using static Vulkan;
    using static SharpGame.Sdl2.Sdl2Native;

    public class Swapchain
    {
        public VkSurfaceKHR Surface { get; private set; }
        public uint QueueNodeIndex { get; private set; } = uint.MaxValue;
        public VkExtent3D extent;
        public VkFormat ColorFormat { get; private set; }
        public VkColorSpaceKHR ColorSpace { get; private set; }
        public VkSwapchainKHR swapchain;
        public uint ImageCount => VkImages.Count;
        public Vector<VkImage> VkImages { get; private set; } = new Vector<VkImage>();
        public Image[] Images { get; private set; }
        public ImageView[] ImageViews { get; private set; }

        public const uint IMAGE_COUNT = 3;

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

            VkSurfaceKHR surface;

            if (SDL_Vulkan_CreateSurface(sdlWindow, Device.VkInstance.Handle, (IntPtr)(&surface)) == 0)
            {
                var error = UTF8String.FromPointer(SDL_GetError());
                Log.Error("create surface failed." + error);
            };

            Surface = surface;

            VkResult err;

            /*
            if (sysWmInfo.subsystem == SysWMType.Windows)
            {
                Win32WindowInfo win32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
                // Create the os-specific Surface
                VkWin32SurfaceCreateInfoKHR surfaceCreateInfo = VkWin32SurfaceCreateInfoKHR.New();
                var processHandle = Process.GetCurrentProcess().SafeHandle.DangerousGetHandle();
                surfaceCreateInfo.hinstance = processHandle;
                surfaceCreateInfo.hwnd = win32Info.Sdl2Window;
                err = vkCreateWin32SurfaceKHR(Device.VkInstance, &surfaceCreateInfo, null, &surface);
                Surface = surface;
            }
            else if (sysWmInfo.subsystem == SysWMType.X11)
            {
                X11WindowInfo x11Info = Unsafe.Read<X11WindowInfo>(&sysWmInfo.info);
                VkXlibSurfaceCreateInfoKHR surfaceCreateInfo = VkXlibSurfaceCreateInfoKHR.New();
                surfaceCreateInfo.dpy = (Vulkan.Xlib.Display*)x11Info.display;
                surfaceCreateInfo.window = new Vulkan.Xlib.Window { Value = x11Info.Sdl2Window };
                err = vkCreateXlibSurfaceKHR(Device.VkInstance, &surfaceCreateInfo, null, out surface);
                Surface = surface;
            }
            else
            {
                throw new NotImplementedException($"SDL backend not implemented: {sysWmInfo.subsystem}.");
            }*/

            var queueProps = vkGetPhysicalDeviceQueueFamilyProperties(Device.PhysicalDevice);

            // Iterate over each queue to learn whether it supports presenting:
            // Find a queue with present support
            // Will be used to present the swap chain Images to the windowing system
            VkBool32* supportsPresent = stackalloc VkBool32[(int)queueProps.Length];

            for (int i = 0; i < queueProps.Length; i++)
            {
                vkGetPhysicalDeviceSurfaceSupportKHR(Device.PhysicalDevice, (uint)i, Surface, out supportsPresent[i]);
            }

            // Search for a graphics and a present queue in the array of queue
            // families, try to find one that supports both
            uint graphicsQueueNodeIndex = uint.MaxValue;
            uint presentQueueNodeIndex = uint.MaxValue;
            for (int i = 0; i < queueProps.Length; i++)
            {
                if ((queueProps[i].queueFlags & VkQueueFlags.Graphics) != 0)
                {
                    if (graphicsQueueNodeIndex == uint.MaxValue)
                    {
                        graphicsQueueNodeIndex = (uint)i;
                    }

                    if (supportsPresent[i] == true)
                    {
                        graphicsQueueNodeIndex = (uint)i;
                        presentQueueNodeIndex = (uint)i;
                        break;
                    }
                }
            }

            if (presentQueueNodeIndex == uint.MaxValue)
            {
                // If there's no queue that supports both present and graphics
                // try to find a separate present queue
                for (uint i = 0; i < queueProps.Length; ++i)
                {
                    if (supportsPresent[i] == true)
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

            using Vector<VkSurfaceFormatKHR> surfaceFormats = new Vector<VkSurfaceFormatKHR>(formatCount);
            err = vkGetPhysicalDeviceSurfaceFormatsKHR(Device.PhysicalDevice, Surface, &formatCount, surfaceFormats.DataPtr);
            surfaceFormats.Count = formatCount;
            Debug.Assert(err == VkResult.Success);

            // If the Surface format list only includes one entry with VK_FORMAT_UNDEFINED,
            // there is no preferered format, so we assume VK_FORMAT_B8G8R8A8_UNORM
            if ((formatCount == 1) && (surfaceFormats[0].format == VkFormat.Undefined))
            {
                ColorFormat = VkFormat.B8G8R8A8UNorm;
                ColorSpace = surfaceFormats[0].colorSpace;
            }
            else
            {
                // iterate over the list of available Surface format and
                // check for the presence of VK_FORMAT_B8G8R8A8_UNORM
                bool found_B8G8R8A8_UNORM = false;
                foreach (var surfaceFormat in surfaceFormats)
                {
                    if (surfaceFormat.format == VkFormat.B8G8R8A8UNorm)
                    {
                        ColorFormat = surfaceFormat.format;
                        ColorSpace = surfaceFormat.colorSpace;
                        found_B8G8R8A8_UNORM = true;
                        break;
                    }
                }

                // in case VK_FORMAT_B8G8R8A8_UNORM is not available
                // select the first available color format
                if (!found_B8G8R8A8_UNORM)
                {
                    ColorFormat = surfaceFormats[0].format;
                    ColorSpace = surfaceFormats[0].colorSpace;
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
        public unsafe void Create(ref uint width, ref uint height, bool vsync = false)
        {
            VkResult err;
            VkSwapchainKHR oldSwapchain = swapchain;

            // Get physical Device Surface properties and formats
            VkSurfaceCapabilitiesKHR surfCaps;
            err = vkGetPhysicalDeviceSurfaceCapabilitiesKHR(Device.PhysicalDevice, Surface, out surfCaps);
            Debug.Assert(err == VkResult.Success);

            // Get available present modes
            uint presentModeCount;
            err = vkGetPhysicalDeviceSurfacePresentModesKHR(Device.PhysicalDevice, Surface, &presentModeCount, null);
            Debug.Assert(err == VkResult.Success);
            Debug.Assert(presentModeCount > 0);

            using (Vector<VkPresentModeKHR> presentModes = new Vector<VkPresentModeKHR>(presentModeCount))
            {
                err = vkGetPhysicalDeviceSurfacePresentModesKHR(Device.PhysicalDevice, Surface, &presentModeCount, (VkPresentModeKHR*)presentModes.Data);
                Debug.Assert(err == VkResult.Success);
                presentModes.Count = presentModeCount;

                // If width (and height) equals the special value 0xFFFFFFFF, the size of the Surface will be set by the swapchain
                if (surfCaps.currentExtent.width != unchecked((uint)-1))
                {
                    width = surfCaps.currentExtent.width;
                    height = surfCaps.currentExtent.height;
                }

                extent = new VkExtent3D(width, height, 1);

                // Select a present mode for the swapchain

                // The VK_PRESENT_MODE_FIFO_KHR mode must always be present as per spec
                // This mode waits for the vertical blank ("v-sync")
                VkPresentModeKHR swapchainPresentMode = VkPresentModeKHR.Fifo;

                // If v-sync is not requested, try to find a mailbox mode
                // It's the lowest latency non-tearing present mode available
                if (!vsync)
                {
                    for (uint i = 0; i < presentModeCount; i++)
                    {
                        if (presentModes[i] == VkPresentModeKHR.Mailbox)
                        {
                            swapchainPresentMode = VkPresentModeKHR.Mailbox;
                            break;
                        }
                        if ((swapchainPresentMode != VkPresentModeKHR.Mailbox) && (presentModes[i] == VkPresentModeKHR.Immediate))
                        {
                            swapchainPresentMode = VkPresentModeKHR.Immediate;
                        }
                    }
                }

                // Determine the number of Images
                uint desiredNumberOfSwapchainImages = IMAGE_COUNT;// surfCaps.minImageCount + 1;
                if ((surfCaps.maxImageCount > 0) && (desiredNumberOfSwapchainImages > surfCaps.maxImageCount))
                {
                    Debug.Assert(false);
                    desiredNumberOfSwapchainImages = surfCaps.maxImageCount;
                }

                // Find the transformation of the Surface
                VkSurfaceTransformFlagsKHR preTransform;
                if ((surfCaps.supportedTransforms & VkSurfaceTransformFlagsKHR.Identity) != 0)
                {
                    // We prefer a non-rotated transform
                    preTransform = VkSurfaceTransformFlagsKHR.Identity;
                }
                else
                {
                    preTransform = surfCaps.currentTransform;
                }

                VkSwapchainCreateInfoKHR swapchainCI = new VkSwapchainCreateInfoKHR
                {
                    sType = VkStructureType.SwapchainCreateInfoKHR,
                    pNext = null,
                    surface = Surface,
                    minImageCount = desiredNumberOfSwapchainImages,
                    imageFormat = ColorFormat,
                    imageColorSpace = ColorSpace,
                    imageExtent = new VkExtent2D(extent.width, extent.height),
                    imageUsage = VkImageUsageFlags.ColorAttachment,
                    preTransform = preTransform,
                    imageArrayLayers = 1,
                    imageSharingMode = VkSharingMode.Exclusive,
                    queueFamilyIndexCount = 0,
                    pQueueFamilyIndices = null,
                    presentMode = swapchainPresentMode,
                    oldSwapchain = oldSwapchain,
                    // Setting clipped to VK_TRUE allows the implementation to discard rendering outside of the Surface area
                    clipped = true,
                    compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque
                };

                // Set additional usage flag for blitting from the swapchain Images if supported
                Device.GetPhysicalDeviceFormatProperties(ColorFormat, out VkFormatProperties formatProps);

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
                        ImageViews[i].Dispose();
                    }

                    Device.DestroySwapchainKHR(oldSwapchain);
                }

                var vkImages = Vulkan.vkGetSwapchainImagesKHR(Device.Handle, swapchain);
                VkImages.Clear();
                Images = new Image[vkImages.Length];
                ImageViews = new ImageView[vkImages.Length];
                for (int i = 0; i < vkImages.Length; i++)
                {
                    Images[i] = new Image(vkImages[i])
                    {
                        imageType = VkImageType.Image2D,
                        extent = extent
                    };
                    ImageViews[i] = ImageView.Create(Images[i], VkImageViewType.Image2D, ColorFormat, VkImageAspectFlags.Color, 0, 1);
                }

                // Get the swap chain Images
                VkImages.Add(vkImages);
            }
        }

        public bool AcquireNextImage(VkSemaphore presentCompleteSemaphore, out int imageIndex)
        {
            VkResult res = Device.AcquireNextImageKHR(swapchain, ulong.MaxValue, presentCompleteSemaphore, new VkFence(), out uint nextImageIndex);

            if (res == VkResult.ErrorOutOfDateKHR)
            {
                Log.Error(res.ToString());
                //uint w = 0, h = 0;                
                //Create(ref w, ref h, false);
            }
            else if (res == VkResult.SuboptimalKHR)
            {
                Log.Info(res.ToString());
            }
            else if (res != VkResult.Success)
            {
                Log.Info(res.ToString());
                imageIndex = 0;
                return false;
            }

            imageIndex = (int)nextImageIndex;
            return true;

        }

        public unsafe void QueuePresent(VkQueue queue, uint imageIndex, VkSemaphore waitSemaphore = default)
        {
            var sc = swapchain;
            var presentInfo = new VkPresentInfoKHR
            {
                sType = VkStructureType.PresentInfoKHR,
                pNext = null,
                swapchainCount = 1,
                pSwapchains = &sc,
                pImageIndices = &imageIndex
            };

            // Check if a wait semaphore has been specified to wait for before presenting the image
            if (waitSemaphore != VkSampler.Null)
            {
                presentInfo.pWaitSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphore);
                presentInfo.waitSemaphoreCount = 1;
            }

            VulkanUtil.CheckResult(vkQueuePresentKHR(queue, &presentInfo));
        }
    }

}
