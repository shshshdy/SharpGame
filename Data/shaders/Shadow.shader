Shader "Shadow"
{
	Properties
    {
        DiffMap  = "White"
    }

	Pass "main"
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
            #version 450

            layout(location = 0) in vec3 inPos;
            layout(location = 1) in vec2 inUV;

            // todo: pass via specialization constant
            #define SHADOW_MAP_CASCADE_COUNT 4

            layout(push_constant) uniform PushConsts{
                mat4 model;
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
                vec4 pos = pushConsts.model * vec4(inPos, 1);
                gl_Position = ubo.cascadeViewProjMat[pushConsts.cascadeIndex] * pos;
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
