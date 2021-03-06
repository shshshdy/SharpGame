﻿using System;
using System.Collections.Generic;
using System.Text;


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


    public class StaticModelGroup : Drawable
    {
        const int OBJECT_INSTANCE_COUNT = 2048;
        // Circular range of plant distribution
        const float PLANT_RADIUS = 25.0f;

        // Contains the instanced data
        Buffer instanceBuffer;
        // Contains the indirect drawing commands
        Buffer indirectCommandsBuffer;

        Vector<VkDrawIndexedIndirectCommand> indirectCommands = new Vector<VkDrawIndexedIndirectCommand>();
        Vector<InstanceData> instanceData = new Vector<InstanceData>();

        uint objectCount = 0;
        uint indirectDrawCount = 0;
        GroupBatch batch;
        Model model_;

        public StaticModelGroup()
        {
            batch = new GroupBatch
            {
                geometryType = GeometryType,
                geometry = new Geometry(),
            };

            batches = new[] { batch };
        }

        public void SetModel(Model model)
        {
            if (model == model_)
                return;

            model_ = model;

            if (model_ != null)
            {
                batch.geometry.VertexBuffer = model.VertexBuffers[0];
                batch.geometry.IndexBuffer = model.IndexBuffers[0];
                if (node_)
                {
                    batch.worldTransform = node_.worldTransform_;
                }

                batch.numWorldTransforms = 1;
          
            }
            else
            {
            }

            PrepareIndirectData();
            PrepareInstanceData();
        }


        // Prepare (and stage) a buffer containing the indirect draw commands
        void PrepareIndirectData()
        {
            indirectCommands.Clear();
            batch.triangleCount = 0;
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
                batch.triangleCount += indirectCmd.indexCount * indirectCmd.instanceCount;
            }

            indirectDrawCount = indirectCommands.Count;

            objectCount = 0;
            foreach (var indirectCmd in indirectCommands)
            {
                objectCount += indirectCmd.instanceCount;
            }

            indirectCommandsBuffer = Buffer.Create<VkDrawIndexedIndirectCommand>(VkBufferUsageFlags.IndirectBuffer, false, indirectCommands.Count, indirectCommands.Data);

        }

        // Prepare (and stage) a buffer containing instanced data for the mesh draws
        void PrepareInstanceData()
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

            instanceBuffer = Buffer.Create<InstanceData>(VkBufferUsageFlags.VertexBuffer, false, instanceData.Count, instanceData.Data);
            this.boundingBox_ = new BoundingBox(-PLANT_RADIUS, PLANT_RADIUS);
        }

        public override void UpdateBatches(in FrameInfo frame)
        {
            ref BoundingBox worldBoundingBox = ref WorldBoundingBox;
            distance_ = frame.camera.GetDistance(worldBoundingBox.Center);

            batch.distance = distance_;

            ref mat4 worldTransform = ref node_.WorldTransform;

            batch.worldTransform = node_.worldTransform_;
            batch.instanceBuffer = instanceBuffer;
            batch.indirectCommandsBuffer = indirectCommandsBuffer;
            batch.indirectDrawCount = indirectDrawCount;

        }


        public class GroupBatch : SourceBatch
        {
            // Contains the instanced data
            public Buffer instanceBuffer;
            // Contains the indirect drawing commands
            public Buffer indirectCommandsBuffer;

            public uint indirectDrawCount;
            public long triangleCount = 0;

            public override void Draw(CommandBuffer cb, Span<ConstBlock> pushConsts, DescriptorSet resourceSet, Span<DescriptorSet> resourceSet1, Pass pass)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet, offset);

                int firstSet = 1;
                foreach (var rs in resourceSet1)
                {
                    if (firstSet < pass.PipelineLayout.ResourceLayout.Length && rs != null)
                    {
                        cb.BindGraphicsResourceSet(pass.PipelineLayout, firstSet, rs, -1);
                    }
                    firstSet++;
                }

                foreach (ConstBlock constBlock in pushConsts)
                {
                    cb.PushConstants(pass.PipelineLayout, constBlock.range.stageFlags, constBlock.range.offset, constBlock.range.size, constBlock.data);
                }

                material.Bind(pass.passIndex, cb);

                cb.BindVertexBuffer(0, geometry.VertexBuffer);
                cb.BindVertexBuffer(1, instanceBuffer);
                cb.BindIndexBuffer(geometry.IndexBuffer, 0, VkIndexType.Uint32);

                cb.DrawIndexedIndirect(indirectCommandsBuffer, 0, indirectDrawCount, 20/*(uint)sizeof(VkDrawIndexedIndirectCommand)*/);

                Stats.indirectTriCount += triangleCount;
            }

        }


    }
}
