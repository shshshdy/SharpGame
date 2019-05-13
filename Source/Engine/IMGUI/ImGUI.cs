using NuklearSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using VulkanCore;

using static NuklearSharp.NuklearNative;

namespace SharpGame
{
    public struct GUIEvent
    {
    }

    public unsafe partial class ImGUI : Object
    {
        static nk_context* ctx;
        static nk_allocator* Allocator;
        static nk_font_atlas* FontAtlas;
        static nk_draw_null_texture* NullTexture;
        static nk_convert_config* ConvertCfg;

        static nk_buffer* Commands, Vertices, Indices;
        static byte[] LastMemory;

        static nk_draw_vertex_layout_element* VertexLayout;
        static nk_plugin_alloc_t Alloc;
        static nk_plugin_free_t Free;

        private GraphicsBuffer _vertexBuffer;
        private GraphicsBuffer _indexBuffer;
        private GraphicsBuffer _projMatrixBuffer;


        private ResourceSet resourceSet_;
        private Shader uiShader_;
        private Pipeline pipeline_;
        private Texture fontTex_;

        List<Texture> textures = new List<Texture>();

        public ImGUI()
        {
            Init();

            var resourceLayout = uiShader_.Main.ResourceLayout;
            resourceSet_ = new ResourceSet(resourceLayout);

            resourceSet_.Bind(0, _projMatrixBuffer)
            .Bind(1, fontTex_)
            .UpdateSets();

            this.SubscribeToEvent((ref BeginFrame e) => UpdateGUI());

            this.SubscribeToEvent((EndRenderPass e) => RenderGUI(e.renderPass));
        }

        static IntPtr ManagedAlloc(IntPtr Size, bool ClearMem = true)
        {
            if (ClearMem)
                return Utilities.AllocateAndClear(Size.ToInt32(), 0);
            else
                return Utilities.Allocate(Size.ToInt32());
        }

        static IntPtr ManagedAlloc(int Size)
        {
            return Utilities.Allocate(Size);
        }

        static void ManagedFree(IntPtr Mem)
        {
            Utilities.Free(Mem);
        }

        void FontStash()
        {
            nk_font_atlas_init(FontAtlas, Allocator);
            nk_font_atlas_begin(FontAtlas);

            FontStash(new IntPtr(FontAtlas));

            int W, H;
            IntPtr Image = NuklearNative.nk_font_atlas_bake(FontAtlas, &W, &H, nk_font_atlas_format.NK_FONT_ATLAS_RGBA32);
            int TexHandle = CreateTextureHandle(W, H, Image);

            nk_font_atlas_end(FontAtlas, NuklearNative.nk_handle_id(TexHandle), NullTexture);

            if (FontAtlas->default_font != null)
                NuklearNative.nk_style_set_font(ctx, &FontAtlas->default_font->handle);
        }


