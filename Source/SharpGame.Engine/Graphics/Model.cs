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
    [DataContract]
    public class Model : Resource<Model>
    {
        /// Vertex buffers.
        private DeviceBuffer[] vertexBuffers_;
        [DataMember]
        public DeviceBuffer[] VertexBuffers
        {
            get => vertexBuffers_; set => vertexBuffers_ = value;
        }

        /// Index buffers.
        private DeviceBuffer[] indexBuffers_;
        [DataMember]
        public DeviceBuffer[] IndexBuffers
        {
            get => indexBuffers_; set => indexBuffers_ = value;
        }

        /// Bounding box.
        [DataMember]
        public BoundingBox BoundingBox { get; set; }

        /// Skeleton.
        [DataMember]
        public Skeleton Skeleton { get; set; }

        /// Geometries.
        [IgnoreDataMember]
        public Geometry[][] Geometries { get => geometries_; set => geometries_ = value; }
        private Geometry[][] geometries_ = new Geometry[0][];

        [IgnoreDataMember]
        public int NumGeometries => geometries_.Length;

        /// Geometry bone mappings.
        public List<int[]> GeometryBoneMappings { get; set; }
        /// Geometry centers.
        public List<Vector3> GeometryCenters { get; set; } = new List<Vector3>();
        public List<Material> Materials { get; set; } = new List<Material>();
        public ResourceRefList MaterialList { get; set; }

        public Model()
        {
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

        protected override void Destroy()
        {
            foreach (var vb in vertexBuffers_)
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

    }

}
