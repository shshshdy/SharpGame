Shader "Reflect"
{
	Properties = {}

	Pass "main"
	{
		CullMode = None
		FrontFace = CounterClockwise

		PushConstant lodBias
		{
			StageFlags = Vertex
			Offset = 0
			Size = 4
		}
		
		@VertexShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable

			#include "UniformsVS.glsl"

			layout(push_constant) uniform PushConsts{
				float lodBias;
			};

			layout (location = 0) in vec3 inPos;
			layout (location = 1) in vec3 inNormal;

			layout (location = 0) out vec3 outPos;
			layout (location = 1) out vec3 outNormal;
			layout (location = 2) out float outLodBias;
			layout (location = 3) out vec3 outViewVec;
			layout (location = 4) out vec3 outLightVec;
			layout (location = 5) out mat4 outInvModelView;
			
			out gl_PerVertex 
			{
				vec4 gl_Position;
			};

			void main() 
			{
				mat4 worldView = View * Model;
				gl_Position = ViewProj * Model * vec4(inPos.xyz, 1.0);
				
				outPos = vec3(worldView * vec4(inPos, 1.0));
				outNormal = mat3(worldView) * inNormal;

				outLodBias = lodBias;
				
				outInvModelView = inverse(worldView);
				
				vec3 lightPos = vec3(0.0f, 5.0f, -5.0f);
				outLightVec = lightPos.xyz - outPos.xyz;
				outViewVec = -outPos.xyz;		
			}


		}
		
		@PixelShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable

			layout (set = 1, binding = 0) uniform samplerCube samplerColor;

			layout (location = 0) in vec3 inPos;
			layout (location = 1) in vec3 inNormal;
			layout (location = 2) in float inLodBias;
			layout (location = 3) in vec3 inViewVec;
			layout (location = 4) in vec3 inLightVec;
			layout (location = 5) in mat4 inInvModelView;
			
			layout (location = 0) out vec4 outFragColor;

			void main() 
			{
				vec3 cI = normalize (inPos);
				vec3 cR = reflect(cI, normalize(inNormal));

				cR = vec3(inInvModelView * vec4(cR, 0.0));
				cR.y *= -1.0;

				vec4 color = texture(samplerColor, cR, inLodBias);

				vec3 N = normalize(inNormal);
				vec3 L = normalize(inLightVec);
				vec3 V = normalize(inViewVec);
				vec3 R = reflect(-L, N);
				vec3 ambient = vec3(0.5)*color.rgb;
				vec3 diffuse = max(dot(N, L), 0.0) * vec3(1.0);
				vec3 specular = pow(max(dot(R, V), 0.0), 16.0) * vec3(0.5);
				outFragColor = vec4(ambient + diffuse * color.rgb + specular, 1.0);		
			}

		}
		
	}

}
