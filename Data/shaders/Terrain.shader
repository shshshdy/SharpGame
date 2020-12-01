Shader "Terrain"
{
	Pass
	{
		CullMode = None
		FrontFace = CounterClockwise

		@VertexShader
		{
			#include "Natural/terrain.vert"
		}
	/*
		@TessControl
		{
			#include "Natural/terrain.tesc"
		}
		
		@TessEvaluation
		{
			#include "Natural/terrain.tese"
		}*/
		
		@PixelShader
		{
			//#include "Natural/terrain.frag"

			#version 450

			layout (location = 0) in vec3 in_Normal;
			layout (location = 1) in vec2 in_TexCoord;
			layout (location = 0) out vec4 out_Color;

			void main()
            {
				out_Color = vec4(in_Normal, 1.0);
			}
		}

	}


}
