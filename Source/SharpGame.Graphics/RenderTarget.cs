using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct Attachment
    {
        public Format format;
        public SampleCountFlags samples;
        public ImageUsageFlags usage;

        public Attachment(Format fmt = Format.Undefined, SampleCountFlags sampleCountFlags = SampleCountFlags.Count1, ImageUsageFlags imageUsageFlags = ImageUsageFlags.ColorAttachment)
        {
            format = fmt;
            samples = sampleCountFlags;
            usage = imageUsageFlags;
        }

    }

    public class RenderTarget
    {
        Extent3D extent;

        List<Image> images;

        List<ImageView> views;

        List<Attachment> attachments;

        /// By default there are no input attachments
        List<uint> input_attachments;

        /// By default the output attachments is attachment 0
        List<uint> output_attachments;
    }
}
