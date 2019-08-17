Shader "Brdf"
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
		}

		@ComputeShader
		{
			#include "spbrdf_cs.glsl"

		}

	}

}