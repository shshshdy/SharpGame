Shader "SkySphere"
{
	Pass "main"
	{
		CullMode = Front
		FrontFace = CounterClockwise
        DepthTest = false
        DepthWrite = false

		@VertexShader
		{
            #include "vegetation/skysphere.vert"
		}
		
		@PixelShader
		{
            #include "vegetation/skysphere.frag"
		}
		
	}

}
