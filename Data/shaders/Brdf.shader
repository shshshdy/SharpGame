Shader "Brdf"
{
	Properties = {}

	Pass
	{
		@ComputeShader
		{
			#include "spbrdf_cs.glsl"

		}

	}

}