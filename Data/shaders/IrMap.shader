Shader "IrMap"
{
	Properties = {}

	Pass
	{
		ResourceLayout
		{
			ResourceLayoutBinding
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Compute
			}

			ResourceLayoutBinding
			{
				DescriptorType = StorageImage
				StageFlags = Compute
			}
		}

		@ComputeShader
		{
			#include "irmap_cs.glsl"
		}

	}

}
