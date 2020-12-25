using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class FullScreenSubpass : Subpass
    {
        public Pass Pass { get; }
        public PipelineResourceSet PipelineResourceSet { get; }
        public DescriptorSet[] resourceSet { get; set; } = new DescriptorSet[0];

        public Action<PipelineResourceSet> onBindResource;

        private Dictionary<(uint, uint), StringID> inputResources { get; } = new Dictionary<(uint, uint), StringID>();
        private Dictionary<uint, StringID> inputResourceSets { get; } = new Dictionary<uint, StringID>();

        public FullScreenSubpass(string fs, SpecializationInfo specializationInfo = null)
        {
            Pass = ShaderUtil.CreatePass("shaders/post/fullscreen.vert", fs);
            Pass.CullMode = VkCullModeFlags.None;
            Pass.DepthTestEnable = false;
            Pass.DepthWriteEnable = false;
            Pass.PixelShader.SpecializationInfo = specializationInfo;

            PipelineResourceSet = new PipelineResourceSet(Pass.PipelineLayout);
        }

        public FullScreenSubpass(Pass pass)
        {
            Pass = pass;
            PipelineResourceSet = new PipelineResourceSet(pass.PipelineLayout);
        }

        public StringID this[uint set, uint binding]
        {
            get
            {
                if(inputResources.TryGetValue((set, binding), out var resId))
                {
                    return resId;                    
                }

                return StringID.Empty;
            }

            set
            {
                inputResources[(set, binding)] = value;
            }
        }

        public StringID this[uint set]
        {
            get
            {
                if (inputResourceSets.TryGetValue(set, out var res))
                {
                    return res;
                }

                return null;
            }

            set
            {
                inputResourceSets[set] = value;
            }
        }

        public bool AddtiveMode
        {
            set
            {
                if(value)
                {
                    Pass.BlendMode = BlendMode.Add;
                }
                else
                {
                    Pass.BlendMode = BlendMode.Replace;
                }
            }

        }

        public void SetResource(uint set, uint binding, StringID resId)
        {
            var res = Renderer.GetResource(resId);
            if (res != null)
                PipelineResourceSet.ResourceSet[set].BindResource(binding, res);
            else
                Log.Warn("Cannot find res ", resId);
        }

        public void SetResource(uint set, uint binding, IBindableResource res)
        {
            PipelineResourceSet.SetResource(set, binding, res);
        }

        public void SetResourceSet(uint set, StringID resId)
        {
            var res = Renderer.GetResourceSet(resId);
            if (res != null)
                SetResourceSet(set, res);
            else
                Log.Warn("Cannot find res ", resId);
        }

        public void SetResourceSet(uint set, DescriptorSet res)
        {               
            PipelineResourceSet.SetResourceSet(set, res.bindedRes);
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

            foreach (var it in inputResourceSets)
            {
                SetResourceSet(it.Key, it.Value);                
            }

            foreach (var it in inputResources)
            {
                var (set, bind) = it.Key;
                SetResource(set, bind, it.Value);
            }

            OnBindResources();

            onBindResource?.Invoke(PipelineResourceSet);
        }

        protected virtual void OnBindResources()
        {
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            var pipe = Pass.GetGraphicsPipeline(FrameGraphPass.RenderPass, SubpassIndex, null);

            cmd.BindPipeline(VkPipelineBindPoint.Graphics, pipe);

            foreach (var rs in resourceSet)
            {
                if(rs != null)
                    cmd.BindGraphicsResourceSet(Pass.PipelineLayout, rs.Set, rs);
            }
            
            PipelineResourceSet.BindGraphicsResourceSet(cmd);

            cmd.Draw(3, 1, 0, 0);

        }


    }
}
