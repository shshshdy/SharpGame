Shader "Debug"
{
	Pass
	{
		CullMode = None
		DepthTest = true

		@VertexShader
		{
			#include "debug.vert"
		}
		
		@PixelShader
		{
			#include "debug.frag"
		}

	}

	Pass "NoDepth"
	{
		CullMode = None
		DepthTest = false

		@VertexShader
		{
			#include "debug.vert"
		}

		@PixelShader
		{
			#include "debug.frag"
		}

	}

}
