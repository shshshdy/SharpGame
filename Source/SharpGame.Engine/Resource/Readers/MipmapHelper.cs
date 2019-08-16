using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace SharpGame.ImageSharp
{
    /// <summary>
    /// Contains helper methods for dealing with mipmaps.
    /// </summary>
    internal static class MipmapHelper
    {
        /// <summary>
        /// Computes the number of mipmap levels in a texture.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns>The number of mipmap levels needed for a texture of the given dimensions.</returns>
        public static uint ComputeMipLevels(uint width, uint height)
        {
            return 1 + (uint)Math.Floor(Math.Log(Math.Max(width, height), 2));
        }

        public static int GetDimension(int largestLevelDimension, int mipLevel)
        {
            int ret = largestLevelDimension;
            for (int i = 0; i < mipLevel; i++)
            {
                ret /= 2;
            }

            return Math.Max(1, ret);
        }

        internal static Image<T>[] GenerateMipmaps<T>(Image<T> baseImage) where T : struct, IPixel<T>
        {
            uint mipLevelCount = MipmapHelper.ComputeMipLevels((uint)baseImage.Width, (uint)baseImage.Height);
            Image<T>[] mipLevels = new Image<T>[mipLevelCount];
            mipLevels[0] = baseImage;
            int i = 1;

            int currentWidth = baseImage.Width;
            int currentHeight = baseImage.Height;
            while (currentWidth != 1 || currentHeight != 1)
            {
                int newWidth = Math.Max(1, currentWidth / 2);
                int newHeight = Math.Max(1, currentHeight / 2);
                Image<T> newImage = baseImage.Clone(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                Debug.Assert(i < mipLevelCount);
                mipLevels[i] = newImage;

                i++;
                currentWidth = newWidth;
                currentHeight = newHeight;
            }

            Debug.Assert(i == mipLevelCount);

            return mipLevels;
        }
    }
}
