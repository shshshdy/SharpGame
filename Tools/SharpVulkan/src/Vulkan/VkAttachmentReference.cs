// Copyright (c) BobbyBao and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

namespace SharpGame
{
    /// <summary>
    ///  SStructure specifying an attachment reference.
    /// </summary>
    public partial struct VkAttachmentReference
    {
        public VkAttachmentReference(uint attachment, VkImageLayout layout)
        {
            this.attachment = attachment;
            this.layout = layout;
        }
    }
}
