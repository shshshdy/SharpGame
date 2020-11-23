﻿// Copyright (c) BobbyBao and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace SharpGame
{
    /// <summary>
    /// The exception class for errors that occur in vulka.
    /// </summary>
    public class VkException : Exception
    {
        /// <summary>
        /// Gets the result returned by Vulkan.
        /// </summary>
        public VkResult Result { get; }

        /// <summary>
        /// Gets if the result is considered an error.
        /// </summary>
        public bool IsError => Result < 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="VkException" /> class.
        /// </summary>
        public VkException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VkException" /> class.
        /// </summary>
        /// <param name="result">The result code that caused this exception.</param>
        /// <param name="message"></param>
        public VkException(VkResult result, string message = "Vulkan error occured")
            : base($"[{(int)result}] {result} - {message}")
        {
            Result = result;
        }

        protected VkException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        public VkException(string message)
            : base(message)
        {
        }

        public VkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
