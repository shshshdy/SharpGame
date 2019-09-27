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

	}

	Pass "clustering"
	{
		CullMode = Back

		FrontFace = CounterClockwise
		DepthWrite = false

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
		DepthWrite = false

		PushConstant Material_properties
		{
			StageFlags = Fragment
			Offset = 0
			Size = 32
		}

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
