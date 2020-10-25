using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Subpass
    {
        public virtual void Init()
        {
        }
        
        public virtual void Update(RenderView view)
        {
        }

        public virtual void Draw(RenderView view, uint subpass)
        {
        }
    }
}