        private void Init()
        {
            // TODO: Free these later
            ctx = (nk_context*)ManagedAlloc(sizeof(nk_context));
            Allocator = (nk_allocator*)ManagedAlloc(sizeof(nk_allocator));
            FontAtlas = (nk_font_atlas*)ManagedAlloc(sizeof(nk_font_atlas));
            NullTexture = (nk_draw_null_texture*)ManagedAlloc(sizeof(nk_draw_null_texture));
            ConvertCfg = (nk_convert_config*)ManagedAlloc(sizeof(nk_convert_config));
            Commands = (nk_buffer*)ManagedAlloc(sizeof(nk_buffer));
            Vertices = (nk_buffer*)ManagedAlloc(sizeof(nk_buffer));
            Indices = (nk_buffer*)ManagedAlloc(sizeof(nk_buffer));

            VertexLayout = (nk_draw_vertex_layout_element*)ManagedAlloc(sizeof(nk_draw_vertex_layout_element) * 4);
            VertexLayout[0] = new nk_draw_vertex_layout_element
            {
                attribute = nk_draw_vertex_layout_attribute.NK_VERTEX_POSITION,
                format = nk_draw_vertex_layout_format.NK_FORMAT_FLOAT,
                offset_nksize = Marshal.OffsetOf(typeof(Pos2dTexColorVertex), nameof(Pos2dTexColorVertex.Position))
            };
            VertexLayout[1] = new nk_draw_vertex_layout_element
            {
                attribute = nk_draw_vertex_layout_attribute.NK_VERTEX_TEXCOORD,
                format = nk_draw_vertex_layout_format.NK_FORMAT_FLOAT,
                offset_nksize = Marshal.OffsetOf(typeof(Pos2dTexColorVertex), nameof(Pos2dTexColorVertex.TexCoord))
            };
            VertexLayout[2] = new nk_draw_vertex_layout_element
            {
                attribute = nk_draw_vertex_layout_attribute.NK_VERTEX_COLOR,
                format = nk_draw_vertex_layout_format.NK_FORMAT_R8G8B8A8,
                offset_nksize = Marshal.OffsetOf(typeof(Pos2dTexColorVertex), nameof(Pos2dTexColorVertex.Color))
            };
            VertexLayout[3] = new nk_draw_vertex_layout_element
            {
                attribute = nk_draw_vertex_layout_attribute.NK_VERTEX_ATTRIBUTE_COUNT,
                format = nk_draw_vertex_layout_format.NK_FORMAT_COUNT,
                offset_nksize = IntPtr.Zero
            };

            Alloc = (Handle, Old, Size) => ManagedAlloc(Size);
            Free = (Handle, Old) => ManagedFree(Old);

            Allocator->alloc_nkpluginalloct = Marshal.GetFunctionPointerForDelegate(Alloc);
            Allocator->free_nkpluginfreet = Marshal.GetFunctionPointerForDelegate(Free);

            nk_init(ctx, Allocator, null);

            CreateGraphicsResource();

            FontStash();

            ConvertCfg->shape_AA = nk_anti_aliasing.NK_ANTI_ALIASING_ON;
            ConvertCfg->line_AA = nk_anti_aliasing.NK_ANTI_ALIASING_ON;
            ConvertCfg->vertex_layout = VertexLayout;
            ConvertCfg->vertex_size = new IntPtr(sizeof(Pos2dTexColorVertex));
            ConvertCfg->vertex_alignment = new IntPtr(1);
            ConvertCfg->circle_segment_count = 22;
            ConvertCfg->curve_segment_count = 22;
            ConvertCfg->arc_segment_count = 22;
            ConvertCfg->global_alpha = 1.0f;
            ConvertCfg->null_tex = *NullTexture;

            nk_buffer_init(Commands, Allocator, new IntPtr(4 * 1024));
            nk_buffer_init(Vertices, Allocator, new IntPtr(4 * 1024));
            nk_buffer_init(Indices, Allocator, new IntPtr(4 * 1024));
        }

        static void SetDeltaTime(float Delta)
        {
            if (ctx != null)
                ctx->delta_time_Seconds = Delta;
        }

        void CreateGraphicsResource()
        {

            var graphics = Get<Graphics>();
            var cache = Get<ResourceCache>();

            uiShader_ = new Shader(
                "UI",
                new Pass("ImGui.vert.spv", "ImGui.frag.spv")
                {
                    ResourceLayout = new ResourceLayout(
                        new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                        new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
                    )
                }
            );

            _projMatrixBuffer = GraphicsBuffer.CreateUniform<Matrix>();

            pipeline_ = new Pipeline
            {
                VertexInputState = Pos2dTexColorVertex.Layout,
                DepthTestEnable = false,
                DepthWriteEnable = false,
                CullMode = CullModes.None,
                BlendMode = BlendMode.Alpha,
                DynamicStateCreateInfo = new PipelineDynamicStateCreateInfo(DynamicState.Scissor)
            };

            _vertexBuffer = GraphicsBuffer.CreateDynamic<Pos2dTexColorVertex>(BufferUsages.VertexBuffer, 4046);
            _indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsages.IndexBuffer, 4046);

 
        }

        void FontStash(IntPtr Atlas)
        {
        }

        int CreateTextureHandle(int w, int h, IntPtr image)
        {
            var tex = Texture.Create2D(w, h, 4, image);
            textures.Add(tex);

            fontTex_ = tex;
            return textures.Count - 1;
        }

