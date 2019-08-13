Shader "Pbr"
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
		}

		ResourceLayout PerMaterial
		{			
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

			layout (location = 0) in vec3 inPos;
			layout (location = 1) in vec3 inNormal;
			layout (location = 2) in vec2 inUV;

			layout (location = 0) out vec3 outWorldPos;
			layout (location = 1) out vec3 outNormal;
			layout (location = 2) out vec2 outUV;

			out gl_PerVertex 
			{
				vec4 gl_Position;
			};

			void main() 
			{
				vec3 locPos = vec3(Model * vec4(inPos, 1.0));
				outWorldPos = locPos;
				outNormal = mat3(Model) * inNormal;
				outUV = inUV;
				outUV.t = 1.0 - inUV.t;
				gl_Position =  ViewProj * vec4(outWorldPos, 1.0);
			}


		}
		
		@PixelShader
		{
			#version 450

            #include "UniformsPS.glsl"
            #include "Pbr.glsl"

			layout (binding = 1) uniform UBOParams {
				vec4 lights[4];
				float exposure;
				float gamma;
			} uboParams;

			layout (location = 0) in vec3 inWorldPos;
			layout (location = 1) in vec3 inNormal;
			layout (location = 2) in vec2 inUV;

			layout (binding = 2) uniform samplerCube samplerIrradiance;
			layout (binding = 3) uniform sampler2D samplerBRDFLUT;
			layout (binding = 4) uniform samplerCube prefilteredMap;

			layout (binding = 5) uniform sampler2D albedoMap;
			layout (binding = 6) uniform sampler2D normalMap;
			layout (binding = 7) uniform sampler2D aoMap;
			layout (binding = 8) uniform sampler2D metallicMap;
			layout (binding = 9) uniform sampler2D roughnessMap;


			layout (location = 0) out vec4 outColor;


			void main()
			{		
				vec3 N = perturbNormal();
				vec3 V = normalize(CameraPos - inWorldPos);
				vec3 R = reflect(-V, N); 

				float metallic = texture(metallicMap, inUV).r;
				float roughness = texture(roughnessMap, inUV).r;

				vec3 F0 = vec3(0.04); 
				F0 = mix(F0, ALBEDO, metallic);

				vec3 Lo = vec3(0.0);
				for(int i = 0; i < uboParams.lights[i].length(); i++) {
					vec3 L = normalize(uboParams.lights[i].xyz - inWorldPos);
					Lo += specularContribution(L, V, N, F0, metallic, roughness);
				}   
				
				vec2 brdf = texture(samplerBRDFLUT, vec2(max(dot(N, V), 0.0), roughness)).rg;
				vec3 reflection = prefilteredReflection(R, roughness).rgb;	
				vec3 irradiance = texture(samplerIrradiance, N).rgb;

				// Diffuse based on irradiance
				vec3 diffuse = irradiance * ALBEDO;	

				vec3 F = F_SchlickR(max(dot(N, V), 0.0), F0, roughness);

				// Specular reflectance
				vec3 specular = reflection * (F * brdf.x + brdf.y);

				// Ambient part
				vec3 kD = 1.0 - F;
				kD *= 1.0 - metallic;	  
				vec3 ambient = (kD * diffuse + specular) * texture(aoMap, inUV).rrr;
				
				vec3 color = ambient + Lo;

				// Tone mapping
				color = Uncharted2Tonemap(color * uboParams.exposure);
				color = color * (1.0f / Uncharted2Tonemap(vec3(11.2f)));	
				// Gamma correction
				color = pow(color, vec3(1.0f / uboParams.gamma));

				outColor = vec4(color, 1.0);
			}

		}
		
	}

}
