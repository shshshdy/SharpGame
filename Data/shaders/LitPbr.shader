Shader "LitPbr"
{
    Properties = {}

    Pass
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

            ResourceLayoutBinding "prefilteredMap"
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

        @VertexShader
        {
            #include "pbr_vs.glsl"
        }

        @PixelShader
        {
            #include "pbr_fs.glsl"
        }

    }

}
