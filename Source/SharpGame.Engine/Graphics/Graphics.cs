using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
        public CString Name { get; set; }
        public bool Validation { get; set; } = true;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = false;
        public bool SingleLoop { get; set; }
    }

    public unsafe partial class Graphics : System<Graphics>
    {
        public Settings Settings { get; } = new Settings();
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
        public uint nextImage;

        private NativeList<Semaphores> semaphores = new NativeList<Semaphores>(1, 1);
        private DepthStencil depthStencil;

        public NativeList<VkPipelineStageFlags> submitPipelineStages = new NativeList<VkPipelineStageFlags>() { VkPipelineStageFlags.ColorAttachmentOutput };

        private VkSubmitInfo submitInfo;

        DeviceBuffer[] positionBuffer = new DeviceBuffer[2];
        DeviceBuffer[] instanceBuffer = new DeviceBuffer[2];
        DeviceBuffer[] transistBuffer = new DeviceBuffer[2];

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
                // Color attachment
                new AttachmentDescription(Swapchain.ColorFormat, finalLayout : ImageLayout.PresentSrcKHR),
                // Depth attachment
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

            // Subpass dependencies for layout transitions
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
            DeviceBuffer newBuffer = DeviceBuffer.Create(BufferUsage.TransferSrc | BufferUsage.TransferDst,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, size, 1);
            return newBuffer;
        }

        public void WaitIdle()
        {
            device.WaitIdle();
        }

        public void BeginRender()
        {
            // Acquire the next image from the swap chaing
            VulkanUtil.CheckResult(Swapchain.AcquireNextImage(semaphores[0].PresentComplete, ref currentImage));
            nextImage = (currentImage + 1)%(uint)ImageCount;
            MainSemWait();
        }

        public void EndRender()
        {
            // Command buffer to be sumitted to the queue
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = (VkCommandBuffer*)primaryCmdPool.GetAddress((uint)RenderContext);

            // Submit to queue
            VulkanUtil.CheckResult(vkQueueSubmit(queue, 1, ref submitInfo, VkFence.Null));

            VulkanUtil.CheckResult(Swapchain.QueuePresent(queue, currentImage, semaphores[0].RenderComplete));

            VulkanUtil.CheckResult(vkQueueWaitIdle(queue));

            RenderSemPost();
        }

        #region MULTITHREADED

        private int currentContext_;
        public int WorkContext => SingleLoop ? 0 : currentContext_;
        public int RenderContext => SingleLoop ? 0 : 1 - currentContext_;

        private int currentFrame_;
        public int CurrentFrame => currentFrame_;

        private int renderThreadID_;
        public bool IsRenderThread => renderThreadID_ == System.Threading.Thread.CurrentThread.ManagedThreadId;

        private System.Threading.Semaphore renderSem_ = new System.Threading.Semaphore(0, 1);
        private System.Threading.Semaphore mainSem_ = new System.Threading.Semaphore(0, 1);
        private long waitSubmit_;
        private long waitRender_;

        public bool SingleLoop => Settings.SingleLoop;

        private List<Action> commands_ = new List<Action>();

        public void Post(Action action) { commands_.Add(action); }

        public void Frame()
        {
            RenderSemWait();
            FrameNoRenderWait();
        }

        public void Close()
        {
            MainSemWait();
            RenderSemPost();
        }

        /*
        int imageIndex = 0;
        public int BeginRender()
        {
            imageIndex = Swapchain.AcquireNextImage(semaphore: ImageAvailableSemaphore);

            if (MainSemWait())
            {
                return imageIndex;
            }

            return imageIndex;
        }

        public void EndRender()
        {
            RenderSemPost();
        }*/

        void SwapContext()
        {
            currentFrame_++;
            if(!SingleLoop)
            currentContext_ = 1 - currentContext_;
            //Console.WriteLine("===============SwapContext : {0}", currentContext_);
        }

        public void FrameNoRenderWait()
        {
            SwapContext();
            // release render thread
            MainSemPost();
        }

        public void MainSemPost()
        {
            if (!SingleLoop)
            {
                mainSem_.Release();
            }
        }

        bool MainSemWait()
        {
            if (SingleLoop)
            {
                return true;
            }

            long curTime = Stopwatch.GetTimestamp();
            bool ok = mainSem_.WaitOne(-1);
            if (ok)
            {
                waitSubmit_ = (long)((Stopwatch.GetTimestamp() - curTime) * Timer.MilliSecsPerTick);
                return true;
            }

            return false;
        }

        void RenderSemPost()
        {
            if (!SingleLoop)
            {
                renderSem_.Release();
            }
        }

        void RenderSemWait()
        {
            if (!SingleLoop)
            {
                long curTime = Stopwatch.GetTimestamp();
                bool ok = renderSem_.WaitOne();
                waitRender_ = (long)((Stopwatch.GetTimestamp() - curTime) * Timer.MilliSecsPerTick);
            }
        }
        #endregion
    }
}
