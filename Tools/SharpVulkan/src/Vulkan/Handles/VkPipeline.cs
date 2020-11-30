using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{

    public unsafe partial struct VkBuffer : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyBuffer(Vulkan.device, this, null);
        }

    }

    public unsafe partial struct VkBufferView : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyBufferView(Vulkan.device, this, null);
        }

    }

    public unsafe partial struct VkImage : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyImage(Vulkan.device, this, null);
        }

    }

    public unsafe partial struct VkImageView : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyImageView(Vulkan.device, this, null);
        }

    }




    public unsafe partial struct VkShaderModule : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyShaderModule(Vulkan.device, this, null);
        }

    }

    public unsafe partial struct VkPipelineLayout : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyPipelineLayout(Vulkan.device, this, null);
        }

    }

    public unsafe partial struct VkPipeline : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyPipeline(Vulkan.device, this, null);            
        }

    }

    public unsafe partial struct VkRenderPass : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyRenderPass(Vulkan.device, this, null);
        }

    }

    public unsafe partial struct VkFramebuffer : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyFramebuffer(Vulkan.device, this, null);
        }

    }
}
