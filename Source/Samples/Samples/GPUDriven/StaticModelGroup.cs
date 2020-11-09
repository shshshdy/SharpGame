using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{  
    // Per-instance data block
    public struct InstanceData
    {
        public vec3 pos;
        public vec3 rot;
        public float scale;
        public uint texIndex;
    }


    public class StaticModelGroup : StaticModel
    {
        const int OBJECT_INSTANCE_COUNT = 2048;
        // Circular range of plant distribution
        const float PLANT_RADIUS = 25.0f;

        // Contains the instanced data
        Buffer instanceBuffer;
        // Contains the indirect drawing commands
        Buffer indirectCommandsBuffer;

        NativeList<VkDrawIndexedIndirectCommand> indirectCommands = new NativeList<VkDrawIndexedIndirectCommand>();

        uint objectCount = 0;
        uint indirectDrawCount = 0;


        public override StaticModel SetModel(Model model)
        {
            if (model == model_)
                return this;

            model_ = model;

            if (model_ != null)
            {
                SetNumGeometries(model.Geometries.Count);

                List<Geometry[]> geometries = model.Geometries;
                List<vec3> geometryCenters = model.GeometryCenters;

                for (int i = 0; i < geometries_.Length; ++i)
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
                    if (m == null)
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


        // Prepare (and stage) a buffer containing the indirect draw commands
        void prepareIndirectData()
        {
            indirectCommands.Clear();
         
            // Create on indirect command for node in the scene with a mesh attached to it
            uint m = 0;
            foreach (var geo in model_.Geometries)
            {
                VkDrawIndexedIndirectCommand indirectCmd = new VkDrawIndexedIndirectCommand
                {
                    instanceCount = OBJECT_INSTANCE_COUNT,
                    firstInstance = m * OBJECT_INSTANCE_COUNT,
                    firstIndex = geo[0].IndexStart,
                    indexCount = geo[0].IndexCount
                };

                indirectCommands.Add(indirectCmd);

                m++;
                
            }

            indirectDrawCount = indirectCommands.Count;
 
            objectCount = 0;
            foreach (var indirectCmd in indirectCommands)
            {
                objectCount += indirectCmd.instanceCount;
            }

            indirectCommandsBuffer = Buffer.Create<VkDrawIndexedIndirectCommand>(BufferUsageFlags.IndirectBuffer, false, indirectCommands.Count, indirectCommands.Data);

        }

        NativeList<InstanceData> instanceData = new NativeList<InstanceData>();
        // Prepare (and stage) a buffer containing instanced data for the mesh draws
        void prepareInstanceData()
        {
            instanceData.Resize(objectCount);

            for (uint i = 0; i < objectCount; i++)
            {
                float theta = 2 * glm.pi * glm.random();
                float phi = glm.acos(1 - 2 * glm.random());
                instanceData[i].rot = glm.vec3(0.0f, glm.pi * glm.random(), 0.0f);
                instanceData[i].pos = glm.vec3(glm.sin(phi) * glm.cos(theta), 0.0f, glm.cos(phi)) * PLANT_RADIUS;
                instanceData[i].scale = 1.0f + glm.random() * 2.0f;
                instanceData[i].texIndex = i / OBJECT_INSTANCE_COUNT;
            }

            instanceBuffer = Buffer.Create<InstanceData>(BufferUsageFlags.VertexBuffer, false, instanceBuffer.Count, instanceData.Data);

        }
    }
}
