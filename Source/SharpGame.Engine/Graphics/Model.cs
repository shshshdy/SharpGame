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
    public struct GeometryDesc
    {
        /// Primitive type.
        public PrimitiveTopology primitiveTopology;
        /// Vertex buffer ref.
        public int vbRef;
        /// Index buffer ref.
        public int ibRef;
        /// Index start.
        public int indexStart;
        /// Index count.
        public int indexCount;
    };

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

        [DataMember]
        public BoundingBox BoundingBox { get; set; }

        /// Bounding box.
        [DataMember]
        public GeometryDesc[] GeometryDesc { get => geometryDesc; set => geometryDesc = value; }
        private GeometryDesc[] geometryDesc;

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
        public List<vec3> GeometryCenters { get; set; } = new List<vec3>();
        public List<Material> Materials { get; set; } = new List<Material>();
        public ResourceRefList MaterialList { get; set; }

        public Model()
        {
        }

        public void SetNumGeometry(int count)
        {
            Array.Resize(ref geometries, count);
            Array.Resize(ref geometryDesc, count);
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

        public static Model Create(List<Geometry> geometries, List<BoundingBox> bboxList = null)
        {
            Model model = new Model();
            model.SetNumGeometry(geometries.Count);

            Array.Resize(ref model.vertexBuffers, geometries.Count);
            Array.Resize(ref model.indexBuffers, geometries.Count);

            BoundingBox bbox = new BoundingBox();
            for (int i = 0; i < geometries.Count; i++)
            {
                model.geometries[i] = new[] { geometries[i] };
                model.vertexBuffers[i] = geometries[i].VertexBuffers[0];
                model.IndexBuffers[i] = geometries[i].IndexBuffer;

                ref GeometryDesc desc = ref model.geometryDesc[i];
                desc.primitiveTopology = geometries[i].PrimitiveTopology;
                desc.vbRef = i;
                desc.ibRef = i;
                desc.indexStart = (int)geometries[i].IndexStart;
                desc.indexCount = (int)geometries[i].IndexCount;

                if(bboxList != null)
                {
                    model.GeometryCenters[i] = bboxList[i].Center;
                    bbox.Merge(bboxList[i]);
                }
            }

            model.BoundingBox = bbox;
            return model;
        }

    }

}
