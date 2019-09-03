using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class AnimationReader : ResourceReader<Animation>
    {
        public AnimationReader() : base(".anim")
        {
        }
        protected override bool OnLoad(Animation model, File source)
        {
            int memoryUse = Unsafe.SizeOf<Animation>();

            // Check ID
            if (source.ReadFileID() != "UANI")
            {
                //   Log.Error(source.Name + " is not a valid animation file");
                return false;
            }

            // Read name and length
            model.AnimationName = source.ReadCString();
            model.NameHash = model.AnimationName.GetHashCode();
            model.Length = source.Read<float>();
            model.Tracks.Clear();

            int tracks = source.Read<int>();
            memoryUse += tracks * Unsafe.SizeOf<AnimationTrack>();

            // Read tracks
            for (int i = 0; i < tracks; ++i)
            {
                AnimationTrack newTrack = model.CreateTrack(source.ReadCString());
                newTrack.channelMask_ = source.Read<byte>();

                int keyFrames = source.Read<int>();
                newTrack.keyFrames_ = new FastList<AnimationKeyFrame>(keyFrames);
                memoryUse += keyFrames * Unsafe.SizeOf<AnimationKeyFrame>();

                // Read keyframes of the track
                for (int j = 0; j < keyFrames; ++j)
                {
                    AnimationKeyFrame newKeyFrame = new AnimationKeyFrame();
                    newKeyFrame.time_ = source.Read<float>();
                    if ((newTrack.channelMask_ & Animation.CHANNEL_POSITION) != 0)
                        newKeyFrame.position_ = source.Read<vec3>();
                    if ((newTrack.channelMask_ & Animation.CHANNEL_ROTATION) != 0)
                    {
                        Quat r = source.Read<Quat>();
                        newKeyFrame.rotation_ = new quat(r.W, r.X, r.Y, r.Z);// source.Read<quat>();
                    }

                    if ((newTrack.channelMask_ & Animation.CHANNEL_SCALE) != 0)
                        newKeyFrame.scale_ = source.Read<vec3>();
                    newTrack.AddKeyFrame(ref newKeyFrame);
                }
            }

            model.MemoryUse = memoryUse;
            return true;
        }
    }
}
