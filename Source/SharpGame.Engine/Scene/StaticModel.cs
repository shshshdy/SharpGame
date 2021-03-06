﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    public class StaticModel : Drawable
    {
        /// All geometries.
        protected Geometry[][] geometries_;
        /// Extra per-geometry data.
        protected StaticModelGeometryData[] geometryData_ = new StaticModelGeometryData[0];
        /// Model.
        protected Model model_;

        public StaticModel()
        {
        }

        public Model Model
        {
            get
            {
                return  model_;
            }

            set
            {
                SetModel(value);
            }
        }

        public string ModelFile
        {
            set
            {
                SetModel(Resources.Instance.Load<Model>(value));
            }
        }

        public void SetBoundingBox(BoundingBox box)
        {
            boundingBox_ = box;
            OnMarkedDirty(node_);
        }

        public virtual void SetNumGeometries(int num)
        {
            Array.Resize(ref batches, num);

            for (int i = 0; i < num; i++)
            {
                if (batches[i] == null)
                {
                    batches[i] = new SourceBatch
                    {
                        geometryType = GeometryType
                    };
                }
            }

            Array.Resize(ref geometries_, num);
            Array.Resize(ref geometryData_, num);

            ResetLodLevels();
        }

        public void SetGeometry(int index, Geometry geometry)
        {
            batches[index].geometry = geometry;
            batches[index].worldTransform = node_.worldTransform_;
            batches[index].numWorldTransforms = 1;
        }

        public virtual StaticModel SetModel(Model model)
        {
            if (model == model_)
                return this;

            model_ = model;

            if(model_ != null)
            {             
                SetNumGeometries(model.Geometries.Count);

                List<Geometry[]> geometries = model.Geometries;
                List<vec3> geometryCenters = model.GeometryCenters;

                for(int i = 0; i < geometries_.Length; ++i)
                {
                    geometries_[i] = (Geometry[])geometries[i].Clone();
                    geometryData_[i].center_ = geometryCenters[i];

                    batches[i].geometry = geometries_[i][0];
                    if (node_)
                    {
                        batches[i].worldTransform = node_.worldTransform_;
                    }
                    batches[i].numWorldTransforms = 1;

                    var m = GetMaterial(i);
                    if(m == null)
                    {
                        Material mat = model.GetMaterial(i);
                        if (mat)
                        {
                            SetMaterial(i, mat);
                        }
                    }
                   
                }

                SetBoundingBox(model.BoundingBox);
                ResetLodLevels();
            }
            else
            {
                SetNumGeometries(0);
                SetBoundingBox(BoundingBox.Empty);
            }

            return this;
        }

        public void SetMaterialAttr(string[] resourceRef)
        {
            for (int i = 0; i < resourceRef.Length; i++)
            {
                Material mat = Resources.Instance.Load<Material>(resourceRef[i]);
                SetMaterial(i, mat);
            }

        }

        public override void UpdateBatches(in FrameInfo frame)
        {
            ref BoundingBox worldBoundingBox = ref WorldBoundingBox;
            
            distance_ = frame.camera.GetDistance(worldBoundingBox.Center);

            if (batches.Length == 1)
                batches[0].distance = distance_;
            else
            {
                ref mat4 worldTransform = ref node_.WorldTransform;
                for (int i = 0; i < batches.Length; ++i)
                {
                    batches[i].worldTransform = node_.worldTransform_;
                    vec3.Transform(geometryData_[i].center_, worldTransform, out vec3 worldCenter);
                    batches[i].distance = frame.camera.GetDistance(worldCenter);
                }
            }

            float scale = vec3.Dot(worldBoundingBox.Size, (vec3)MathUtil.DotScale);
            float newLodDistance = frame.camera.GetLodDistance(distance_, scale, lodBias_);
            
            if (newLodDistance != lodDistance_)
            {
                lodDistance_ = newLodDistance;
                CalculateLodLevels();
            }
        }

        public override Geometry GetLodGeometry(int batchIndex, int level)
        {
            if (batchIndex >= geometries_.Length)
                return null;

            // If level is out of range, use visible geometry
            if (level < geometries_[batchIndex].Length)
                return geometries_[batchIndex][level];
            else
                return batches[batchIndex].geometry;
        }


        protected void ResetLodLevels()
        {
            // Ensure that each subgeometry has at least one LOD level, and reset the current LOD level
            for (int i = 0; i < geometryData_.Length; ++i)
            {
                if (geometries_[i] == null ||　geometries_[i].Length == 0)
                    Array.Resize(ref geometries_[i], 1);

                batches[i].geometry = geometries_[i][0];
                geometryData_[i].lodLevel_ = 0;
            }

            // Find out the real LOD levels on next geometry update
            lodDistance_ = float.PositiveInfinity;
        }

        protected void CalculateLodLevels()
        {
            for (int i = 0; i < batches.Length; ++i)
            {
                Geometry[] batchGeometries = geometries_[i];
                // If only one LOD geometry, no reason to go through the LOD calculation
                if (batchGeometries.Length <= 1)
                    continue;

                int j;
                for (j = 1; j < batchGeometries.Length; ++j)
                {
                    if (batchGeometries[j] != null && lodDistance_ <= batchGeometries[j].LodDistance)
                        break;
                }

                ref StaticModelGeometryData geoData = ref geometryData_[i];
                int newLodLevel = j - 1;
                if (geoData.lodLevel_ != newLodLevel)
                {
                    geoData.lodLevel_ = newLodLevel;
                    batches[i].geometry = batchGeometries[newLodLevel];
                }
            }
        }

        protected struct StaticModelGeometryData
        {
            /// Geometry center.
            public vec3 center_;
            /// Current LOD level.
            public int lodLevel_;
        };

    }

}
