Shader "SpMap"
{
	Pass
	{
		PushConstant level
		{
			StageFlags = Compute
			Offset = 0
			Size = 4
		}

		PushConstant roughness
		{
			StageFlags = Compute
			Offset = 4
			Size = 4
		}

		@ComputeShader
		{
			#include "spmap_cs.glsl"
		}

	}

}
