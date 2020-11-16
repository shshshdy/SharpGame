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
        private List<Buffer> vertexBuffers;
        [DataMember]
        public List<Buffer> VertexBuffers
        {
            get => vertexBuffers; set => vertexBuffers = value;
        }

        /// Index buffers.
        private List<Buffer> indexBuffers;
        [DataMember]
        public List<Buffer> IndexBuffers
        {
            get => indexBuffers; set => indexBuffers = value;
        }

        [DataMember]
        public BoundingBox BoundingBox { get; set; }

        /// Bounding box.
//         [DataMember]
//         public FastList<GeometryDesc> GeometryDesc { get => geometryDesc; set => geometryDesc = value; }
//         private FastList<GeometryDesc> geometryDesc = new FastList<GeometryDesc>();

        /// Skeleton.
        [DataMember]
        public Skeleton Skeleton { get; set; }

        /// Geometries.
        [IgnoreDataMember]
        public List<Geometry[]> Geometries { get => geometries; set => geometries = value; }
        private List<Geometry[]> geometries = new List<Geometry[]>();

        [IgnoreDataMember]
        public int NumGeometries => geometries.Count;

        /// Geometry bone mappings.
        public List<int[]> GeometryBoneMappings { get; set; }
        /// Geometry centers.
        public List<vec3> GeometryCenters { get; set; } = new List<vec3>();
        public List<Material> Materials { get; set; } = new List<Material>();

        public Model()
        {
        }

        public void SetNumGeometry(int count)
        {            
            geometries.Resize(count);
            //geometryDesc.Resize(count);
            GeometryCenters.Resize(count);
        }
        
        public int GetNumGeometryLodLevels(int index)
        {
            return index < geometries.Count ? geometries[index].Length : 0;
        }

        public Geometry GetGeometry(int index, int lodLevel)
        {
            if (index >= geometries.Count || geometries[index].Empty())
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

        protected override void Destroy(bool disposing)
        {
            foreach (var vb in vertexBuffers)
            {
                vb.Dispose();
            }

            vertexBuffers.Clear();

            foreach (var ib in indexBuffers)
            {
                ib.Dispose();
            }

            indexBuffers.Clear();

            Geometries.Clear();

            base.Destroy(disposing);
        }

        public static Model Create(List<Geometry> geometries, List<BoundingBox> bboxList = null)
        {
            Model model = new Model();
            model.SetNumGeometry(geometries.Count);

            if(model.vertexBuffers == null)
            {
                model.vertexBuffers = new List<Buffer>();
            }

            model.vertexBuffers.Resize(geometries.Count);

            if (model.indexBuffers == null)
            {
                model.indexBuffers = new List<Buffer>();
            }

            model.indexBuffers.Resize(geometries.Count);

            BoundingBox bbox = new BoundingBox();
            for (int i = 0; i < geometries.Count; i++)
            {
                model.geometries[i] = new[] { geometries[i] };
                model.vertexBuffers[i] = geometries[i].VertexBuffer;
                model.IndexBuffers[i] = geometries[i].IndexBuffer;

                //ref GeometryDesc desc = ref model.geometryDesc.At(i);
                //desc.primitiveTopology = geometries[i].PrimitiveTopology;
//                 desc.vbRef = i;
//                 desc.ibRef = i;
//                 desc.indexStart = (int)geometries[i].IndexStart;
//                 desc.indexCount = (int)geometries[i].IndexCount;

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
