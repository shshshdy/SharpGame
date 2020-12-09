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

    Pass "gbuffer"
    {
        CullMode = Back

        @VertexShader
        {
            #include "cluster_gbuffer.vert"
        }

        @PixelShader
        {
            #define ALPHA_TEST
            #include "cluster_gbuffer.frag"
        }
    }

	Pass "clustering"
	{
		CullMode = Back

		FrontFace = CounterClockwise
		//DepthWrite = false

		@VertexShader
		{
            #include "clustering.vert"
		}

		@PixelShader
		{
            #include "clustering.frag"
		}

	}

	Pass "cluster_forward"
	{
		CullMode = Back

		FrontFace = CounterClockwise
		//DepthWrite = false

		@VertexShader
		{
            #include "cluster_forward.vert"
		}

		@PixelShader
		{
            #include "cluster_forward.frag"
		}

	}

    Pass "shadow"
    {
        CullMode = Front
        FrontFace = CounterClockwise

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
