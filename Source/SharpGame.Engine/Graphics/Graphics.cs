#define NEW_SYNC

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Vulkan;

using static Vulkan.VulkanNative;

namespace SharpGame
{
    public struct Semaphores
    {
        public VkSemaphore PresentComplete;
        public VkSemaphore RenderComplete;
    }

    public class Settings
    {
        public CString ApplicationName { get; set; }
        public bool Validation { get; set; } = true;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = false;
        public bool SingleLoop { get; set; }
    }

    public unsafe partial class Graphics : System<Graphics>
    {
        public Settings Settings { get; } = new Settings();
        internal Statistics stats = new Statistics();

        public VkInstance VkInstance { get; protected set; }
        public VkPhysicalDeviceFeatures enabledFeatures;
        public NativeList<IntPtr> EnabledExtensions { get; } = new NativeList<IntPtr>();

        public static VkDevice device { get; protected set; }
        public static VkQueue queue { get; protected set; }
        public Format ColorFormat => Swapchain.ColorFormat;
        public Format DepthFormat { get; protected set; }
        public Swapchain Swapchain { get; } = new Swapchain();

        public int Width { get; private set; }
        public int Height { get; private set; }

        public int ImageCount => Swapchain.ImageCount;

        private Framebuffer[] framebuffers;
        public Framebuffer[] Framebuffers => framebuffers;

        internal static DescriptorPoolManager DescriptorPoolManager { get; private set; }

        private CommandBufferPool primaryCmdPool;
        private CommandBufferPool[] secondaryCmdPool;

        public CommandBuffer RenderCmdBuffer => primaryCmdPool.CommandBuffers[RenderContext];

        //todo: multithread
        public CommandBufferPool WorkCmdPool => secondaryCmdPool[WorkContext];

        private RenderPass renderPass;
        public RenderPass RenderPass => renderPass;

        public uint currentImage;
        public int nextImage;

        private NativeList<Semaphores> semaphores = new NativeList<Semaphores>(1, 1);
        private DepthStencil depthStencil;

        public NativeList<VkPipelineStageFlags> submitPipelineStages = new NativeList<VkPipelineStageFlags>() { VkPipelineStageFlags.ColorAttachmentOutput };

        private VkSubmitInfo submitInfo;

        DeviceBuffer[] positionBuffer = new DeviceBuffer[2];
        DeviceBuffer[] instanceBuffer = new DeviceBuffer[2];
        DeviceBuffer[] transistBuffer = new DeviceBuffer[2];

        private ManualResetEvent _renderActive;
        private ManualResetEvent _renderComandsReady;
        private ManualResetEvent _renderCompleted;

        public Graphics(Settings settings)
        {
            Settings = settings;

            enabledFeatures.samplerAnisotropy = True;
            VkInstance = Device.CreateInstance(Settings);
            device = Device.Init(enabledFeatures, EnabledExtensions);
           
            // Get a graphics queue from the Device
            queue = Device.GetDeviceQueue(Device.QFIndices.Graphics, 0);

            DepthFormat = Device.GetSupportedDepthFormat();            

            // Create synchronization objects
            Semaphores* pSem = (Semaphores*)semaphores.GetAddress(0);
            pSem->PresentComplete = Device.CreateSemaphore();
            pSem->RenderComplete = Device.CreateSemaphore();

            // Set up submit info structure
            // Semaphores will stay the same during application lifetime
            // Command buffer submission info is set by each example
            submitInfo = VkSubmitInfo.New();
            submitInfo.pWaitDstStageMask = (VkPipelineStageFlags*)submitPipelineStages.Data;
            submitInfo.waitSemaphoreCount = 1;
            submitInfo.pWaitSemaphores = &pSem->PresentComplete;
            submitInfo.signalSemaphoreCount = 1;
            submitInfo.pSignalSemaphores = &pSem->RenderComplete;

            Texture.Init();


            _renderComandsReady = new ManualResetEvent(false);

            _renderActive = new ManualResetEvent(false);
            _renderCompleted = new ManualResetEvent(true);
        }


        public void Init(IntPtr wnd)
        {
            Swapchain.InitSurface(wnd);

            DescriptorPoolManager = new DescriptorPoolManager();

            CreateSwapChain();
            CreateDepthStencil();
            CreateRenderPass();
            CreateFrameBuffer();

            CreateCommandPool();
            CreateCommandBuffers();

        }

        protected override void Destroy()
        {
            Device.Shutdown();

            base.Destroy();
        }

        public void Resize(int w, int h)
        {
            Width = w;
            Height = h;

            // Ensure all operations on the device have been finished before destroying resources
            WaitIdle();

            CreateSwapChain();
            // Recreate the frame buffers
            CreateDepthStencil();

            CreateFrameBuffer();

            // Command buffers need to be recreated as they may store
            // references to the recreated frame buffer
            CreateCommandBuffers();

        }

