Shader "IrMap"
{
	Properties = {}

	Pass
	{
		@ComputeShader
		{
			#include "irmap_cs.glsl"
		}

	}

}
