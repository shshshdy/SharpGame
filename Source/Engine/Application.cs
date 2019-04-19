using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using VulkanCore;


namespace SharpGame
{
    public enum PlatformType
    {
        Android, Win32, MacOS
    }

    public interface IPlatform : IDisposable
    {
        IntPtr WindowHandle { get; }
        IntPtr InstanceHandle { get; }
        int Width { get; }
        int Height { get; }
        PlatformType Platform { get; }

        Stream Open(string path);
    }

    public abstract class Application : Object
    {
        public IPlatform Platform { get; set; }
        public Graphics Graphics { get; private set; }
        public  Renderer Renderer { get; private set; }
        public ResourceCache ResourceCache { get; private set; }

        bool inited = false;
        public Application()
        {
            new Context();
        }

        public void Initialize(IPlatform host)
        {
            Platform = host;

            Graphics = CreateSubsystem<Graphics>(Platform);
            ResourceCache = CreateSubsystem<ResourceCache>(Platform, "Content");
            Renderer = CreateSubsystem<Renderer>();

            //new Thread(() =>
            {

                // Allow concrete samples to initialize their resources.
                InitializePermanent();
                //_initializingPermanent = false;
                InitializeFrame();

                // Record commands for execution by Vulkan.
                RecordCommandBuffers();

                inited = true;
            }
            //).Start();

        }

        /// <summary>
        /// Allows derived classes to initializes resources the will stay alive for the duration of
        /// the application.
        /// </summary>
        protected virtual void InitializePermanent() { }

        /// <summary>
        /// Allows derived classes to initializes resources that need to be recreated on events such
        /// as window resize.
        /// </summary>
        protected virtual void InitializeFrame() { }

        public void Resize()
        {
            Graphics.Resize();

            Renderer.Recreate();

            InitializeFrame();

            // Re-record command buffers.
            RecordCommandBuffers();
        }

        public void Tick(Timer timer)
        {
            Update(timer);
            //if(inited)
            Graphics.Draw(timer);
        }

        protected virtual void Update(Timer timer) { }

        void RecordCommandBuffers()
        {
            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);
            for (int i = 0; i < Graphics.CommandBuffers.Length; i++)
            {
                CommandBuffer cmdBuffer = Graphics.CommandBuffers[i];
                cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.SimultaneousUse));

                if (Graphics.PresentQueue != Graphics.GraphicsQueue)
                {
                    var barrierFromPresentToDraw = new ImageMemoryBarrier(
                        Graphics.SwapchainImages[i], subresourceRange,
                        Accesses.MemoryRead, Accesses.ColorAttachmentWrite,
                        ImageLayout.Undefined, ImageLayout.PresentSrcKhr,
                        Graphics.PresentQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);

                    cmdBuffer.CmdPipelineBarrier(
                        PipelineStages.ColorAttachmentOutput,
                        PipelineStages.ColorAttachmentOutput,
                        imageMemoryBarriers: new[] { barrierFromPresentToDraw });
                }

                RecordCommandBuffer(cmdBuffer, i);

                if (Graphics.PresentQueue != Graphics.GraphicsQueue)
                {
                    var barrierFromDrawToPresent = new ImageMemoryBarrier(
                        Graphics.SwapchainImages[i], subresourceRange,
                        Accesses.ColorAttachmentWrite, Accesses.MemoryRead,
                        ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr,
                        Graphics.GraphicsQueue.FamilyIndex, Graphics.PresentQueue.FamilyIndex);

                    cmdBuffer.CmdPipelineBarrier(
                        PipelineStages.ColorAttachmentOutput,
                        PipelineStages.BottomOfPipe,
                        imageMemoryBarriers: new[] { barrierFromDrawToPresent });
                }

                cmdBuffer.End();
            }
        }


        protected abstract void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex);

        protected T ToDispose<T>(T disposable) => Graphics.ToDispose(disposable);
        protected T ToDisposeFrame<T>(T disposable) => Graphics.ToDisposeFrame(disposable);

        public override void Dispose()
        {
            Graphics.Dispose();
        }
    }

}
