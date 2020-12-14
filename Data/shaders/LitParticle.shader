Shader "LitParticle"
{
	Properties
	{
		DiffMap = "White"
	}

	Pass "main"
	{
        CullMode = None
		BlendMode = Alpha
        DepthWrite = false

        @VertexShader
        {
            #include "general.vert"
        }

        @PixelShader
        {
            #include "general.frag"
        }
		
	}

    Pass "clustering"
    {
        CullMode = None
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
        CullMode = None
        BlendMode = Alpha
        DepthWrite = false

        @VertexShader
        {
            #include "cluster_forward.vert"
        }

        @PixelShader
        {
#define TRANSLUCENT
            #include "cluster_forward.frag"
        }

    }

}
