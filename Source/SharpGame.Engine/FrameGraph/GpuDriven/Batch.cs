using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Batch
    {
        public Pass pass;

        public Buffer materials;

        public NativeList<Vulkan.VkDescriptorBufferInfo> buffers;

        public NativeList<DescriptorImageInfo> diffMaps = new NativeList<DescriptorImageInfo>();
        public NativeList<DescriptorImageInfo> normMaps = new NativeList<DescriptorImageInfo>();
        public NativeList<DescriptorImageInfo> specMaps = new NativeList<DescriptorImageInfo>();

        public List<Geometry> geometries = new List<Geometry>();

    }
}
