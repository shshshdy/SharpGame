using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Vulkan;

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
        private GraphicsBuffer[] vertexBuffers_;
        public GraphicsBuffer[] VertexBuffers { get => vertexBuffers_; set => vertexBuffers_ = value; }

        /// Index buffers.
        private GraphicsBuffer[] indexBuffers_;
        public GraphicsBuffer[] IndexBuffers
        {
            get => indexBuffers_; set => indexBuffers_ = value;
        }


        /// Bounding box.
        [DataMember]
        public BoundingBox BoundingBox { get => boundingBox_; set => boundingBox_ = value; }
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
        public ModelMorph[] Morphs { get => morphs_; set => morphs_ = value; }
        ModelMorph[] morphs_;

        /// Vertex buffer morph range start.
        public int[] morphRangeStarts_;
        /// Vertex buffer morph range vertex count.
        public int[] morphRangeCounts_;


        public Model()
        {
        }

        protected override void Destroy()
        {
            foreach(var vb in vertexBuffers_)
            {
                vb.Dispose();
            }
            Array.Clear(vertexBuffers_, 0, vertexBuffers_.Length);

            foreach (var ib in indexBuffers_)
            {
                ib.Dispose();
            }
            Array.Clear(indexBuffers_, 0, indexBuffers_.Length);

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

        protected override bool OnLoad(File source)
        {
            return true;
        }
    
        protected override bool OnBuild()
        {            
            return true;
        }
    }

}
