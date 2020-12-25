using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public interface DrawHelper
    {
        public RenderPipeline Renderer { get; }
        public Pass Pass { get; }
        public PipelineResourceSet PipelineResourceSet { get; }
        public Dictionary<(uint, uint), StringID> inputResources { get; }
        public Dictionary<uint, StringID> inputResourceSets { get; }


        public StringID this[uint set, uint binding]
        {
            get
            {
                if (inputResources.TryGetValue((set, binding), out var resId))
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

        public void SetResource(uint set, uint binding, IBindableResource res)
        {
            PipelineResourceSet.SetResource(set, binding, res);
        }

        public void SetResource(uint set, uint binding, StringID resId)
        {
            var res = Renderer.GetResource(resId);
            if (res != null)
                PipelineResourceSet.ResourceSet[set].BindResource(binding, res);
            else
                Log.Warn("Cannot find res ", resId);
        }

    }
}
