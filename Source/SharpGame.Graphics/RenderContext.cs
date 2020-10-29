using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public enum SubmitQueue
    {
        EarlyGraphics = 0,
        Compute = 1,
        Graphics = 2,
        MaxCount
    }

    public struct SubmitQueueData
    {
        public CommandBuffer cmdBuffer;
        public Semaphore semaphore;
        public Fence submitFence;
        public PipelineStageFlags pipelineStageFlags;
    }

    public class RenderContext
    {
        public int id;
        public int imageIndex;

        public Semaphore acquireSemaphore;

        public Semaphore preRenderSemaphore => submitQueue[0].semaphore;
        public Semaphore computeSemaphore => submitQueue[1].semaphore;
        public Semaphore renderSemaphore => submitQueue[2].semaphore;

        public Fence presentFence;

        public CommandBuffer RenderCmdBuffer => submitQueue[2].cmdBuffer;

        public SubmitQueueData[] submitQueue = new SubmitQueueData[(int)SubmitQueue.MaxCount];

        private TransientBufferManager transientVB = new TransientBufferManager(BufferUsageFlags.VertexBuffer, 1024 * 1024);
        private TransientBufferManager transientIB = new TransientBufferManager(BufferUsageFlags.IndexBuffer, 1024 * 1024);


        public List<System.Action> postActions = new List<Action>();

        //
        static CommandBufferPool[] pools = new CommandBufferPool[(int)SubmitQueue.MaxCount];

        public RenderContext(int id)
        {
            this.id = id;

            acquireSemaphore = new Semaphore();

            for(int i = 0; i < (int)SubmitQueue.MaxCount; i++)
            {
                submitQueue[i] = new SubmitQueueData
                {
                    cmdBuffer = pools[i].AllocateCommandBuffer(CommandBufferLevel.Primary),
                    submitFence = new Fence(FenceCreateFlags.Signaled),
                    semaphore = new Semaphore(),
                    pipelineStageFlags = (i == (int)SubmitQueue.Compute ? PipelineStageFlags.ComputeShader : PipelineStageFlags.FragmentShader)
                };
            }

        }

        public static void Init()
        {
            pools[0] = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);
            pools[1] = new CommandBufferPool(Device.QFCompute, CommandPoolCreateFlags.ResetCommandBuffer);
            pools[2] = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);
        }

        public static void Shutdown()
        {
            foreach(var pool in pools)
            {
                pool.Dispose();
            }
        }

        public CommandBuffer GetCmdBuffer(SubmitQueue queue)
        {
            return submitQueue[(int)queue].cmdBuffer;
        }

        public void DeviceLost()
        {
            for (int i = 0; i < (int)SubmitQueue.MaxCount; i++)
            {
                pools[i].FreeCommandBuffer(submitQueue[i].cmdBuffer);
            }

        }

        public void DeviceReset()
        {
            for (int i = 0; i < (int)SubmitQueue.MaxCount; i++)
            {
                submitQueue[i].cmdBuffer = pools[i].AllocateCommandBuffer(CommandBufferLevel.Primary);
            }
        }

        public void Begin()
        {
            for (int i = 0; i < (int)SubmitQueue.MaxCount; i++)
            {
                submitQueue[i].cmdBuffer.Begin();
            }

        }

        public void End()
        {
            for (int i = 0; i < (int)SubmitQueue.MaxCount; i++)
            {
                submitQueue[i].cmdBuffer.End();
            }

        }

        public void Submit(Action<RenderContext, SubmitQueue> onSubmit)
        {
            var sem = this.acquireSemaphore;
            for (int i = 0; i < (int)SubmitQueue.MaxCount; i++)
            {
                var fence = submitQueue[i].submitFence;
                fence.Wait();
                fence.Reset();

                CommandBuffer cmdBuffer = submitQueue[i].cmdBuffer;

                if(i == (int)SubmitQueue.Compute)
                {
                    Graphics.ComputeQueue.Submit(sem, submitQueue[i].pipelineStageFlags,
                        cmdBuffer, submitQueue[i].semaphore, fence);
                }
                else
                {
                    Graphics.GraphicsQueue.Submit(sem, submitQueue[i].pipelineStageFlags,
                        cmdBuffer, submitQueue[i].semaphore, fence);
                }

                onSubmit?.Invoke(this, (SubmitQueue)i);
                sem = submitQueue[i].semaphore;
            }

        }

        public void Post(System.Action action)
        {
            postActions.Add(action);
        }

        public TransientBuffer AllocVertexBuffer(uint count)
        {
            return transientVB.Alloc(count);
        }

        public TransientBuffer AllocIndexBuffer(uint count)
        {
            return transientIB.Alloc(count);
        }

        public void ResetBuffers()
        {
            transientVB.Reset();
            transientIB.Reset();
        }

        public void FlushBuffers()
        {
            transientVB.Flush();
            transientIB.Flush();
        }

    }
}
