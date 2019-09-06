Shader "Shadow"
{
	Properties
    {
        DiffMap  = "White"
    }

	Pass "main"
	{
		CullMode = None
		FrontFace = CounterClockwise

        PushConstant position
        {
            StageFlags = Vertex
            Offset = 0
            Size = 16
        }

        PushConstant cascadeIndex
        {
            StageFlags = Vertex
            Offset = 16
            Size = 4
        }

		@VertexShader
		{
            #version 450

            layout(location = 0) in vec3 inPos;
            layout(location = 1) in vec2 inUV;

            // todo: pass via specialization constant
            #define SHADOW_MAP_CASCADE_COUNT 4

            layout(push_constant) uniform PushConsts{
                vec4 position;
                uint cascadeIndex;
            } pushConsts;

            layout(binding = 0) uniform UBO{
                mat4[SHADOW_MAP_CASCADE_COUNT] cascadeViewProjMat;
            } ubo;

            layout(location = 0) out vec2 outUV;

            out gl_PerVertex{
                vec4 gl_Position;
            };

            void main()
            {
                outUV = inUV;
                vec3 pos = inPos + pushConsts.position.xyz;
                gl_Position = ubo.cascadeViewProjMat[pushConsts.cascadeIndex] * vec4(pos, 1.0);
            }
		}
		
		@PixelShader
		{
            #version 450

            layout(set = 1, binding = 0) uniform sampler2D DiffMap;
            layout(location = 0) in vec2 inUV;

            void main()
            {
                float alpha = texture(DiffMap, inUV).a;
                if (alpha < 0.5) {
                    discard;
                }
            }
		}
		
	}

}
