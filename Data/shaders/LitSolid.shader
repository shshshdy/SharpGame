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
#define ALPHA_TEST
            #include "general.frag"
		}
		
	}

	Pass "early_z"
	{
		CullMode = Back
		
		FrontFace = CounterClockwise

		@VertexShader
		{
			#include "clustering.vert"
		}

		@PixelShader
		{  
			#include "clustering.frag"
		}

	}

    Pass "shadow"
    {
        CullMode = Front
        FrontFace = CounterClockwise

        PushConstant g_cascadeIndex
        {
            StageFlags = Vertex
            Offset = 0
            Size = 4
        }

        @VertexShader
        {
#define ALPHA_TEST
            #include "shadow.vert"
        }

        @PixelShader
        {
#define ALPHA_TEST            
            #include "shadow.frag"
        }

    }

}
