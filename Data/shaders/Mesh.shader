Shader "UI"
{
	Properties = {}

	Pass "main"
	{
				
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
				binding	= 0
				descriptorType = UniformBuffer
				stageFlags = Vertex
				descriptorCount = 1
			}

			ResourceLayoutBinding
			{
				binding = 1
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

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable
			
			#include "UniformsVS.glsl"

			layout (location = 0) in vec3 inPos;
			layout (location = 1) in vec3 inNormal;
			layout (location = 2) in vec2 inUV;
			layout (location = 3) in vec4 inColor;

			layout(push_constant) uniform PushConsts {
				mat4 model;
			};

			layout (set = 1, binding = 0) uniform UBO 
			{
				vec4 lightPos;
			};

			layout (location = 0) out vec3 outNormal;
			layout (location = 1) out vec4 outColor;
			layout (location = 2) out vec2 outUV;
			layout (location = 3) out vec3 outViewVec;
			layout (location = 4) out vec3 outLightVec;

			out gl_PerVertex
			{
				vec4 gl_Position;
			};

			void main() 
			{
				outNormal = inNormal;
				outColor = inColor;
				outUV = inUV;
				
				vec4 pos = model * vec4(inPos, 1.0);

				gl_Position = ViewProj * pos;
				
				outNormal = mat3(model) * inNormal;
				vec3 lPos = lightPos.xyz;
				outLightVec = lPos - pos.xyz;
				outViewVec = CameraPos.xyz-pos.xyz;		
			}


		}
		
		@PixelShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable

			layout (set = 1, binding = 1) uniform sampler2D samplerColorMap;

			layout (location = 0) in vec3 inNormal;
			layout (location = 1) in vec4 inColor;
			layout (location = 2) in vec2 inUV;
			layout (location = 3) in vec3 inViewVec;
			layout (location = 4) in vec3 inLightVec;

			layout (location = 0) out vec4 outFragColor;

			void main() 
			{
				vec4 color = texture(samplerColorMap, inUV) * inColor;

				vec3 N = normalize(inNormal);
				vec3 L = normalize(inLightVec);
				vec3 V = normalize(inViewVec);
				vec3 R = reflect(-L, N);
				vec3 diffuse = max(dot(N, L), 0.0) * inColor.xyz;
				vec3 specular = pow(max(dot(R, V), 0.0), 16.0) * vec3(0.75);
				outFragColor = vec4(diffuse * color.rgb + specular, 1.0);		
			}


		}
		
	}

}
