using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    /// Description of vertex buffer data for asynchronous loading.
    struct VertexBufferDesc
    {
        /// Vertex count.
        public int vertexCount_;

        public int vertexSize_;
        /// Vertex declaration.
        public VertexLayout layout;
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

    public class MdlModelReader : ResourceReader<Model>
    {
        public MdlModelReader() : base(".mdl")
        {
        }

        protected override bool OnLoad(Model model, File stream)
        {
            String fileID = stream.ReadFileID();
            if (fileID != "UMDL" && fileID != "UMD2")
            {
                Log.Error("Invalid model file");
                return false;
            }

            bool hasVertexDeclarations = (fileID == "UMD2");

            int memoryUse = Unsafe.SizeOf<Model>();

            DeviceBuffer[] vertexBuffers_;
            DeviceBuffer[] indexBuffers_;
            Geometry[][] geometries_ = new Geometry[0][];

            VertexBufferDesc[] loadVBData_;
            IndexBufferDesc[] loadIBData_;
            List<GeometryDesc[]> loadGeometries_;

            // Read vertex buffers
            int numVertexBuffers = (int)stream.Read<uint>();
            vertexBuffers_ = new DeviceBuffer[numVertexBuffers];
            var morphRangeStarts_ = new int[numVertexBuffers];
            var morphRangeCounts_ = new int[numVertexBuffers];
            loadVBData_ = new VertexBufferDesc[numVertexBuffers];
            var GeometryBoneMappings = new List<int[]>();
            var GeometryCenters = new List<vec3>();

            for (int i = 0; i < numVertexBuffers; ++i)
            {
                loadVBData_[i].vertexCount_ = stream.Read<int>();
                uint vertexSize = 0;
                if (!hasVertexDeclarations)
                {
                    uint elementMask = stream.Read<uint>();
                    loadVBData_[i].layout = CreateVertexInputStateCreateInfo(elementMask, out vertexSize);
                }
                else
                {
                    /*
                    enum VertexElementSemantic
                    {
                        SEM_POSITION = 0,
                        SEM_NORMAL,
                        SEM_BINORMAL,
                        SEM_TANGENT,
                        SEM_TEXCOORD,
                        SEM_COLOR,
                        SEM_BLENDWEIGHTS,
                        SEM_BLENDINDICES,
                        SEM_OBJECTINDEX,
                        MAX_VERTEX_ELEMENT_SEMANTICS
                    }*/

                    uint numElements = stream.Read<uint>();
                    FastList<VertexInputAttribute> attrs = new FastList<VertexInputAttribute>();
                    uint offset = 0;
                    for (uint j = 0; j < numElements; ++j)
                    {
                        uint elementDesc = stream.Read<uint>();
                        uint type = (elementDesc & 0xff);
                        uint semantic =((elementDesc >> 8) & 0xff);
                        uint index = ((elementDesc >> 16) & 0xff);

                        VertexInputAttribute attr = new VertexInputAttribute(0, j, semanticToFormat[semantic], offset);
                        offset += semanticSize[semantic];
                        attrs.Add(attr);
                    }

                    vertexSize = offset;
                    var layout = new VertexLayout(attrs.ToArray());
                    loadVBData_[i].layout = layout;
                    layout.Print();
                    
                }

                morphRangeStarts_[i] = stream.Read<int>();
                morphRangeCounts_[i] = stream.Read<int>();
                loadVBData_[i].vertexSize_ = (int)vertexSize;
                loadVBData_[i].dataSize_ = loadVBData_[i].vertexCount_ * (int)vertexSize;
                loadVBData_[i].data_ = stream.ReadArray<byte>(loadVBData_[i].dataSize_);
            }

            // Read index buffers
            int numIndexBuffers = (int)stream.Read<uint>();
            indexBuffers_ = new DeviceBuffer[numIndexBuffers];
            loadIBData_ = new IndexBufferDesc[numIndexBuffers];
            for (int i = 0; i < numIndexBuffers; ++i)
            {
                int indexCount = stream.Read<int>();
                int indexSize = stream.Read<int>();

                loadIBData_[i].indexCount_ = indexCount;
                loadIBData_[i].indexSize_ = indexSize;
                loadIBData_[i].dataSize_ = indexCount * indexSize;
                loadIBData_[i].data_ = stream.ReadArray<byte>(loadIBData_[i].dataSize_);
            }

            // Read geometries
            int numGeometries = stream.Read<int>();
            loadGeometries_ = new List<GeometryDesc[]>(numGeometries);
            Array.Resize(ref geometries_, numGeometries);

            for (int i = 0; i < numGeometries; ++i)
            {
                // Read bone mappings
                int boneMappingCount = stream.Read<int>();
                int[] boneMapping = new int[boneMappingCount];
                for (uint j = 0; j < boneMappingCount; ++j)
                    boneMapping[j] = stream.Read<int>();

                GeometryBoneMappings.Add(boneMapping);

                int numLodLevels = stream.Read<int>();
                Geometry[] geometryLodLevels = new Geometry[numLodLevels];
                GeometryDesc[] deoDesc = new GeometryDesc[numLodLevels];
                loadGeometries_.Add(deoDesc);
                for (int j = 0; j < numLodLevels; ++j)
                {
                    float distance = stream.Read<float>();

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

                    PrimitiveTopology type = primitiveType2Topology[stream.Read<int>()];
                    int vbRef = stream.Read<int>();
                    int ibRef = stream.Read<int>();
                    int indexStart = stream.Read<int>();
                    int indexCount = stream.Read<int>();

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
                    deoDesc[j].primitiveTopology = type;
                    deoDesc[j].vbRef = vbRef;
                    deoDesc[j].ibRef = ibRef;
                    deoDesc[j].indexStart = indexStart;
                    deoDesc[j].indexCount = indexCount;

                    geometryLodLevels[j] = geometry;
                    memoryUse += Unsafe.SizeOf<Geometry>();
                }

                geometries_[i] = geometryLodLevels;
            }

            // Read morphs
            uint numMorphs = stream.Read<uint>();
            var morphs_ = new ModelMorph[(int)numMorphs];
            for (int i = 0; i < numMorphs; ++i)
            {
                morphs_[i].name_ = stream.ReadCString();
                morphs_[i].weight_ = 0.0f;
                uint numBuffers = stream.Read<uint>();

                for (int j = 0; j < numBuffers; ++j)
                {
                    VertexBufferMorph newBuffer;
                    int bufferIndex = stream.Read<int>();

                    newBuffer.elementMask_ = stream.Read<uint>();
                    newBuffer.vertexCount_ = stream.Read<int>();

                    // Base size: size of each vertex index
                    int vertexSize = sizeof(int);
                    // Add size of individual elements
                    unsafe
                    {
                        if ((newBuffer.elementMask_ & MASK_POSITION) != 0)
                            vertexSize += sizeof(vec3);
                        if ((newBuffer.elementMask_ & MASK_NORMAL) != 0)
                            vertexSize += sizeof(vec3);
                        if ((newBuffer.elementMask_ & MASK_TANGENT) != 0)
                            vertexSize += sizeof(vec3);
                    }

                    newBuffer.dataSize_ = newBuffer.vertexCount_ * (int)vertexSize;
                    newBuffer.morphData_ = stream.ReadArray<byte>((int)newBuffer.dataSize_);
                    morphs_[i].buffers_[bufferIndex] = newBuffer;
                    memoryUse += Unsafe.SizeOf<VertexBufferMorph>() + newBuffer.vertexCount_ * vertexSize;
                }

                memoryUse += Unsafe.SizeOf<ModelMorph>();
            }

            Skeleton skeleton_ = new Skeleton();
            // Read skeleton
            skeleton_.Load(stream);
            memoryUse += skeleton_.NumBones * Unsafe.SizeOf<Bone>();

            // Read bounding box
            var boundingBox_ = stream.Read<BoundingBox>();

            // Read geometry centers
            for (int i = 0; i < geometries_.Length && !stream.IsEof; ++i)
                GeometryCenters.Add(stream.Read<vec3>());
            while (GeometryCenters.Count < geometries_.Length)
                GeometryCenters.Add(vec3.Zero);

            model.VertexBuffers = vertexBuffers_;
            model.IndexBuffers = indexBuffers_;

            model.Skeleton = skeleton_;
            model.BoundingBox = boundingBox_;
            model.Geometries = geometries_;
            model.GeometryBoneMappings = GeometryBoneMappings;
            model.GeometryCenters = GeometryCenters;
            //model.morphRangeStarts_ = morphRangeStarts_;
            //model.morphRangeCounts_ = morphRangeCounts_;
            model.MemoryUse = memoryUse;

            for (int i = 0; i < vertexBuffers_.Length; ++i)
            {
                ref DeviceBuffer buffer = ref vertexBuffers_[i];
                ref VertexBufferDesc desc = ref loadVBData_[i];
                if (desc.data_ != null)
                {
                    buffer = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, false
                        , (uint)desc.vertexSize_, (uint)desc.vertexCount_, Utilities.AsPointer(ref desc.data_[0]));
                }
            }

            // Upload index buffer data
            for (int i = 0; i < indexBuffers_.Length; ++i)
            {
                ref DeviceBuffer buffer = ref indexBuffers_[i];
                ref IndexBufferDesc desc = ref loadIBData_[i];
                if (desc.data_ != null)
                {
                    buffer = DeviceBuffer.Create(BufferUsageFlags.IndexBuffer, false, (uint)desc.indexSize_, (uint)desc.indexCount_, Utilities.AsPointer(ref desc.data_[0]));
                }
            }

            // Set up geometries
            for (int i = 0; i < geometries_.Length; ++i)
            {
                for (int j = 0; j < geometries_[i].Length; ++j)
                {
                    Geometry geometry = geometries_[i][j];
                    ref GeometryDesc desc = ref loadGeometries_[i][j];

                    geometry.VertexBuffers = new[] { vertexBuffers_[desc.vbRef] };
                    geometry.VertexLayout = loadVBData_[desc.vbRef].layout;
                    geometry.IndexBuffer = indexBuffers_[desc.ibRef];
                    geometry.SetDrawRange(desc.primitiveTopology, (uint)desc.indexStart, (uint)desc.indexCount);
                }
            }

            loadVBData_ = null;
            loadIBData_ = null;
            loadGeometries_ = null;
            return true;
        }


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

        Format[] semanticToFormat =
        {
            Format.R32g32b32Sfloat,
            Format.R32g32b32Sfloat,
            Format.R32g32b32a32Sfloat,
            Format.R32g32b32a32Sfloat,
            Format.R32g32Sfloat,
            Format.R8g8b8a8Unorm,
            Format.R32g32b32a32Sfloat,
            Format.R8g8b8a8Sint,
        };

        uint[] semanticSize = { 12, 12, 16, 16, 8, 4, 16, 4 };
        static VertexLayout CreateVertexInputStateCreateInfo(uint mask, out uint stride)
        {
            List<VertexInputAttribute> vertexInputAttributes = new List<VertexInputAttribute>();
            stride = 0;
            uint location = 0;
            if ((mask & MASK_POSITION) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttribute(0, location, Format.R32g32b32Sfloat, stride));
                stride += 12;
                location++;
            }
            if ((mask & MASK_NORMAL) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttribute(0, location, Format.R32g32b32Sfloat, stride));
                stride += 12;
                location++;
            }
            if ((mask & MASK_COLOR) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttribute(0, location, Format.R8g8b8a8Unorm, stride));
                stride += 4;
                location++;
            }
            if ((mask & MASK_TEXCOORD1) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttribute(0, location, Format.R32g32Sfloat, stride));
                stride += 8;
                location++;
            }
            if ((mask & MASK_TEXCOORD2) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttribute(0, location, Format.R32g32Sfloat, stride));
                stride += 8;
                location++;
            }
            if ((mask & MASK_TANGENT) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttribute(0, location, Format.R32g32b32a32Sfloat, stride));
                stride += 16;
                location++;
            }
            if ((mask & MASK_BLENDWEIGHTS) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttribute(0, location, Format.R32g32b32a32Sfloat, stride));
                stride += 16;
                location++;
            }
            if ((mask & MASK_BLENDINDICES) != 0)
            {
                vertexInputAttributes.Add(new VertexInputAttribute(0, location, Format.R8g8b8a8Uint, stride));
                stride += 4;
                location++;
            }

            Log.Info("vertex attribute : ");
            foreach(var attr in vertexInputAttributes)
            {
                Log.Info("{{{0}, {1}, {2}}}", attr.location, attr.format, attr.offset);                
            }

            return new VertexLayout(vertexInputAttributes.ToArray());
        }
    }
}
