using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace SharpGame.Samples
{
    // Virtual texture page as a part of the partially resident texture
    // Contains memory bindings, offsets and status information
    public struct VirtualTexturePage : IDisposable
    {
        public VkOffset3D offset;
        public VkExtent3D extent;
        public VkSparseImageMemoryBind imageMemoryBind;                            // Sparse image memory bind for this page
        public ulong size;                                                  // Page (memory) size in bytes
        public uint mipLevel;                                                  // Mip level that this page belongs to
        public uint layer;                                                     // Array layer that this page belongs to
        public uint index;

        public bool resident()
        {
            return (imageMemoryBind.memory != VkDeviceMemory.Null);
        }

        public void allocate(uint memoryTypeIndex)
        {
            if (imageMemoryBind.memory != VkDeviceMemory.Null)
            {
                return;
            }

            imageMemoryBind = new VkSparseImageMemoryBind();

            var allocInfo = VkMemoryAllocateInfo.New();
            allocInfo.allocationSize = size;
            allocInfo.memoryTypeIndex = memoryTypeIndex;
            imageMemoryBind.memory = Device.AllocateMemory(ref allocInfo);

            // Sparse image memory binding
            imageMemoryBind.subresource = new VkImageSubresource
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = mipLevel,
                arrayLayer = layer,
            };

            imageMemoryBind.extent = extent;
            imageMemoryBind.offset = offset;
        }

        public void Dispose()
        {
            if (imageMemoryBind.memory != VkDeviceMemory.Null)
            {
                Device.FreeMemory(imageMemoryBind.memory);
                imageMemoryBind.memory = VkDeviceMemory.Null;
            }
        }
    }

    // Virtual texture object containing all pages
    public class VirtualTexture : IDisposable
    {
        Image image;                                                      // Texture image handle
        BindSparseInfo bindSparseInfo;                                    // Sparse queue binding information
        Vector<VirtualTexturePage> pages = new Vector<VirtualTexturePage>();                              // Contains all virtual pages of the texture
        Vector<VkSparseImageMemoryBind> sparseImageMemoryBinds = new Vector<VkSparseImageMemoryBind>();   // Sparse image memory bindings of all memory-backed virtual tables
        Vector<VkSparseMemoryBind> opaqueMemoryBinds = new Vector<VkSparseMemoryBind>();                  // Sparse ópaque memory bindings for the mip tail (if present)
        SparseImageMemoryBindInfo[] imageMemoryBindInfo;                    // Sparse image memory bind info
        SparseImageOpaqueMemoryBindInfo[] opaqueMemoryBindInfo;             // Sparse image opaque memory bind info (mip tail)
        uint mipTailStart;                                              // First mip level in mip tail
        VkSparseImageMemoryRequirements sparseImageMemoryRequirements; 
        uint memoryTypeIndex;                                           

        // @todo: comment
        public struct MipTailInfo
        {
            public bool singleMipTail;
            public bool alingedMipSize;
        }

        public MipTailInfo mipTailInfo;

        public ref VirtualTexturePage addPage(VkOffset3D offset, VkExtent3D extent, ulong size, uint mipLevel, uint layer)
        {
            VirtualTexturePage newPage = new VirtualTexturePage();
            newPage.offset = offset;
            newPage.extent = extent;
            newPage.size = size;
            newPage.mipLevel = mipLevel;
            newPage.layer = layer;
            newPage.index = (uint)pages.Count;
            newPage.imageMemoryBind = new VkSparseImageMemoryBind();
            newPage.imageMemoryBind.offset = offset;
            newPage.imageMemoryBind.extent = extent;
            pages.Add(newPage);
            return ref pages.Back();
            
        }

        public void updateSparseBindInfo()
        {
            // Update list of memory-backed sparse image memory binds
            sparseImageMemoryBinds.Clear();
            foreach (var page in pages)
            {
                sparseImageMemoryBinds.Add(page.imageMemoryBind);
            }

            // Image memory binds
            imageMemoryBindInfo = new[] { new SparseImageMemoryBindInfo(image, sparseImageMemoryBinds) };

            // Opaque image memory binds for the mip tail
            opaqueMemoryBindInfo = new[] { new SparseImageOpaqueMemoryBindInfo(image, opaqueMemoryBinds) };

            bindSparseInfo = new BindSparseInfo(null, null, opaqueMemoryBindInfo, imageMemoryBindInfo, null);
        }

        public void Dispose()
        {
            foreach (var page in pages)
            {
                page.Dispose();
            }

            foreach (var bind in opaqueMemoryBinds)
            {
                Device.FreeMemory(bind.memory);
            }
        }
    }

    [SampleDesc(sortOrder = 10)]
    public class VirtualTextureSample : Sample
    {
        public override void Init()
        {
            scene = new Scene()
            {
                new Octree { },
                new DebugRenderer { },

                new Environment
                {
                    SunlightDir = glm.normalize(new vec3(-1.0f, -1.0f, 0.0f))
                },

                new Node("Camera", new vec3(0, 2, -10), glm.radians(10, 0, 0) )
                {
                    new Camera
                    {
                        NearClip = 0.5f,
                        FarClip = 100,
                    },

                },

            };

            camera = scene.GetComponent<Camera>(true);

            var importer = new AssimpModelReader
            {
                vertexComponents = new[]
                {
                    VertexComponent.Position,
                    VertexComponent.Texcoord
                }
            };

            {
                var node = scene.CreateChild("Plane");
                var staticModel = node.AddComponent<StaticModel>();
                var model = GeometryUtil.CreatePlaneModel(100, 100, 32, 32, true);
                staticModel.SetModel(model);
                var mat = Resources.Load<Material>("materials/Grass.material");
                mat.SetTexture("NormalMap", Texture.Blue);
                mat.SetTexture("SpecMap", Texture.Black);
                staticModel.SetMaterial(mat);
            }

            MainView.Attach(camera, scene);

        }


        Uint3 alignedDivision(in VkExtent3D extent, in VkExtent3D granularity)
        {
            Uint3 res = new Uint3();
            res.x = extent.width / granularity.width + ((extent.width % granularity.width != 0) ? 1u : 0u);
            res.y = extent.height / granularity.height + ((extent.height % granularity.height != 0) ? 1u : 0u);
            res.z = extent.depth / granularity.depth + ((extent.depth % granularity.depth != 0) ? 1u : 0u);
            return res;
        }

    }
}
