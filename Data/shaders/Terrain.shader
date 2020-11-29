Shader "Terrain"
{
	Pass
	{
		@VertexShader
		{
			#include "Natural/terrain.vert"
		}
		
		@TessControl
		{
			#include "Natural/terrain.tesc"
		}
		
		@TessEvaluation
		{
			#include "Natural/terrain.tese"
		}
		
		@PixelShader
		{
			#include "Natural/terrain.frag"
		}

	}


}
