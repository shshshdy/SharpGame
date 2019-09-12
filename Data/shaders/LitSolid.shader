Shader "LitSolid"
{
	Properties
	{
		DiffMap = "White"
	}

	Pass "main"
	{
        CullMode = Back
		FrontFace = Clockwise
				
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

    Pass "shadow"
    {
        CullMode = Back
        FrontFace = Clockwise

        PushConstant model
        {
            StageFlags = Vertex
            Offset = 0
            Size = 68
        }

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
