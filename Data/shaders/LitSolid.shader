Shader "LitSolid"
{
	Properties
	{
		DiffMap = "White"
	}

	Pass "main"
	{
        CullMode = Back
				
		@VertexShader
		{
            #include "general.vert"
		}
		
		@PixelShader
		{
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
            #include "shadow.vert"
        }

        @PixelShader
        {
            #include "shadow.frag"
        }

    }

}
