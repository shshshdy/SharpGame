Shader "LitAlphaTest"
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
            #define ALPHA_MAP
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

        PushConstant g_cascadeIndex
        {
            StageFlags = Vertex
            Offset = 0
            Size = 4
        }

        @VertexShader
        {
#define ALPHA_TEST
#define ALPHA_MAP
            #include "shadow.vert"
        }

        @PixelShader
        {
#define ALPHA_TEST   
#define ALPHA_MAP         
            #include "shadow.frag"
        }

    }

}