        static void TestWindow(float X, float Y)
        {
            const nk_panel_flags Flags = nk_panel_flags.NK_WINDOW_BORDER | nk_panel_flags.NK_WINDOW_MOVABLE | nk_panel_flags.NK_WINDOW_SCALABLE |
                nk_panel_flags.NK_WINDOW_MINIMIZABLE | nk_panel_flags.NK_WINDOW_SCROLL_AUTO_HIDE;

            ImGUI.Window("Test Window", X, Y, 200, 200, Flags, () =>
            {
                ImGUI.LayoutRowDynamic(35);

                for (int i = 0; i < 5; i++)
                    if (ImGUI.ButtonLabel("Some Button " + i))
                        Console.WriteLine("You pressed button " + i);

                if (ImGUI.ButtonLabel("Exit"))
                    Environment.Exit(0);
            });
        }

        private void UpdateGUI()
        {
            SetDeltaTime(Time.Delta);

            HandleInput();

            //TestWindow(100, 100);

            this.SendGlobalEvent(new GUIEvent());
        }
       

        void RenderGUI(RenderPass renderPass)
        {
            nk_convert_result R = (nk_convert_result)nk_convert(ctx, Commands, Vertices, Indices, ConvertCfg);
            if (R != nk_convert_result.NK_CONVERT_SUCCESS)
                throw new Exception(R.ToString());

            Pos2dTexColorVertex* VertsPtr = (Pos2dTexColorVertex*)Vertices->memory.ptr;
            ushort* IndicesPtr = (ushort*)Indices->memory.ptr;

            if (ctx->draw_list.cmd_count == 0)
            {
                return;
            }

            if (ctx->draw_list.vertex_count > _vertexBuffer.Count)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = GraphicsBuffer.CreateDynamic<Pos2dTexColorVertex>(BufferUsages.VertexBuffer,
                    (int)(1.5f * ctx->draw_list.vertex_count));
            }