        protected void CreateFrameBuffer()
        {
            if(Framebuffers != null)
            {
                for (int i = 0; i < Framebuffers.Length; i++)
                {
                    Framebuffers[i].Dispose();
                }
            }

            framebuffers = CreateSwapChainFramebuffers(renderPass);
        }

        private void CreateSwapChain()
        {
            uint width, height;
            Swapchain.Create(&width, &height, Settings.VSync);

            Width = (int)width;
            Height = (int)height;
        }

        public void CreateRenderPass()
        {
            AttachmentDescription[] attachments =
            {
                new AttachmentDescription(Swapchain.ColorFormat, finalLayout : ImageLayout.PresentSrcKHR),
                new AttachmentDescription(DepthFormat, finalLayout : ImageLayout.DepthStencilAttachmentOptimal)
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,
                    pColorAttachments = new []
                    {
                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                    },
                    pDepthStencilAttachment = new []
                    {
                        new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal)
                    },

                }
            };

            SubpassDependency[] dependencies = 
            {
                new SubpassDependency
                {
                    srcSubpass = SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = AccessFlags.MemoryRead,
                    dstAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            RenderPassCreateInfo renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            
            renderPass = new RenderPass(ref renderPassInfo);           
        }

        public Framebuffer[] CreateSwapChainFramebuffers(RenderPass vkRenderPass)
        {
            VkImageView* attachments = stackalloc VkImageView[2];
            // Depth/Stencil attachment is the same for all frame buffers
            attachments[1] = depthStencil.view;

            var frameBufferCreateInfo = new FramebufferCreateInfo
            {
                renderPass = vkRenderPass,
                attachmentCount = 2,
                pAttachments = attachments,
                width = Width,
                height = Height,
                layers = 1
            };

            // Create frame buffers for every swap chain image
            var framebuffers = new Framebuffer[Swapchain.ImageCount];
            for (uint i = 0; i < framebuffers.Length; i++)
            {
                attachments[0] = Swapchain.Buffers[i].View;
                framebuffers[i] = new Framebuffer(ref frameBufferCreateInfo);
            }

            return framebuffers;
        }
        
        private void CreateCommandPool()
        {
            primaryCmdPool = new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
            secondaryCmdPool = new CommandBufferPool[2]
            {
                new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer),
                new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer)
            };            
        }

        protected void CreateCommandBuffers()
        {
            primaryCmdPool.Allocate(CommandBufferLevel.Primary, (uint)Swapchain.ImageCount);

            foreach (var cmdPool in secondaryCmdPool)
            {
                cmdPool.Allocate(CommandBufferLevel.Secondary, 8);
            }
        }

        protected void CreateDepthStencil()
        {
            if (depthStencil != null)
            {
                depthStencil.Dispose();
                depthStencil = null;
            }

            depthStencil = new DepthStencil(Width, Height, DepthFormat);

        }

        private Texture GetFreeStagingTexture(uint width, uint height, uint depth, Format format)
        {/*
            uint totalSize = FormatHelpers.GetRegionSize(width, height, depth, format);
            lock (_stagingResourcesLock)
            {
                for (int i = 0; i < _availableStagingTextures.Count; i++)
                {
                    VkTexture tex = _availableStagingTextures[i];
                    if (tex.Memory.Size >= totalSize)
                    {
                        _availableStagingTextures.RemoveAt(i);
                        tex.SetStagingDimensions(width, height, depth, format);
                        return tex;
                    }
                }
            }

            uint texWidth = Math.Max(256, width);
            uint texHeight = Math.Max(256, height);
            Texture newTex = (Texture)ResourceFactory.CreateTexture(TextureDescription.Texture3D(
                texWidth, texHeight, depth, 1, format, TextureUsage.Staging));
            newTex.SetStagingDimensions(width, height, depth, format);
            */
            return null;
        }

        private const int MinStagingBufferSize = 64;
        private const int MaxStagingBufferSize = 512;

        private readonly object _stagingResourcesLock = new object();
        private readonly List<Texture> _availableStagingTextures = new List<Texture>();
        private readonly List<DeviceBuffer> _availableStagingBuffers = new List<DeviceBuffer>();
        private DeviceBuffer GetFreeStagingBuffer(int size)
        {
            lock (_stagingResourcesLock)
            {
                for (int i = 0; i < _availableStagingBuffers.Count; i++)
                {
                    DeviceBuffer buffer = _availableStagingBuffers[i];
                    if (buffer.Size >= size)
                    {
                        _availableStagingBuffers.RemoveAt(i);
                        return buffer;
                    }
                }
            }

            int newBufferSize = Math.Max(MinStagingBufferSize, size);
            DeviceBuffer newBuffer = DeviceBuffer.Create(BufferUsageFlags.TransferSrc | BufferUsageFlags.TransferDst,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, size, 1);
            return newBuffer;
        }

        public void WaitIdle()
        {
            device.WaitIdle();
        }

        public void BeginRender()
        {
#if NEW_SYNC
            Profiler.BeginSample("RenderWait");
            _renderActive.Reset();
            _renderCompleted.Set();
            _renderComandsReady.WaitOne();

            _renderCompleted.Reset();
            _renderComandsReady.Reset();
            //SwapBuffers();
            SwapContext();
            _renderActive.Set();
            Profiler.EndSample();
#else

            Profiler.BeginSample("MainSemWait");
            MainSemWait();
            Profiler.EndSample();

            Profiler.BeginSample("RenderSemPost");
            RenderSemPost();
            Profiler.EndSample();
#endif
            Profiler.BeginSample("Acquire");

            // Acquire the next image from the swap chaing
            VulkanUtil.CheckResult(Swapchain.AcquireNextImage(semaphores[0].PresentComplete, ref currentImage));
            nextImage = ((int)currentImage + 1)%ImageCount;
            Profiler.EndSample();

        }

        public void EndRender()
        {
            // Command buffer to be sumitted to the queue
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = (VkCommandBuffer*)primaryCmdPool.GetAddress((uint)RenderContext);

            Profiler.BeginSample("Submit");
            // Submit to queue
            VulkanUtil.CheckResult(vkQueueSubmit(queue, 1, ref submitInfo, VkFence.Null));

            Profiler.EndSample();

            Profiler.BeginSample("Present");


            VulkanUtil.CheckResult(Swapchain.QueuePresent(queue, currentImage, semaphores[0].RenderComplete));

            VulkanUtil.CheckResult(vkQueueWaitIdle(queue));

            Profiler.EndSample();
        }

