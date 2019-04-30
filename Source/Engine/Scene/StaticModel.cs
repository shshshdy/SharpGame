﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    public class StaticModel : Drawable
    {
        public Model resourceRef;

        /// All geometries.
        protected Geometry[][] geometries_;
        /// Extra per-geometry data.
        protected StaticModelGeometryData[] geometryData_ = new StaticModelGeometryData[0];
        /// Model.
        protected Model model_;

        protected struct StaticModelGeometryData
        {
            /// Geometry center.
            public Vector3 center_;
            /// Current LOD level.
            public int lodLevel_;
        };

        public StaticModel()
        {
        }

        public void SetBoundingBox(BoundingBox box)
        {
            boundingBox_ = box;
            OnMarkedDirty(node_);
        }

        protected override void SetNumGeometries(int num)
        {
            base.SetNumGeometries(num);
            
            Array.Resize(ref geometries_, num);
            Array.Resize(ref geometryData_, num);

            ResetLodLevels();
        }

        public unsafe void SetModel(Model model)
        {
            if (model == model_)
                return;

            if (!node_)
            {
                Log.Error("Can not set model while model component is not attached to a scene node");
                return;
            }

            model_ = model;

            if(model_ != null)
            {             
                SetNumGeometries(model.Geometries.Length);

                Geometry[][] geometries = model.Geometries;
                List<Vector3> geometryCenters = model.GeometryCenters;

                for(int i = 0; i < geometries_.Length; ++i)
                {
                    geometries_[i] = (Geometry[])geometries[i].Clone();
                    geometryData_[i].center_ = geometryCenters[i];

                    batches_[i].geometry_ = geometries_[i][0];                    
                    batches_[i].worldTransform_ = node_.worldTransform_;
                    batches_[i].numWorldTransforms_ = 1;
                }

                SetBoundingBox(model.BoundingBox);
                ResetLodLevels();

            }
            else
            {
                SetNumGeometries(0);
                SetBoundingBox(BoundingBox.Empty);
            }                      

        }

        public bool SetMaterial(int index, Material mat)
        {
            if (index >= batches_.Length)
            {
                Log.Error("Material index out of bounds");
                return false;
            }

            batches_[index].material_ = mat;
            return true;
        }

        public Material GetMaterial(int idx)
        {
            return idx < batches_.Length ? batches_[idx].material_ : null;
        }

        public override void UpdateBatches(ref FrameInfo frame)
        {
            ref BoundingBox worldBoundingBox = ref WorldBoundingBox;
            distance_ = frame.camera_.GetDistance(worldBoundingBox.Center);

            if (batches_.Length == 1)
                batches_[0].distance_ = distance_;
            else
            {
                ref Matrix worldTransform = ref node_.WorldTransform;
                for (int i = 0; i < batches_.Length; ++i)
                {
                    Vector3.Transform(ref geometryData_[i].center_, ref worldTransform, out Vector3 worldCenter);
                    batches_[i].distance_ = frame.camera_.GetDistance(worldCenter);
                }
            }

            float scale = Vector3.Dot(worldBoundingBox.Size, MathUtil.DotScale);
            float newLodDistance = frame.camera_.GetLodDistance(distance_, scale, lodBias_);

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
                return batches_[batchIndex].geometry_;
        }

        protected override void OnWorldBoundingBoxUpdate()
        {
            worldBoundingBox_ = boundingBox_.Transformed(ref node_.WorldTransform);
        }

        protected void ResetLodLevels()
        {
            // Ensure that each subgeometry has at least one LOD level, and reset the current LOD level
            for (int i = 0; i < geometryData_.Length; ++i)
            {
                if (geometries_[i] == null ||　geometries_[i].Length == 0)
                    Array.Resize(ref geometries_[i], 1);

                batches_[i].geometry_ = geometries_[i][0];
                geometryData_[i].lodLevel_ = 0;
            }

            // Find out the real LOD levels on next geometry update
            lodDistance_ = float.PositiveInfinity;
        }

        protected void CalculateLodLevels()
        {
            for (int i = 0; i < batches_.Length; ++i)
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
                    batches_[i].geometry_ = batchGeometries[newLodLevel];
                }
            }
        }

    }
}