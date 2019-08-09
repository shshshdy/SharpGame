Shader "Unlit"
{
	Properties = {}

	Pass "main"
	{
		CullMode = None
		FrontFace = CounterClockwise

		ResourceLayout
		{
			ResourceLayoutBinding "CameraVS"
			{
				DescriptorType = UniformBuffer
				StageFlags = Vertex
			}

			ResourceLayoutBinding "ObjectVS"
			{
				DescriptorType = UniformBufferDynamic
				StageFlags = Vertex
			}
		}

		ResourceLayout
		{
			Dynamic = true
			
			ResourceLayoutBinding "DiffMap"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}
		}

		@VertexShader
		{
			#version 450
			
			#include "UniformsVS.glsl"

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
				gl_Position = ViewProj * Model* vec4(in_Position.xyz, 1.0);
			}

		}
		
		@PixelShader
		{
			#version 450

			layout (set = 1, binding = 0) uniform sampler2D DiffMap;

			layout (location = 0) in vec2 in_TexCoord;

			layout (location = 0) out vec4 out_Color;

			void main() {
			   
				vec4 color = texture(DiffMap, in_TexCoord);
				out_Color = vec4(color.rgb, 1.0);
			}

		}
		
	}

}
