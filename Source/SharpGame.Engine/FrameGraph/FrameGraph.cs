//#define SIMPLE_RENDER
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public struct RenderUpdate
    {
    }

    public struct PostRenderUpdate
    {
    }

    public struct BeginRender
    {
    }

    public struct EndRender
    {
    }

    public class FrameGraph : System<FrameGraph>
    {
        private List<RenderView> views = new List<RenderView>();
        public Graphics Graphics => Graphics.Instance;

        protected float debugImageHeight = 200.0f;
        List<ImageView> debugImages = new List<ImageView>();

        public DynamicBuffer TransformBuffer { get; }        
        public DynamicBuffer InstanceBuffer { get; }
        public DynamicBuffer MaterialBuffer { get; }

        public FrameGraphPass OverlayPass { get; set; }

        public static bool EarlyZ { get; set; }
        
        public event Action<RenderContext> OnBeginSubmit;
        public event Action<RenderContext, SubmitQueue> OnSubmit;
        public event Action<RenderContext> OnEndSubmit;

        public static bool drawDebug;
        public static bool debugDepthTest;
        public static bool debugOctree;
        public static bool debugImage = false;

        public FrameGraph()
        {
            this.Subscribe<GUIEvent>(e => OnDebugImage());

            uint size = Graphics.Settings.Validation ? 64 * 1000u : 64 * 1000 * 100u;

            TransformBuffer = new DynamicBuffer(VkBufferUsageFlags.UniformBuffer, size);
        }

        public void Resize(int w, int h)
        {
            foreach (var viewport in views)
            {
                viewport.Renderer.DeviceLost();
            }

            OverlayPass?.DeviceLost();

            Graphics.Resize(w, h);

            foreach (var viewport in views)
            {
                viewport.Renderer.DeviceReset();
            }

            OverlayPass?.DeviceReset();

        }


        public RenderView CreateRenderView(Camera camera = null, Scene scene = null, RenderPipeline frameGraph = null)
        {
            var view = new RenderView();           
            views.Add(view);
            view.Attach(camera, scene, frameGraph);
            return view;
        }

        public CommandBuffer GetWorkCmdBuffer(SubmitQueue queue)
        {
            return Graphics.WorkFrame.submitQueue[(int)queue].cmdBuffer;
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

            OverlayPass?.Update();

            this.SendGlobalEvent(new PostRenderUpdate());

            TransformBuffer.FlushAll();
        }

        public void Render()
        {
            this.SendGlobalEvent(new BeginRender());

            var rc = Graphics.WorkFrame;

            rc.Begin();

            foreach (var viewport in views)
            {
                viewport.Render(rc);
            }

            OverlayPass?.Draw(rc, rc.RenderCmdBuffer);

            rc.End();

            this.SendGlobalEvent(new EndRender());

            //Log.Info("Render frame " + Graphics.WorkContext);

        }

        public void Submit()
        {
            Profiler.BeginSample("RenderSystem.Submit");
            
            if (Graphics.BeginRender())
            {
                var renderFrame = Graphics.renderFrame;
                //Log.Info("          Submit frame " + renderFrame.id);

                OnBeginSubmit?.Invoke(renderFrame);
                renderFrame.Submit(OnSubmit);            
                OnEndSubmit?.Invoke(renderFrame);

                Graphics.EndRender();
            }

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