#region MULTITHREADED

        private int currentContext;
        public int WorkContext => SingleLoop ? 0 : currentContext;
        public int RenderContext => SingleLoop ? 0 : 1 - currentContext;

        private int currentFrame;
        public int CurrentFrame => currentFrame;

        static int mainThreadID;
        public static bool IsMainThread => mainThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId;

        static int renderThreadID;
        public static bool IsRenderThread => renderThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId;

        private System.Threading.Semaphore renderSem = new System.Threading.Semaphore(0, 1);
        private System.Threading.Semaphore mainSem = new System.Threading.Semaphore(0, 1);
        public bool SingleLoop => Settings.SingleLoop;

        public static void SetMainThread()
        {
            mainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public static void SetRenderThread()
        {
            renderThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public void Frame()
        {
            Profiler.BeginSample("RenderSemWait");
#if NEW_SYNC
            _renderCompleted.WaitOne();
            _renderComandsReady.Set();
            _renderActive.WaitOne();
#else
            RenderSemWait();
            FrameNoRenderWait();
#endif
            Profiler.EndSample();
        }

        public void Close()
        {
#if NEW_SYNC
            _renderCompleted.Set();
#else
            MainSemWait();
            RenderSemPost();
#endif
        }

        void SwapContext()
        {
            currentFrame++;

            if(!SingleLoop)
            {
                currentContext = 1 - currentContext;
            }

        }

        public void FrameNoRenderWait()
        {
#if NEW_SYNC
#else
            SwapContext();
            // release render thread
            MainSemPost();
#endif
        }

        public void MainSemPost()
        {
            if (!SingleLoop)
            {
                mainSem.Release();
            }
        }

        bool MainSemWait()
        {
            if (SingleLoop)
            {
                return true;
            }

            long curTime = Stopwatch.GetTimestamp();
            bool ok = mainSem.WaitOne(-1);
            if (ok)
            {
                stats.LogicWait = Stopwatch.GetTimestamp() - curTime;
                return true;
            }

            return false;
        }

        void RenderSemPost()
        {
            if (!SingleLoop)
            {
                renderSem.Release();
            }
        }

        void RenderSemWait()
        {
            if (!SingleLoop)
            {
                long curTime = Stopwatch.GetTimestamp();
                bool ok = renderSem.WaitOne();
                stats.RenderWait = Stopwatch.GetTimestamp() - curTime;
            }
        }

#endregion

    }

    public struct Statistics
    {
        static long frameTick;
        public long LogicWait;
        public long RenderWait;
        
        public static void Tick(float timeStep)
        {
            frameTick = Stopwatch.GetTimestamp();
        }
    }
}
