Shader "test"
{
	Properties = {}

	Pass "main"
	{
		CullMode = Back
		FrontFace = CounterClockwise

		ResourceLayout
		{
			ResourceLayoutBinding
			{
				binding	= 0
				descriptorType = UniformBuffer
				stageFlags = Vertex
				descriptorCount = 1
			}
		}

		ResourceLayout
		{
			ResourceLayoutBinding
			{
				binding = 0
				descriptorType = CombinedImageSampler
				stageFlags = Fragment
				descriptorCount = 1
			}
		}

		PushConstant
		{
			stageFlags = Vertex
			offset = 0
			size = 64		
		}
		
		@VertexShader
		{
			#version 450

			layout (binding = 0) uniform CameraVS
			{
				mat4 View;
				mat4 ViewInv;
				mat4 ViewProj;
				vec3 CameraPos;
				float NearClip;
				vec3 FrustumSize;
				float FarClip;
			};

			layout(push_constant) uniform PushConsts {
				mat4 model;
			};

			layout (location = 0) in vec3 in_Position;
			layout (location = 1) in vec3 in_Normal;
			layout (location = 2) in vec2 in_TexCoord;

			layout (location = 0) out vec2 out_TexCoord;

			out gl_PerVertex
			{
				vec4 gl_Position;
			};

			void main() {
				out_TexCoord = in_TexCoord;
				gl_Position = ViewProj * model* vec4(in_Position.xyz, 1.0);
			}

		}
		
		@PixelShader
		{
			#version 450

			layout (set = 1, binding = 0) uniform sampler2D sampler_Color;

			layout (location = 0) in vec2 in_TexCoord;

			layout (location = 0) out vec4 out_Color;

			void main() {
			   vec4 color = texture(sampler_Color, in_TexCoord);
			//out_Color = vec4(1,1,1,1);
				out_Color = vec4(color.rgb, 1.0);
			}

		}
		
	}

}
