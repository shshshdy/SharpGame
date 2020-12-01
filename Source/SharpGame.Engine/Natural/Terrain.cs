using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{       
    // Shared values for tessellation control and evaluation stages
    public unsafe struct TessUBO
    {           
        public mat4 projection;
        public mat4 vkProjection;
        public mat4 modelview;
        public vec4 lightPos;
        public FixedArray6<Plane> frustumPlanes;
        public float displacementFactor;
        public float tessellationFactor;
        public vec2 viewportDim;
        // Desired size of tessellated quad patch edge
        public float tessellatedEdgeSize;
    };

    public class Terrain : Drawable
    {
        const uint PATCH_SIZE = 64;
        const float UV_SCALE = 1.0f;
     
        HeightMap heightMap;
        Texture colorMap;
        Material material;

        TessUBO uboTess;

        SharedBuffer ubTess;
        TerrainBatch batch;

        public DescriptorSetLayout dsLayout;
        public DescriptorSet dsTess;

        public Terrain()
        {
            batch = new TerrainBatch();
            batches = new[] { batch };

            material = new Material("shaders/Terrain.shader");

            uboTess = new TessUBO
            {
                lightPos = new vec4(-48.0f, -40.0f, 46.0f, 0.0f),
                displacementFactor = 32.0f,
                tessellationFactor = 0.75f,
                tessellatedEdgeSize = 20.0f
            };

            batch.material = material;
              
            ubTess = new SharedBuffer(VkBufferUsageFlags.UniformBuffer, (uint)Utilities.SizeOf<TessUBO>());           

            dsLayout = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.Fragment | VkShaderStageFlags.TessellationControl | VkShaderStageFlags.TessellationEvaluation),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment | VkShaderStageFlags.TessellationControl | VkShaderStageFlags.TessellationEvaluation),
                new DescriptorSetLayoutBinding(2, VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment | VkShaderStageFlags.TessellationControl | VkShaderStageFlags.TessellationEvaluation),
            };

            dsTess = new DescriptorSet(dsLayout);
            dsTess.Bind(0, ubTess);
        }

        bool wireframeMode = false;
        public bool WireframeMode
        {
            get => wireframeMode;

            set
            {
                if(value != wireframeMode)
                {
                    wireframeMode = value;
                    material.Shader.Main.FillMode = (value ? VkPolygonMode.Line : VkPolygonMode.Fill);
                    material.Shader.Main.MakeDirty();
                }

            }
        }

        // Generate a terrain quad patch for feeding to the tessellation control shader
        public void GenerateTerrain()
        {
            heightMap = new HeightMap("textures/terrain_heightmap_r16.ktx", PATCH_SIZE);

            const uint vertexCount = PATCH_SIZE * PATCH_SIZE;
            // We use the Vertex definition from the glTF model loader, so we can re-use the vertex input state
            VertexPosTexNorm[] vertices = new VertexPosTexNorm[vertexCount];

            const float wx = 2.0f;
            const float wy = 2.0f;

            for (uint x = 0; x < PATCH_SIZE; x++)
            {
                for (uint y = 0; y < PATCH_SIZE; y++)
                {
                    uint index = (x + y * PATCH_SIZE);
                    vertices[index].position[0] = x * wx + wx / 2.0f - PATCH_SIZE * wx / 2.0f;
                    vertices[index].position[1] = 0.0f;
                    vertices[index].position[2] = y * wy + wy / 2.0f - PATCH_SIZE * wy / 2.0f;
                    vertices[index].texcoord = glm.vec2((float)x / PATCH_SIZE, (float)y / PATCH_SIZE) * UV_SCALE;
                }
            }

            // Calculate normals from height map using a sobel filter
            float[,] heights = new float[3, 3];
            for (uint x = 0; x < PATCH_SIZE; x++)
            {
                for (uint y = 0; y < PATCH_SIZE; y++)
                {
                    // Get height samples centered around current position                    
                    for (int hx = -1; hx <= 1; hx++)
                    {
                        for (int hy = -1; hy <= 1; hy++)
                        {
                            heights[hx + 1, hy + 1] = heightMap.GetHeight((int)x + hx, (int)y + hy);
                        }
                    }

                    // Calculate the normal
                    vec3 normal = new vec3
                    {
                        // Gx sobel filter
                        x = heights[0, 0] - heights[2, 0] + 2.0f * heights[0, 1] - 2.0f * heights[2, 1] + heights[0, 2] - heights[2, 2],
                        // Gy sobel filter
                        z = heights[0, 0] + 2.0f * heights[1, 0] + heights[2, 0] - heights[0, 2] - 2.0f * heights[1, 2] - heights[2, 2]
                    };
                    // Calculate missing up component of the normal using the filtered x and y axis
                    // The first value controls the bump strength
                    normal.y = 0.25f * glm.sqrt(1.0f - normal.x * normal.x - normal.z * normal.z);

                    vertices[x + y * PATCH_SIZE].normal = glm.normalize(normal * glm.vec3(2.0f, 1.0f, 2.0f));
                }
            }

            // Indices
            const uint w = (PATCH_SIZE - 1);
            const uint indexCount = w * w * 4;
            uint[] indices = new uint[indexCount];
            for (uint x = 0; x < w; x++)
            {
                for (uint y = 0; y < w; y++)
                {
                    uint index = (x + y * w) * 4;
                    indices[index] = (x + y * PATCH_SIZE);
                    indices[index + 1] = indices[index] + PATCH_SIZE;
                    indices[index + 2] = indices[index + 1] + 1;
                    indices[index + 3] = indices[index] + 1;
                }
            }

            batch.geometry = new Geometry
            {
                VertexBuffer = Buffer.Create(VkBufferUsageFlags.VertexBuffer, vertices),
                IndexBuffer = Buffer.Create(VkBufferUsageFlags.IndexBuffer, indices),
                VertexLayout = VertexPosTexNorm.Layout,
            };

            batch.geometry.SetDrawRange(VkPrimitiveTopology.TriangleList, 0, indexCount, 0);
            batch.geometry.PrimitiveTopology = VkPrimitiveTopology.PatchList;


            colorMap = Resources.Instance.Load<Texture>("textures/terrain_texturearray_rgba.ktx");

            dsTess.Bind(1, heightMap.texture);
            dsTess.Bind(2, colorMap);
            dsTess.UpdateSets();
        }

        bool tessellation = true;
        public ref bool Tessellation => ref tessellation;
        public ref float TessellationFactor => ref uboTess.tessellationFactor;

        public override void UpdateGeometry(in FrameInfo frameInfo)
        {
            // Tessellation
            uboTess.projection = frameInfo.camera.Projection;
            uboTess.vkProjection = frameInfo.camera.VkProjection;
            uboTess.modelview = frameInfo.camera.View * new mat4(1.0f);
            uboTess.lightPos.y = -0.5f - uboTess.displacementFactor;
            uboTess.viewportDim = new vec2(frameInfo.viewSize.x, frameInfo.viewSize.y);

            for(int i = 0; i < 6; i++)
            {
                uboTess.frustumPlanes[i] = frameInfo.camera.Frustum.GetPlane(i);
            }

            float savedFactor = uboTess.tessellationFactor;
            if (!tessellation)
            {
                // Setting this to zero sets all tessellation factors to 1.0 in the shader
                uboTess.tessellationFactor = 0.0f;
            }

            ubTess.SetData(ref uboTess);

            if (!tessellation)
            {
                uboTess.tessellationFactor = savedFactor;
            }

        }

        public override void UpdateBatches(in FrameInfo frame)
        {
            ref mat4 worldTransform = ref node_.WorldTransform;
            batch.worldTransform = node_.worldTransform_;
            batch.numWorldTransforms = 1;
            batch.dsTess = dsTess;

        }

        public class TerrainBatch : SourceBatch
        {
            public DescriptorSet dsTess;
            public override void Draw(CommandBuffer cb, Span<ConstBlock> pushConsts, DescriptorSet resourceSet, Span<DescriptorSet> resourceSet1, Pass pass)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, dsTess);
                material.Bind(pass.passIndex, cb);
                geometry.Draw(cb);
            }

        }

    }

    public class HeightMap
    {
        ushort[] heightdata;
        uint dim;
        uint scale;
        public Texture texture;
        public HeightMap(string filename, uint patchsize)
        {
            KtxTextureReader importer = new KtxTextureReader();
            importer.Format = VkFormat.R16UNorm;
            texture = importer.Load(filename);
            dim = texture.width;
            heightdata = new ushort[dim * dim];
            var imgData = texture.GetData(0, 0, 0);
            System.Buffer.BlockCopy(imgData, 0, heightdata, 0, imgData.Length);
            scale = dim / patchsize;

        }

        public float GetHeight(int x, int y)
        {
            Int2 rpos = new Int2(x * (int)scale, y * (int)scale);
            rpos.x = Math.Max(0, Math.Min(rpos.x, (int)dim - 1));
            rpos.y = Math.Max(0, Math.Min(rpos.y, (int)dim - 1));
            rpos /= (int)scale;
            return (heightdata[(rpos.x + rpos.y * dim) * scale]) / 65535.0f;
        }
    }
}
