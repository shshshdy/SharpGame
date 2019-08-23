//#define EVENT_SYNC

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
    public class Settings
    {
        public UTF8String ApplicationName { get; set; }
        public bool Validation { get; set; } = false;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = false;
        public bool SingleLoop { get; set; }
    }

    public unsafe partial class Graphics : System<Graphics>
    {
        public Settings Settings { get; } = new Settings();
        internal Stats stats = new Stats();

        public VkPhysicalDeviceFeatures enabledFeatures;
        public NativeList<IntPtr> EnabledExtensions { get; } = new NativeList<IntPtr>();

        public static VkDevice device { get; protected set; }
        public static Queue GraphicsQueue { get; protected set; }
        public static Queue ComputeQueue { get; protected set; }

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
        private CommandBufferPool workCmdPool;
        private CommandBufferPool computeCmdPool;

        public CommandBuffer RenderCmdBuffer => primaryCmdPool.CommandBuffers[RenderContext];

        public CommandBuffer WorkComputeBuffer => computeCmdPool.CommandBuffers[WorkContext];
        public CommandBuffer RenderComputeBuffer => computeCmdPool.CommandBuffers[RenderContext];

        public CommandBuffer submitComputeCmdBuffer;

        private RenderPass renderPass;
        public RenderPass RenderPass => renderPass;

        public uint currentImage;
        public int nextImage;

        public Semaphore PresentComplete { get; }
        public Semaphore RenderComplete { get; }

        Fence computeFence;

        private DepthStencil depthStencil;
        
        TransientBufferManager transientVertexBuffer = new TransientBufferManager(BufferUsageFlags.VertexBuffer, 1024 * 1024);
        TransientBufferManager transientIndexBuffer = new TransientBufferManager(BufferUsageFlags.IndexBuffer, 1024 * 1024);

#if EVENT_SYNC
        private ManualResetEvent _renderActive;
        private ManualResetEvent _renderComandsReady;
        private ManualResetEvent _renderCompleted;
#endif  

        public Graphics(Settings settings)
        {
            Settings = settings;

            enabledFeatures.samplerAnisotropy = True;
            Device.CreateInstance(Settings);
            device = Device.Init(enabledFeatures, EnabledExtensions);
           
            // Get a graphics queue from the Device
            GraphicsQueue = Queue.GetDeviceQueue(Device.QFIndices.Graphics, 0);
            ComputeQueue = Queue.GetDeviceQueue(Device.QFIndices.Compute, 0);
            DepthFormat = Device.GetSupportedDepthFormat();            
               
            // Create synchronization objects
            PresentComplete = new Semaphore();
            RenderComplete = new Semaphore();
            computeFence = new Fence(FenceCreateFlags.Signaled);

            Texture.Init();

#if EVENT_SYNC
            _renderComandsReady = new ManualResetEvent(false);
            _renderActive = new ManualResetEvent(false);
            _renderCompleted = new ManualResetEvent(true);
#endif
        }


        public void Init(IntPtr wnd)
        {
            Swapchain.InitSurface(wnd);

            DescriptorPoolManager = new DescriptorPoolManager();

            CreateSwapChain();
            CreateDepthStencil();
            CreateDefaultRenderPass();
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

        public void CreateDefaultRenderPass()
        {
            AttachmentDescription[] attachments =
            {
                new AttachmentDescription(Swapchain.ColorFormat/*Format.R16g16b16a16Sfloat*/, finalLayout : ImageLayout.PresentSrcKHR),
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

        public RenderPass CreateRenderPass(bool clearColor = false, bool clearDepth = false)
        {
            AttachmentDescription[] attachments =
            {
                // Color attachment
                new AttachmentDescription
                (
                    ColorFormat,
                    loadOp : clearColor? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                    storeOp : AttachmentStoreOp.Store,
                    finalLayout : ImageLayout.PresentSrcKHR
                ),

                // Depth attachment
                new AttachmentDescription
                (
                    DepthFormat,
                    loadOp : clearDepth ? AttachmentLoadOp.Clear : AttachmentLoadOp.DontCare,
                    storeOp : AttachmentStoreOp.DontCare,
                    finalLayout : ImageLayout.DepthStencilAttachmentOptimal
                )
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
                    srcSubpass = VulkanNative.SubpassExternal,
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
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            renderPass = new RenderPass(ref renderPassInfo);
            return renderPass;
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
            primaryCmdPool = new CommandBufferPool(Swapchain.QueueNodeIndex, CommandPoolCreateFlags.ResetCommandBuffer);
            workCmdPool = new CommandBufferPool(Swapchain.QueueNodeIndex, CommandPoolCreateFlags.ResetCommandBuffer);
            computeCmdPool = new CommandBufferPool(ComputeQueue.FamilyIndex, CommandPoolCreateFlags.ResetCommandBuffer);
        }

        protected void CreateCommandBuffers()
        {
            primaryCmdPool.Allocate(CommandBufferLevel.Primary, (uint)Swapchain.ImageCount);
            workCmdPool.Allocate(CommandBufferLevel.Primary, (uint)Swapchain.ImageCount);
            computeCmdPool.Allocate(CommandBufferLevel.Primary, (uint)Swapchain.ImageCount);
        }

        public CommandBuffer BeginWorkCommandBuffer()
        {
            workCmdPool[WorkContext].Begin(CommandBufferUsageFlags.OneTimeSubmit);
            return workCmdPool[WorkContext];
        }

        public void EndWorkCommandBuffer(CommandBuffer commandBuffer)
        {
            commandBuffer.End();

            GraphicsQueue.Submit(null, PipelineStageFlags.None, commandBuffer, null);
            GraphicsQueue.WaitIdle();

            commandBuffer.Reset(true);
        }

        protected void CreateDepthStencil()
        {
            depthStencil?.Dispose();         
            depthStencil = new DepthStencil(Width, Height, DepthFormat);
        }
        
        public TransientBuffer AllocVertexBuffer(uint count)
        {
            return transientVertexBuffer.Alloc(count);
        }

        public TransientBuffer AllocIndexBuffer(uint count)
        {
            return transientIndexBuffer.Alloc(count);
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
        private DeviceBuffer GetFreeStagingBuffer(uint size)
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

            uint newBufferSize = Math.Max(MinStagingBufferSize, size);
            DeviceBuffer newBuffer = DeviceBuffer.Create(BufferUsageFlags.TransferSrc | BufferUsageFlags.TransferDst, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent, size, 1);
            return newBuffer;
        }

        public void WaitIdle()
        {
            device.WaitIdle();
        }

        public void BeginRender()
        {
#if EVENT_SYNC
            if (!SingleLoop)
            {
                Profiler.BeginSample("RenderWait");
                _renderActive.Reset();
                _renderCompleted.Set();
                _renderComandsReady.WaitOne();

                _renderCompleted.Reset();
                _renderComandsReady.Reset();
                //SwapBuffers();
                //SwapContext();
                _renderActive.Set();
                Profiler.EndSample();
            }

#else

            Profiler.BeginSample("MainSemWait");
            MainSemWait();
            Profiler.EndSample();

#endif
            Profiler.BeginSample("Acquire");
            // Acquire the next image from the swap chaing
            Swapchain.AcquireNextImage(PresentComplete, ref currentImage);

            System.Diagnostics.Debug.Assert(currentImage == nextImage);
            nextImage = ((int)currentImage + 1)%ImageCount;
            Profiler.EndSample();

        }

        public void EndRender()
        {
            Profiler.BeginSample("Submit");

            if(submitComputeCmdBuffer)
            {
                ComputeQueue.Submit(null, PipelineStageFlags.None, submitComputeCmdBuffer, null, computeFence);
                computeFence.Wait();
                computeFence.Reset();
                submitComputeCmdBuffer = null;
            }

            GraphicsQueue.Submit(PresentComplete, PipelineStageFlags.ColorAttachmentOutput, primaryCmdPool[RenderContext], RenderComplete);

            Profiler.EndSample();

            Profiler.BeginSample("Present");

            Swapchain.QueuePresent(GraphicsQueue, currentImage, RenderComplete);
            
            GraphicsQueue.WaitIdle();

            Profiler.EndSample();


#if EVENT_SYNC

#else
            Profiler.BeginSample("RenderSemPost");
            RenderSemPost();
            Profiler.EndSample();
#endif
        }

#region MULTITHREADED   

        static int mainThreadID;
        static int renderThreadID;

        public static bool IsMainThread => mainThreadID == Thread.CurrentThread.ManagedThreadId;
        public static bool IsRenderThread => renderThreadID == Thread.CurrentThread.ManagedThreadId;
        public static void SetMainThread() => mainThreadID = Thread.CurrentThread.ManagedThreadId;
        public static void SetRenderThread() => renderThreadID = Thread.CurrentThread.ManagedThreadId;

        public bool SingleLoop => Settings.SingleLoop;

        private int currentContext;
        public int WorkContext => SingleLoop ? 0 : currentContext;
        public int RenderContext => SingleLoop ? 0 : 1 - currentContext;

        private int currentFrame;
        public int CurrentFrame => currentFrame;
        
        private System.Threading.Semaphore renderSem = new System.Threading.Semaphore(0, 1);
        private System.Threading.Semaphore mainSem = new System.Threading.Semaphore(0, 1);

        List<System.Action> actions = new List<Action>();

        public void Execute(System.Action action)
        {
            actions.Add(action);
        }

        public void Frame()
        {
            Profiler.BeginSample("RenderSemWait");
            WaitRender();
            WakeRender();
            Profiler.EndSample();
        }

        void SwapContext()
        {
            if(actions.Count > 0)
            {
                foreach(var action in actions)
                {
                    action.Invoke();
                }

                actions.Clear();
            }

            currentFrame++;

            if(!SingleLoop)
            {
                currentContext = 1 - currentContext;
            }

        }

        public void WakeRender()
        {
            SwapContext();
#if EVENT_SYNC
            _renderComandsReady.Set();
            _renderActive.WaitOne();
#else
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

        void WaitRender()
        {
            if (!SingleLoop)
            {
                long curTime = Stopwatch.GetTimestamp();
#if EVENT_SYNC
                _renderCompleted.WaitOne();
#else
                bool ok = renderSem.WaitOne();
#endif
                stats.RenderWait = Stopwatch.GetTimestamp() - curTime;
            }
        }

        public void Close()
        {
#if EVENT_SYNC
            _renderCompleted.Set();
#else
            MainSemWait();
            RenderSemPost();
#endif
        }

#endregion

    }

    public struct Stats
    {
        public long LogicWait;
        public long RenderWait;
        public static int drawCall;
        public static int triCount;
        
        public static void Tick(float timeStep)
        {
            drawCall = 0;
            triCount = 0;
        }
    }
}
