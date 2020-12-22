using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class FullScreenSubpass : Subpass
    {
        protected Pass pass;
        public PipelineResourceSet PipelineResourceSet { get; }

        public Action<PipelineResourceSet> onBindResource;

        public List<(StringID, uint, uint)> inputResources = new List<(StringID, uint, uint)>();

        public FullScreenSubpass(string fs, SpecializationInfo specializationInfo = null)
        {
            pass = ShaderUtil.CreatePass("shaders/post/fullscreen.vert", fs);
            pass.CullMode = VkCullModeFlags.None;
            pass.DepthTestEnable = false;
            pass.DepthWriteEnable = false;
            pass.PixelShader.SpecializationInfo = specializationInfo;

            PipelineResourceSet = new PipelineResourceSet(pass.PipelineLayout);
        }

        public StringID this[uint set, uint binding]
        {
            get
            {
                foreach (var (resId, resSet, resBind) in inputResources)
                {
                    if (set == resSet && binding == resBind)
                    {
                        return resId;
                    }
                }
                return StringID.Empty;
            }

            set
            {
                for(int i = 0; i < inputResources.Count; i++)
                {
                    if (set == inputResources[i].Item2 && binding == inputResources[i].Item3)
                    {
                        inputResources[i] = (value, set, binding);
                        return;
                    }
                }

                inputResources.Add((value, set, binding));

            }
        }

        public bool AddtiveMode
        {
            set
            {
                if(value)
                {
                    pass.BlendMode = BlendMode.Add;
                }
                else
                {
                    pass.BlendMode = BlendMode.Replace;
                }
            }

        }

        public void SetResource(uint set, uint binding, IBindableResource res)
        {
            PipelineResourceSet.SetResource(set, binding, res);
        }

        public override void Init()
        {
            CreateResources();
            BindResources();
        }

        public override void DeviceReset()
        {
            BindResources();
        }

        protected virtual void CreateResources()
        {
        }

        protected void BindResources()
        {
            foreach(var (resId, set, bind) in inputResources)
            {
                var res = Renderer.Get(resId);
                if (res != null)
                    PipelineResourceSet.ResourceSet[set].BindResource(bind, res);
                else
                    Log.Warn("Cannot find res ", resId);
            }

            OnBindResources();

            onBindResource?.Invoke(PipelineResourceSet);
        }

        protected virtual void OnBindResources()
        {
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            DrawFullScreenQuad(cmd, FrameGraphPass.RenderPass, subpassIndex, pass, PipelineResourceSet.ResourceSet);
        }

        public void DrawFullScreenQuad(CommandBuffer cmd, RenderPass renderPass, uint subpass, Pass pass, Span<DescriptorSet> resourceSet)
        {
            var pipe = pass.GetGraphicsPipeline(renderPass, subpass, null);

            cmd.BindPipeline(VkPipelineBindPoint.Graphics, pipe);

            foreach (var rs in resourceSet)
            {
                cmd.BindGraphicsResourceSet(pass.PipelineLayout, rs.Set, rs);
            }

            cmd.Draw(3, 1, 0, 0);
        }

    }
}