            if (ctx->draw_list.element_count > _indexBuffer.Count)
            {
                _indexBuffer.Dispose();
                _indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsages.IndexBuffer, 
                    (int)(1.5f * ctx->draw_list.element_count));
            }

            _vertexBuffer.SetData((IntPtr)VertsPtr, 0, (int)ctx->draw_list.vertex_count * Interop.SizeOf<Pos2dTexColorVertex>());
            _indexBuffer.SetData((IntPtr)IndicesPtr, 0, (int)ctx->draw_list.element_count * sizeof(ushort));

            var graphics = Get<Graphics>();
            Matrix proj = Matrix.OrthoOffCenterLH(
                     0f,
                     graphics.Width,
                     graphics.Height,
                     0.0f,
                     -1.0f,
                     1.0f, false);

            _projMatrixBuffer.SetData(ref proj);

            renderPass.BindVertexBuffer(_vertexBuffer, 0);
            renderPass.BindIndexBuffer(_indexBuffer, 0, IndexType.UInt16);
            renderPass.BindGraphicsPipeline(pipeline_, uiShader_, resourceSet_);

            uint Offset = 0;

            nk_draw_foreach(ctx, Commands, (Cmd) =>
            {
                if (Cmd->elem_count == 0)
                    return;

                int xMin = (int)Cmd->clip_rect.x;
                if (xMin < 0)
                {
                    xMin = 0;
                }
                int xMax = (int)(Cmd->clip_rect.x + Cmd->clip_rect.w);
                if (xMax > graphics.Width)
                {
                    xMax = graphics.Width;
                }

                int yMin = (int)Cmd->clip_rect.y;
                if (yMin < 0)
                {
                    yMin = 0;
                }
                int yMax = (int)(Cmd->clip_rect.y + Cmd->clip_rect.h);
                if (yMax > graphics.Height)
                {
                    yMax = graphics.Height;
                }

                renderPass.SetScissor(new Rect2D(xMin, yMin, xMax - xMin, yMax - yMin));
                renderPass.DrawIndexed((int)Cmd->elem_count, 1, (int)Offset, 0, 0);

                Offset += Cmd->elem_count;
            });


            nk_draw_list* list = &ctx->draw_list;
            if (list != null)
            {
                if (list->buffer != null)
                    nk_buffer_clear(list->buffer);

                if (list->vertices != null)
                    nk_buffer_clear(list->vertices);

                if (list->elements != null)
                    nk_buffer_clear(list->elements);
            }

            nk_clear(ctx);
        }

        void HandleInput()
        {
            var input = Get<Input>();
            var snapshot = input.InputSnapshot;

            nk_input_begin(ctx);

            var mousePos = snapshot.MousePosition;

            foreach (var me in snapshot.MouseEvents)
            {
                nk_input_button(ctx, (nk_buttons)me.MouseButton,
                    (int)me.X, (int)me.Y, me.Down ? 1 : 0);
            }

            foreach (var mme in snapshot.MouseMoveEvents)
            {
                nk_input_motion(ctx,
                    (int)mme.MousePosition.X, (int)mme.MousePosition.Y);
            }

            IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
            for (int i = 0; i < keyCharPresses.Count; i++)
            {
                char c = keyCharPresses[i];
                nk_input_unicode(ctx, c);
            }

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                var key = keyEvents[i];

                int down = key.Down ? 1 : 0;
                //const Uint8* state = SDL_GetKeyboardState(0);
                Key sym = key.Key;
                int modifiers = (key.Down && (key.Modifiers & ModifierKeys.Control) != 0) ? 1 : 0;

                if (sym == Key.RShift || sym == Key.LShift)
                    nk_input_key(ctx, nk_keys.NK_KEY_SHIFT, down);
                else if (sym == Key.Delete)
                    nk_input_key(ctx, nk_keys.NK_KEY_DEL, down);
                else if (sym == Key.Enter)
                    nk_input_key(ctx, nk_keys.NK_KEY_ENTER, down);
                else if (sym == Key.Tab)
                    nk_input_key(ctx, nk_keys.NK_KEY_TAB, down);
                else if (sym == Key.BackSpace)
                    nk_input_key(ctx, nk_keys.NK_KEY_BACKSPACE, down);
                else if (sym == Key.Home)
                {
                    nk_input_key(ctx, nk_keys.NK_KEY_TEXT_START, down);
                    nk_input_key(ctx, nk_keys.NK_KEY_SCROLL_START, down);
                }
                else if (sym == Key.End)
                {
                    nk_input_key(ctx, nk_keys.NK_KEY_TEXT_END, down);
                    nk_input_key(ctx, nk_keys.NK_KEY_SCROLL_END, down);
                }
                else if (sym == Key.PageDown)
                {
                    nk_input_key(ctx, nk_keys.NK_KEY_SCROLL_DOWN, down);
                }
                else if (sym == Key.PageUp)
                {
                    nk_input_key(ctx, nk_keys.NK_KEY_SCROLL_UP, down);
                }
                else if (sym == Key.Z)
                    nk_input_key(ctx, nk_keys.NK_KEY_TEXT_UNDO, modifiers);
                else if (sym == Key.R)
                    nk_input_key(ctx, nk_keys.NK_KEY_TEXT_REDO, modifiers);
                else if (sym == Key.C)
                    nk_input_key(ctx, nk_keys.NK_KEY_COPY, modifiers);
                else if (sym == Key.V)
                    nk_input_key(ctx, nk_keys.NK_KEY_PASTE, modifiers);
                else if (sym == Key.X)
                    nk_input_key(ctx, nk_keys.NK_KEY_CUT, modifiers);
                else if (sym == Key.B)
                    nk_input_key(ctx, nk_keys.NK_KEY_TEXT_LINE_START, modifiers);
                else if (sym == Key.E)
                    nk_input_key(ctx, nk_keys.NK_KEY_TEXT_LINE_END, modifiers);
                else if (sym == Key.Up)
                    nk_input_key(ctx, nk_keys.NK_KEY_UP, down);
                else if (sym == Key.Down)
                    nk_input_key(ctx, nk_keys.NK_KEY_DOWN, down);
                else if (sym == Key.Left)
                {
                    if ((key.Modifiers & ModifierKeys.Control) != 0)
                        nk_input_key(ctx, nk_keys.NK_KEY_TEXT_WORD_LEFT, down);
                    else nk_input_key(ctx, nk_keys.NK_KEY_LEFT, down);
                }
                else if (sym == Key.Right)
                {
                    if ((key.Modifiers & ModifierKeys.Control) != 0)
                        nk_input_key(ctx, nk_keys.NK_KEY_TEXT_WORD_RIGHT, down);
                    else nk_input_key(ctx, nk_keys.NK_KEY_RIGHT, down);
                }

            }
            
            nk_input_end(ctx);

        }

    }
}
