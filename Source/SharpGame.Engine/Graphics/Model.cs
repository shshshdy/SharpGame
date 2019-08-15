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
    public class Model : Resource
    {
        /// Vertex buffers.
        private DeviceBuffer[] vertexBuffers;
        [DataMember]
        public DeviceBuffer[] VertexBuffers
        {
            get => vertexBuffers; set => vertexBuffers = value;
        }

        /// Index buffers.
        private DeviceBuffer[] indexBuffers;
        [DataMember]
        public DeviceBuffer[] IndexBuffers
        {
            get => indexBuffers; set => indexBuffers = value;
        }

        /// Bounding box.
        [DataMember]
        public BoundingBox BoundingBox { get; set; }

        /// Skeleton.
        [DataMember]
        public Skeleton Skeleton { get; set; }

        /// Geometries.
        [IgnoreDataMember]
        public Geometry[][] Geometries { get => geometries; set => geometries = value; }
        private Geometry[][] geometries = new Geometry[0][];

        [IgnoreDataMember]
        public int NumGeometries => geometries.Length;

        /// Geometry bone mappings.
        public List<int[]> GeometryBoneMappings { get; set; }
        /// Geometry centers.
        public List<Vector3> GeometryCenters { get; set; } = new List<Vector3>();
        public List<Material> Materials { get; set; } = new List<Material>();
        public ResourceRefList MaterialList { get; set; }

        public Model()
        {
        }

        public Model(params Geometry[] geometries)
        {
            SetNumGeometry(geometries.Length);

            for(int i = 0; i < geometries.Length; i++)
            {
                this.geometries[i] = new []{ geometries[i] };
            }
        }

        public void SetNumGeometry(int count)
        {
            Array.Resize(ref geometries, count);
            GeometryCenters.Resize(count);
        }
        
        public int GetNumGeometryLodLevels(int index)
        {
            return index < geometries.Length ? geometries[index].Length : 0;
        }

        public Geometry GetGeometry(int index, int lodLevel)
        {
            if (index >= geometries.Length || geometries[index].Empty())
                return null;

            if (lodLevel >= geometries[index].Length)
                lodLevel = geometries[index].Length - 1;

            return geometries[index][lodLevel];
        }

        public Material GetMaterial(int index)
        {
            if (index >= Materials.Count || Materials.Empty())
                return null;
            return Materials[index];
        }

        protected override void Destroy()
        {
            foreach (var vb in vertexBuffers)
            {
                vb.Dispose();
            }

            Array.Clear(vertexBuffers, 0, vertexBuffers.Length);

            foreach (var ib in indexBuffers)
            {
                ib.Dispose();
            }

            Array.Clear(indexBuffers, 0, indexBuffers.Length);

            Geometries.Clear();
        }

    }

}
