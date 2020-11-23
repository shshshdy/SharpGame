using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public class Pipeline : DisposeBase
    {
        internal VkPipeline handle;
        internal Pipeline(VkPipeline handle)
        {
            this.handle = handle;
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);

            Device.DestroyPipeline(handle);
        }
    }
}
