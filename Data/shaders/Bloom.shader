Shader "bloom"
{	
	Pass "brightness_mask"
	{
		CullMode = None
		DepthTest = false

		@VertexShader
		{
			#include "post/fullscreen.vert"
		}

		@PixelShader
		{
			#include "post/brightness_mask.frag"
		}

	}

	Pass "gaussian_blur"
	{
		CullMode = None
		DepthTest = false

		@VertexShader
		{
			#include "post/fullscreen.vert"
		}

		@PixelShader
		{
			#include "post/gaussian_blur.frag"
		}

	}

	Pass "merge"
	{
		CullMode = None
		DepthTest = false

		@VertexShader
		{
			#include "post/fullscreen.vert"
		}

		@PixelShader
		{
			#include "post/merge.frag"
		}

	}


}
