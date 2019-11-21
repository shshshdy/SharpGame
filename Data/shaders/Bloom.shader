Shader "bloom"
{	
	Pass "brightness_mask"
	{
		CullMode = None
		DepthTest = false

		@VertexShader
		{
		#include "fullscreen.vert"
		}

		@PixelShader
		{
		#include "brightness_mask.frag"
		}

	}

	Pass "gaussian_blur"
	{
		CullMode = None
		DepthTest = false

		@VertexShader
		{
			#include "fullscreen.vert"
		}

		@PixelShader
		{
			#include "gaussian_blur.frag"
		}

	}

	Pass "merge"
	{
		CullMode = None
		DepthTest = false

		@VertexShader
		{
			#include "fullscreen.vert"
		}

		@PixelShader
		{
			#include "merge.frag"
		}

	}


}
