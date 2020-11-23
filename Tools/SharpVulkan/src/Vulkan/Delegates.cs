// Copyright (c) BobbyBao and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;

namespace SharpGame
{
    public unsafe delegate VkBool32 vkDebugUtilsMessengerCallbackEXT(
        VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messageTypes,
        VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
        IntPtr userData);
}
