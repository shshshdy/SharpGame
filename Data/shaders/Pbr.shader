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

			ResourceLayoutBinding "ObjectVS"
			{
				DescriptorType = UniformBufferDynamic
				StageFlags = Vertex
			}
		}

		ResourceLayout
		{
			ResourceLayoutBinding CameraPS
			{
				DescriptorType = UniformBuffer
				StageFlags = Fragment
			}

			ResourceLayoutBinding LightPS
			{
				DescriptorType = UniformBuffer
				StageFlags = Fragment
			}
		}
			
		ResourceLayout
		{
			ResourceLayoutBinding "prefilteredMap"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}

			ResourceLayoutBinding "samplerIrradiance"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}

			ResourceLayoutBinding "samplerBRDFLUT"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}

		}

		ResourceLayout PerMaterial
		{
			ResourceLayoutBinding "albedoMap"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}
		}
			
		ResourceLayout PerMaterial
		{
			ResourceLayoutBinding "normalMap"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}
		}

		ResourceLayout PerMaterial
		{
			ResourceLayoutBinding "metallicMap"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}
		}

		ResourceLayout PerMaterial
		{
			ResourceLayoutBinding "roughnessMap"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}
		}

		ResourceLayout PerMaterial
		{
			ResourceLayoutBinding "aoMap"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}
		}

		@VertexShader
		{
			#version 450

            #include "UniformsVS.glsl"

            layout(location = 0) in vec3 inPos;
            layout(location = 1) in vec3 inNormal;
            layout(location = 2) in vec3 inTangent;
            layout(location = 3) in vec3 inBitangent;
            layout(location = 4) in vec2 inUV;

            layout(location = 0) out vec3 outWorldPos;
            layout(location = 1) out vec2 outUV;
            layout(location = 2) out mat3 outTBN;
            

			out gl_PerVertex 
			{
				vec4 gl_Position;
			};

			void main() 
			{
				vec3 locPos = vec3(Model * vec4(inPos, 1.0));
				outWorldPos = locPos;
                outTBN = mat3(Model) * mat3(inTangent, inBitangent, inNormal);

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

			layout (location = 0) in vec3 inWorldPos;
			layout (location = 1) in vec2 inUV;
			layout (location = 2) in mat3 inTBN;

			layout (location = 0) out vec4 outColor;

			void main()
			{
                vec3 tangentNormal = texture(normalMap, inUV).xyz * 2.0 - 1.0;
                vec3 N = normalize(inTBN * tangentNormal);
				vec3 V = normalize(CameraPos - inWorldPos);
				vec3 R = reflect(-V, N); 

				float metallic = texture(metallicMap, inUV).r;
				float roughness = texture(roughnessMap, inUV).r;

				vec3 albedo = ALBEDO;

				vec3 F0 = vec3(0.4); 
				F0 = mix(F0, albedo, metallic);
                
				vec3 Lo = vec3(0.0);

                //for (int i = 0; i < 3; i++)
                {
                    vec3 L = -SunlightDir;
                    Lo += specularContribution(albedo, L, V, N, F0, metallic, roughness);
                }
                /*
                {
                    vec3 L = vec3(1, 0, 0);
                    Lo += specularContribution(albedo, L, V, N, F0, metallic, roughness);
                }

                {
                    vec3 L = vec3(0, 0, 1);
                    Lo += specularContribution(albedo, L, V, N, F0, metallic, roughness);
                }*/

				vec2 brdf = texture(samplerBRDFLUT, vec2(max(dot(N, V), 0.0), roughness)).rg;
				vec3 reflection = prefilteredReflection(R, roughness).rgb;
                vec3 irradiance = texture(samplerIrradiance, N).rgb;

				// Diffuse based on irradiance
				vec3 diffuse = irradiance * albedo;

				vec3 F = F_SchlickR(max(dot(N, V), 0.0), F0, roughness);

				// Specular reflectance
				vec3 specular = reflection * (F * brdf.x + brdf.y);
              
				// Ambient part
				vec3 kD = 1.0 - F;
				kD *= 1.0 - metallic;	  
				vec3 ambient = (kD * diffuse + specular) * texture(aoMap, inUV).rrr;
                
				vec3 color = ambient + Lo;

				// Tone mapping
				color = Uncharted2Tonemap(color * 1);
				color = color * (1.0f / Uncharted2Tonemap(vec3(11.2f)));	
				// Gamma correction
				color = pow(color, vec3(1.0f / 2.2));

				outColor = vec4(color, 1.0);
			}

		}
		
	}

}
