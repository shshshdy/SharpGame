Shader "Brdf"
{
	Properties = {}

	Pass
	{

		ResourceLayout
		{
			ResourceLayoutBinding
			{
				DescriptorType = StorageImage
				StageFlags = Compute
			}
		}

		@ComputeShader
		{
			#include "spbrdf_cs.glsl"

		}

	}

}