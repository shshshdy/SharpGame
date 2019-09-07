Shader "Scene"
{
	Properties
    {
        DiffMap  = "White"
    }

	Pass "main"
	{
		CullMode = None
		FrontFace = CounterClockwise

		@VertexShader
		{		
            #include "scene.vert"
		}
		
		@PixelShader
		{
            #include "scene.frag"
		}
		
	}

}
