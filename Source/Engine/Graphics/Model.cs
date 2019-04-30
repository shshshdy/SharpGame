using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame
{

    /// Vertex buffer morph data.
    public struct VertexBufferMorph
    {
        /// Vertex elements.
        public uint elementMask_;
        /// Number of vertices.
        public int vertexCount_;
        /// Morphed vertices data size as bytes.
        public int dataSize_;
        /// Morphed vertices. Stored packed as <index, data> pairs.
        public byte[] morphData_;
    };

    /// Definition of a model's vertex morph.
    public struct ModelMorph
    {
        /// Morph name.
        public StringID name_;
        /// Current morph weight.
        public float weight_;
        /// Morph data per vertex buffer.
        public Dictionary<int, VertexBufferMorph> buffers_;
    };

    [DataContract]
    public class Model : Resource
    {
        /// Vertex buffers.
        public GraphicsBuffer[] VertexBuffers => vertexBuffers_;
        GraphicsBuffer[] vertexBuffers_;

        /// Index buffers.
        public GraphicsBuffer[] IndexBuffers => indexBuffers_;
        GraphicsBuffer[] indexBuffers_;

        /// Bounding box.
        [DataMember]
        public BoundingBox BoundingBox { get => boundingBox_; }
        private BoundingBox boundingBox_;

        /// Skeleton.
        public Skeleton Skeleton { get => skeleton_; set => skeleton_ = value; }
        private Skeleton skeleton_ = new Skeleton();
        /// Geometries.
        [IgnoreDataMember]
        public Geometry[][] Geometries { get => geometries_; set => geometries_ = value; }
        private Geometry[][] geometries_ = new Geometry[0][];

        [IgnoreDataMember]
        public int NumGeometries => geometries_.Length;

        /// Geometry bone mappings.
        public List<int[]> GeometryBoneMappings { get; set; }
        /// Geometry centers.
        public List<Vector3> GeometryCenters { get; set; }

        /// Vertex morphs.
        public ModelMorph[] Morphs => morphs_;
        ModelMorph[] morphs_;

        /// Vertex buffer morph range start.
        int[] morphRangeStarts_;
        /// Vertex buffer morph range vertex count.
        int[] morphRangeCounts_;


        public Model()
        {
        }

        public override void Dispose()
        {
            // todo: Dispose
            Geometries.Clear();
        }


        public int GetNumGeometryLodLevels(int index)
        {
            return index < geometries_.Length ? geometries_[index].Length : 0;
        }

        public Geometry GetGeometry(int index, int lodLevel)
        {
            if (index >= geometries_.Length || geometries_[index].Empty())
                return null;

            if (lodLevel >= geometries_[index].Length)
                lodLevel = geometries_[index].Length - 1;

            return geometries_[index][lodLevel];
        }

        public ModelMorph? GetMorph(int index) => (index < morphs_.Length) ? (ModelMorph?)morphs_[index] : null;
        
        public int GetMorphRangeStart(int bufferIndex)
        {
            return bufferIndex < vertexBuffers_.Length ? morphRangeStarts_[bufferIndex] : 0;
        }

        public int GetMorphRangeCount(int bufferIndex)
        {
            return bufferIndex < vertexBuffers_.Length ? morphRangeCounts_[bufferIndex] : 0;
        }

        /// Description of vertex buffer data for asynchronous loading.
        struct VertexBufferDesc
        {
            /// Vertex count.
            public int vertexCount_;
            public int vertexSize_;
            /// Vertex declaration.
            //public List<VertexElement> vertexElements_;
            public PipelineVertexInputStateCreateInfo layout;
            /// Vertex data size.
            public int dataSize_;
            /// Vertex data.
            public byte[] data_;
        };

        /// Description of index buffer data for asynchronous loading.
        struct IndexBufferDesc
        {
            /// Index count.
            public int indexCount_;
            /// Index size.
            public int indexSize_;
            /// Index data size.
            public int dataSize_;
            /// Index data.
            public byte[] data_;
        };

        /// Description of a geometry for asynchronous loading.
        struct GeometryDesc
        {
            /// Primitive type.
            public PrimitiveTopology type_;
            /// Vertex buffer ref.
            public int vbRef_;
            /// Index buffer ref.
            public int ibRef_;
            /// Index start.
            public int indexStart_;
            /// Index count.
            public int indexCount_;
        };

        /// Vertex buffer data for asynchronous loading.
        VertexBufferDesc[] loadVBData_;
        /// Index buffer data for asynchronous loading.
        IndexBufferDesc[] loadIBData_;
        /// Geometry definitions for asynchronous loading.
        List<GeometryDesc[]> loadGeometries_;

        static PrimitiveTopology[] primitiveType2Topology = new[]
        {
            PrimitiveTopology.TriangleList, PrimitiveTopology.TriangleStrip, PrimitiveTopology.LineList, PrimitiveTopology.LineStrip, PrimitiveTopology.PointList
        };

        const uint MASK_NONE = 0x0;
        const uint MASK_POSITION = 0x1;
        const uint MASK_NORMAL = 0x2;
        const uint MASK_COLOR = 0x4;
        const uint MASK_TEXCOORD1 = 0x8;
        const uint MASK_TEXCOORD2 = 0x10;
        const uint MASK_CUBETEXCOORD1 = 0x20;
        const uint MASK_CUBETEXCOORD2 = 0x40;
        const uint MASK_TANGENT = 0x80;
        const uint MASK_BLENDWEIGHTS = 0x100;
        const uint MASK_BLENDINDICES = 0x200;
        const uint MASK_INSTANCEMATRIX4 = 0x400;
        const uint MASK_INSTANCEMATRIX3 = 0x800;
        const uint MASK_INSTANCEMATRIX2 = 0x1000;
        const uint MASK_INSTANCEMATRIX1 = 0x2000;

        static PipelineVertexInputStateCreateInfo CreateVertexInputStateCreateInfo(uint mask, out int stride)
        {
            List<VertexInputBindingDescription> vertexInputBinding = new List<VertexInputBindingDescription>();
            List<VertexInputAttributeDescription> vertexInputAttributes = new List<VertexInputAttributeDescription>();
            stride = 0;
            int location = 0;
            if ((mask & MASK_POSITION) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttributeDescription(location, 0, Format.R32G32B32SFloat, stride));
                stride += 12;
                location++;
            }
            if ((mask & MASK_NORMAL) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttributeDescription(location, 0, Format.R32G32B32SFloat, stride));
                stride += 12;
                location++;
            }
            if ((mask & MASK_COLOR) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttributeDescription(location, 0, Format.R8G8B8A8UNorm, stride));
                stride += 4;
                location++;
            }
            if ((mask & MASK_TEXCOORD1) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttributeDescription(location, 0, Format.R32G32SFloat, stride));
                stride += 8;
                location++;
            }
            if ((mask & MASK_TEXCOORD2) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttributeDescription(location, 0, Format.R32G32SFloat, stride));
                stride += 8;
                location++;
            }
            if ((mask & MASK_TANGENT) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttributeDescription(location, 0, Format.R32G32B32A32SFloat, stride));
                stride += 16;
                location++;
            }
            if ((mask & MASK_BLENDWEIGHTS) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttributeDescription(location, 0, Format.R32G32B32A32SFloat, stride));
                stride += 16;
                location++;
            }
            if ((mask & MASK_BLENDINDICES) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttributeDescription(location, 0, Format.R8G8B8A8UInt, stride));
                stride += 4;
                location++;
            }

            vertexInputBinding.Add(new VertexInputBindingDescription(0, stride, VertexInputRate.Vertex));
            //todo:
            return new PipelineVertexInputStateCreateInfo(vertexInputBinding.ToArray(), vertexInputAttributes.ToArray());
        }

        public async override Task<bool> Load(File source)
        {
            String fileID = source.ReadFileID();
            if (fileID != "UMDL" && fileID != "UMD2")
            {
                Log.Error("Invalid model file");
                return false;
            }

            bool hasVertexDeclarations = (fileID == "UMD2");

            int memoryUse = Unsafe.SizeOf<Model>();

            // Read vertex buffers
            int numVertexBuffers = (int)source.Read<uint>();
            vertexBuffers_ = new GraphicsBuffer[numVertexBuffers];
            morphRangeStarts_ = new int[numVertexBuffers];
            morphRangeCounts_ = new int[numVertexBuffers];
            loadVBData_ = new VertexBufferDesc[numVertexBuffers];
            GeometryBoneMappings = new List<int[]>();
            GeometryCenters = new List<Vector3>();

            for (int i = 0; i < numVertexBuffers; ++i)
            {
                loadVBData_[i].vertexCount_ = source.Read<int>();
                int vertexSize = 0;
                if (!hasVertexDeclarations)
                {
                    uint elementMask = source.Read<uint>();
                    loadVBData_[i].layout = CreateVertexInputStateCreateInfo(elementMask, out vertexSize);
                }
                else
                {
                    throw new NotImplementedException();
                }

                morphRangeStarts_[i] = source.Read<int>();
                morphRangeCounts_[i] = source.Read<int>();
                loadVBData_[i].vertexSize_ = vertexSize;
                loadVBData_[i].dataSize_ = loadVBData_[i].vertexCount_ * vertexSize;
                loadVBData_[i].data_ = source.ReadArray<byte>(loadVBData_[i].dataSize_);
            }

            // Read index buffers
            int numIndexBuffers = (int)source.Read<uint>();
            indexBuffers_ = new GraphicsBuffer[numIndexBuffers];
            loadIBData_ = new IndexBufferDesc[numIndexBuffers];
            for (int i = 0; i < numIndexBuffers; ++i)
            {
                int indexCount = source.Read<int>();
                int indexSize = source.Read<int>();

                loadIBData_[i].indexCount_ = indexCount;
                loadIBData_[i].indexSize_ = indexSize;
                loadIBData_[i].dataSize_ = indexCount * indexSize;
                loadIBData_[i].data_ = source.ReadArray<byte>(loadIBData_[i].dataSize_);
            }

            // Read geometries
            int numGeometries = source.Read<int>();
            loadGeometries_ = new List<GeometryDesc[]>(numGeometries);
            Array.Resize(ref geometries_, numGeometries);

            for (int i = 0; i < numGeometries; ++i)
            {
                // Read bone mappings
                int boneMappingCount = source.Read<int>();
                int[] boneMapping = new int[boneMappingCount];
                for (uint j = 0; j < boneMappingCount; ++j)
                    boneMapping[j] = source.Read<int>();

                GeometryBoneMappings.Add(boneMapping);

                int numLodLevels = source.Read<int>();
                Geometry[] geometryLodLevels = new Geometry[numLodLevels];
                GeometryDesc[] deoDesc = new GeometryDesc[numLodLevels];
                loadGeometries_.Add(deoDesc);
                for (int j = 0; j < numLodLevels; ++j)
                {
                    float distance = source.Read<float>();
                    
                    /*
                     *  public enum PrimitiveType : ushort
                        {
                            TRIANGLE_LIST = 0,
                            TRIANGLE_STRIP,
                            LINE_LIST,
                            LINE_STRIP,
                            POINT_LIST
                        }
                    */

                    PrimitiveTopology type = primitiveType2Topology[source.Read<int>()];
                    int vbRef = source.Read<int>();
                    int ibRef = source.Read<int>();
                    int indexStart = source.Read<int>();
                    int indexCount = source.Read<int>();

                    if (vbRef >= vertexBuffers_.Length)
                    {
                        Log.Error("Vertex buffer index out of bounds");
                        loadVBData_ = null;
                        loadIBData_ = null;
                        loadGeometries_.Clear();
                        return false;
                    }

                    if (ibRef >= indexBuffers_.Length)
                    {
                        Log.Error("Index buffer index out of bounds");
                        loadVBData_ = null;
                        loadIBData_ = null;
                        loadGeometries_.Clear();
                        return false;
                    }

                    Geometry geometry = new Geometry();
                    geometry.LodDistance = distance;

                    // Prepare geometry to be defined during EndLoad()
                    deoDesc[j].type_ = type;
                    deoDesc[j].vbRef_ = vbRef;
                    deoDesc[j].ibRef_ = ibRef;
                    deoDesc[j].indexStart_ = indexStart;
                    deoDesc[j].indexCount_ = indexCount;

                    geometryLodLevels[j] = geometry;
                    memoryUse += Unsafe.SizeOf<Geometry>();
                }

                geometries_[i] = geometryLodLevels;
            }

            // Read morphs
            uint numMorphs = source.Read<uint>();
            morphs_ = new ModelMorph[(int)numMorphs];
            for (int i = 0; i < numMorphs; ++i)
            {
                //ModelMorph newMorph = morphs_[i];

                morphs_[i].name_ = source.ReadCString();
                morphs_[i].weight_ = 0.0f;
                uint numBuffers = source.Read<uint>();

                for (int j = 0; j < numBuffers; ++j)
                {
                    VertexBufferMorph newBuffer;
                    int bufferIndex = source.Read<int>();

                    newBuffer.elementMask_ = source.Read<uint>();
                    newBuffer.vertexCount_ = source.Read<int>();

                    // Base size: size of each vertex index
                    int vertexSize = sizeof(int);
                    // Add size of individual elements
                    unsafe
                    {
                        if ((newBuffer.elementMask_ & MASK_POSITION) != 0)
                            vertexSize += sizeof(Vector3);
                        if ((newBuffer.elementMask_ & MASK_NORMAL) != 0)
                            vertexSize += sizeof(Vector3);
                        if ((newBuffer.elementMask_ & MASK_TANGENT) != 0)
                            vertexSize += sizeof(Vector3);
                   
                    }

                    newBuffer.dataSize_ = newBuffer.vertexCount_ * (int)vertexSize;
                        newBuffer.morphData_ = source.ReadArray<byte>((int)newBuffer.dataSize_);
                        morphs_[i].buffers_[bufferIndex] = newBuffer;
                        memoryUse += Unsafe.SizeOf<VertexBufferMorph>() + newBuffer.vertexCount_ * vertexSize;
                }

                memoryUse += Unsafe.SizeOf<ModelMorph>();
            }

            // Read skeleton
            skeleton_.Load(source);
            memoryUse += Skeleton.NumBones * Unsafe.SizeOf<Bone>();

            // Read bounding box
            boundingBox_ = source.Read<BoundingBox>();

            // Read geometry centers
            for (int i = 0; i < geometries_.Length && !source.IsEof; ++i)
                GeometryCenters.Add(source.Read<Vector3>());
            while (GeometryCenters.Count < geometries_.Length)
                GeometryCenters.Add(Vector3.Zero);

            MemoryUse = memoryUse;
            return true;
        }
    
        protected override void OnBuild()
        {
            // Upload vertex buffer data
            for (int i = 0; i < vertexBuffers_.Length; ++i)
            {
                ref GraphicsBuffer buffer = ref vertexBuffers_[i];
                ref VertexBufferDesc desc = ref loadVBData_[i];
                if (desc.data_ != null)
                {
                    buffer = GraphicsBuffer.Vertex(Utilities.AsPointer(ref desc.data_[0]), desc.vertexSize_, desc.vertexCount_);
                }
            }
           
            // Upload index buffer data
            for (int i = 0; i < indexBuffers_.Length; ++i)
            {
                ref GraphicsBuffer buffer = ref indexBuffers_[i];
                ref IndexBufferDesc desc = ref loadIBData_[i];
                if (desc.data_ != null)
                {
                    buffer = GraphicsBuffer.Index(Utilities.AsPointer(ref desc.data_[0]), desc.indexSize_, desc.indexCount_);
                }
            }

            // Set up geometries
            for (int i = 0; i < geometries_.Length; ++i)
            {
                for (int j = 0; j < geometries_[i].Length; ++j)
                {
                    Geometry geometry = geometries_[i][j];
                    ref GeometryDesc desc = ref loadGeometries_[i][j];
                    
                    geometry.VertexBuffers = new[] { vertexBuffers_[desc.vbRef_] };
                    geometry.VertexInputState = loadVBData_[desc.vbRef_].layout;
                    geometry.IndexBuffer = indexBuffers_[desc.ibRef_];
                    geometry.SetDrawRange(desc.type_, desc.indexStart_, desc.indexCount_, 0, -1);
                }
            }

            loadVBData_ = null;
            loadIBData_ = null;
            loadGeometries_ = null;

        }
    }

}
