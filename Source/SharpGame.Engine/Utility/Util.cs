﻿using System;
using Vulkan;

namespace SharpGame
{
    public static class Util
    {
        public static void CheckResult(VkResult result)
        {
            if (result != VkResult.Success)
            {
                throw new InvalidOperationException("Call failed.");
            }
        }

    }
}
