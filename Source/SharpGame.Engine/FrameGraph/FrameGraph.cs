//#define SIMPLE_RENDER
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class FrameGraph : System<FrameGraph>
    {
        private List<RenderView> views = new List<RenderView>();
        public Graphics Graphics => Graphics.Instance;

        protected float debugImageHeight = 200.0f;
        List<ImageView> debugImages = new List<ImageView>();

        public DynamicBuffer TransformBuffer { get; }        
        public DynamicBuffer InstanceBuffer { get; }
        public DynamicBuffer MaterialBuffer { get; }

        public GraphicsPass OverlayPass { get; set; }

        public static bool EarlyZ { get; set; }

        struct CmdBufferBlock
        {
            public CommandBuffer cmdBuffer;
            public Fence submitFence;
        };

        CommandBufferPool preRenderCmdPool;
        CommandBufferPool computeCmdPool;
        CommandBufferPool renderCmdPool;

        CmdBufferBlock[] preRenderCmdBlk = new CmdBufferBlock[3];
        CmdBufferBlock[] computeCmdBlk = new CmdBufferBlock[3];
        CmdBufferBlock[] renderCmdBlk = new CmdBufferBlock[3];

        public CommandBuffer WorkComputeCmdBuffer => computeCmdBlk[Graphics.WorkImage].cmdBuffer;
        public CommandBuffer RenderCmdBuffer => renderCmdBlk[Graphics.RenderImage].cmdBuffer;

        public event Action<int> OnBeginSubmit;
        public event Action<int, PassQueue> OnSubmit;
        public event Action<int> OnEndSubmit;

        public static bool drawDebug;
        public static bool debugDepthTest;
        public static bool debugOctree;
        public static bool debugImage = false;

        public FrameGraph()
        {
            this.Subscribe<GUIEvent>(e => OnDebugImage());

            preRenderCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);
            computeCmdPool = new CommandBufferPool(Device.QFCompute, CommandPoolCreateFlags.ResetCommandBuffer);
            renderCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);

            uint size = Graphics.Settings.Validation ? 64 * 1000u : 64 * 1000 * 100u;

            TransformBuffer = new DynamicBuffer(BufferUsageFlags.UniformBuffer, size);
        }

        public void Initialize()
        {
            preRenderCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for(int i = 0; i < 3; i++)
            {
                preRenderCmdBlk[i].cmdBuffer = preRenderCmdPool.CommandBuffers[i];
                preRenderCmdBlk[i].submitFence = new Fence(FenceCreateFlags.Signaled);
            }            

            computeCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for (int i = 0; i < 3; i++)
            {
                computeCmdBlk[i].cmdBuffer = computeCmdPool.CommandBuffers[i];
                computeCmdBlk[i].submitFence = new Fence(FenceCreateFlags.Signaled);
            }

            renderCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for (int i = 0; i < 3; i++)
            {
                renderCmdBlk[i].cmdBuffer = renderCmdPool.CommandBuffers[i];
                renderCmdBlk[i].submitFence = new Fence(FenceCreateFlags.Signaled);
            }
        }

        public RenderView CreateRenderView(Camera camera = null, Scene scene = null, RenderPipeline frameGraph = null)
        {
            var view = new RenderView();           
            views.Add(view);
            view.Attach(camera, scene, frameGraph);
            return view;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe int GetTransform(IntPtr pos, uint count)
        {
            uint sz = count << 6;// (uint)Utilities.SizeOf<mat4>() * count;
            return (int)TransformBuffer.Alloc(sz, pos);
        }

        public void Update()
        {
            var frameInfo = new FrameInfo
            {
                timeStep = Time.Delta,
                frameNumber = Time.FrameNum
            };

            TransformBuffer.Clear();

            mat4 m = mat4.Identity;
            GetTransform(Utilities.AsPointer(ref m), 1);

            this.SendGlobalEvent(new RenderUpdate());

            foreach (var viewport in views)
            {
                viewport.Scene?.RenderUpdate(frameInfo);

                viewport.Update(ref frameInfo);
            }

            if(drawDebug)
            {
                DrawDebugGeometry();
            }

            OverlayPass?.Update(null);

            this.SendGlobalEvent(new PostRenderUpdate());

            TransformBuffer.Flush();
        }

        public void Render()
        {
            this.SendGlobalEvent(new BeginRender());

            foreach (var viewport in views)
            {
                viewport.Render();
            }

            OverlayPass?.Draw(null);

            this.SendGlobalEvent(new EndRender());

            //Log.Info("Render frame " + Graphics.WorkImage);
        }

        public void Submit()
        {
            Profiler.BeginSample("RenderSystem.Submit");

            Graphics.BeginRender();

            int imageIndex = (int)Graphics.RenderImage;
            //Log.Info("Submit frame " + imageIndex);

            OnBeginSubmit?.Invoke(imageIndex);

            var currentBuffer = Graphics.currentBuffer;

            {
                Profiler.BeginSample("RenderSystem.PreRender");

                var fence = preRenderCmdBlk[imageIndex].submitFence;
                fence.Wait();
                fence.Reset();

                CommandBuffer cmdBuffer = preRenderCmdBlk[imageIndex].cmdBuffer;

                cmdBuffer.Begin();

                foreach (var viewport in views)
                {
                    viewport.Renderer.Submit(cmdBuffer, PassQueue.EarlyGraphics, imageIndex);
                }

                cmdBuffer.End();

                Graphics.GraphicsQueue.Submit(currentBuffer.acquireSemaphore, PipelineStageFlags.FragmentShader,
                    cmdBuffer, currentBuffer.preRenderSemaphore, fence);

                OnSubmit?.Invoke(imageIndex, PassQueue.EarlyGraphics);
                Profiler.EndSample();
            }

            {
                Profiler.BeginSample("RenderSystem.Compute");
                var fence = computeCmdBlk[imageIndex].submitFence;

                fence.Wait();
                fence.Reset();

                CommandBuffer cmdBuffer = computeCmdBlk[imageIndex].cmdBuffer;

                foreach (var viewport in views)
                {
                    viewport.Renderer.Submit(null, PassQueue.Compute, imageIndex);
                }

                if (cmdBuffer.NeedSubmit)
                {
                    Graphics.ComputeQueue.Submit(currentBuffer.preRenderSemaphore, PipelineStageFlags.ComputeShader,
                    cmdBuffer, currentBuffer.computeSemaphore, fence);                    
                }
                else
                {
                    Graphics.ComputeQueue.Submit(currentBuffer.preRenderSemaphore, PipelineStageFlags.ComputeShader,
                    null, currentBuffer.computeSemaphore, fence);
                }

                OnSubmit?.Invoke(imageIndex, PassQueue.Compute);
                Profiler.EndSample();
            }

            {
                Profiler.BeginSample("RenderSystem.Render");
                var fence = renderCmdBlk[imageIndex].submitFence;
                fence.Wait();
                fence.Reset();

                CommandBuffer cmdBuffer = renderCmdBlk[imageIndex].cmdBuffer;
                
                cmdBuffer.Begin();

                foreach (var viewport in views)
                {
                    viewport.Submit(cmdBuffer, PassQueue.Graphics, imageIndex);
                }

                OverlayPass?.Submit(cmdBuffer, imageIndex);

                cmdBuffer.End();

                Graphics.GraphicsQueue.Submit(currentBuffer.computeSemaphore, PipelineStageFlags.ColorAttachmentOutput,
                    cmdBuffer, currentBuffer.renderSemaphore, fence);

                OnSubmit?.Invoke(imageIndex, PassQueue.Graphics);
                Profiler.EndSample();
            }

            OnEndSubmit?.Invoke(imageIndex);

            Graphics.EndRender();

            Profiler.EndSample();

        }

        private void DrawDebugGeometry()
        {
            foreach (var viewport in views)
            {
                if (viewport.Scene == null)
                {
                    continue;
                }

                var debug = viewport.Scene.GetComponent<DebugRenderer>();
                if (debug == null || !debug.IsEnabledEffective())
                {
                    continue;
                }

                var spacePartitioner = viewport.Scene.SpacePartitioner;
                if(debugOctree && spacePartitioner != null)
                {
                    spacePartitioner.DrawDebugGeometry(debug, debugDepthTest);
                }

                foreach (var drawable in viewport.drawables)
                {
                    drawable.DrawDebugGeometry(debug, debugDepthTest);
                }

                foreach (var light in viewport.lights)
                {
                    light.DrawDebugGeometry(debug, debugDepthTest);
                }

            }
        }

        public void DebugImage(bool enable, float height = 200.0f)
        {
            debugImage = enable;
            debugImageHeight = height;
        }

        public void AddDebugImage(params Texture[] textures)
        {
            foreach (var tex in textures)
            {
                ImGUI.Instance.GetOrCreateImGuiBinding(tex);
                debugImages.Add(tex.imageView);
            }
        }

        public void AddDebugImage(params ImageView[] imageViews)
        {
            foreach (var tex in imageViews)
            {
                ImGUI.Instance.GetOrCreateImGuiBinding(tex);
                debugImages.Add(tex);
            }
        }

        void OnDebugImage()
        {
            if (!debugImage || debugImages.Count == 0)
            {
                return;
            }

            var io = ImGui.GetIO();
            {
                vec2 window_pos = new vec2(10, io.DisplaySize.Y - 10);
                vec2 window_pos_pivot = new vec2(0.0f, 1.0f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            }

            if (ImGui.Begin("DebugImage", ref debugImage, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                foreach (var tex in debugImages)
                {
                    float scale = tex.Width / (float)tex.Height;
                    if (scale > 1)
                    {
                        ImGUI.Image(tex, new vec2(debugImageHeight, debugImageHeight / scale)); ImGui.SameLine();
                    }
                    else
                    {
                        ImGUI.Image(tex, new vec2(scale * debugImageHeight, debugImageHeight)); ImGui.SameLine();
                    }

                }
                ImGui.Spacing();
                ImGui.SliderFloat("Image Height", ref debugImageHeight, 100, 1000);
            }

            ImGui.End();

        }

        protected override void Destroy(bool disposing)
        {
            debugImages.Clear();

            base.Destroy(disposing);
        }

    }
}
