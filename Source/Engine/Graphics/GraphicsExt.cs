using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public static class GraphicsExt
    {
        public static PipelineRasterizationStateCreateInfo RasterizationState(bool depthClampEnable = false, bool rasterizerDiscardEnable = false, PolygonMode polygonMode = PolygonMode.Fill, CullModes cullMode = CullModes.Back, FrontFace frontFace = FrontFace.Clockwise, bool depthBiasEnable = false, float depthBiasConstantFactor = 0, float depthBiasClamp = 0, float depthBiasSlopeFactor = 0, float lineWidth = 1)
        {
            return new PipelineRasterizationStateCreateInfo(depthClampEnable, rasterizerDiscardEnable, polygonMode, cullMode, frontFace, depthBiasEnable, depthBiasConstantFactor, depthBiasClamp, depthBiasSlopeFactor, lineWidth);
        }
    }
}
