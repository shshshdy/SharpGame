using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct Resizing
    {
    }

    public struct BeginRender
    {
    }

    public struct EndRender
    {
    }

    public struct BeginView
    {
        public RenderView view;
    }

    public struct EndView
    {
        public RenderView view;
    }

    public struct BeginRenderPass
    {
        public PassHandler renderPass;
    }

    public struct EndRenderPass
    {
        public PassHandler renderPass;
    }
}
