Shader "LitSolid"
{
	Properties
	{
		DiffMap = "White"
	}

	Pass "main"
	{
        CullMode = Back
		FrontFace = CounterClockwise
				
		@VertexShader
		{
            #include "general.vert"
		}
		
		@PixelShader
		{
            #define SHADOW
            #include "general.frag"
		}
		
	}

	Pass "early_z"
	{
		CullMode = Back
		
		FrontFace = CounterClockwise

		@VertexShader
		{
#define ALPHA_TEST
			#include "clustering.vert"
		}

		@PixelShader
		{
#define ALPHA_TEST    
			#include "clustering.frag"
		}

	}

    Pass "shadow"
    {
        CullMode = Front
        FrontFace = CounterClockwise

        @VertexShader
        {
#define TEX_LOCATION 1
            #include "shadow.vert"
        }

        @PixelShader
        {
#define TEX_LOCATION 1            
            #include "shadow.frag"
        }

    }

}
