using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    /// Skeletal animation keyframe.
    [DataContract]
    public struct AnimationKeyFrame
    {
        /// Keyframe time.
        public float time_;
        /// Bone position.
        public Vector3 position_;
        /// Bone rotation.
        public Quaternion rotation_;
        /// Bone scale.
        public Vector3 scale_;

        public static AnimationKeyFrame Null = new AnimationKeyFrame();
    };

    /// Skeletal animation track, stores keyframes of a single bone.
    [DataContract]
    public class AnimationTrack
    {
        /// Bone or scene node name.
        public StringID Name { get; set; }
        /// Bitmask of included data (position, rotation, scale.)
        public byte channelMask_;
        /// Keyframes.
        public FastList<AnimationKeyFrame> keyFrames_;

        static int CompareKeyFrames(AnimationKeyFrame lhs, AnimationKeyFrame rhs)
        {
            return Math.Sign(lhs.time_ - rhs.time_);
        }

        public void SetKeyFrame(int index, ref AnimationKeyFrame keyFrame)
        {
            if (index < keyFrames_.Count)
            {
                keyFrames_[index] = keyFrame;
                keyFrames_.Sort(CompareKeyFrames);
            }
            else if (index == keyFrames_.Count)
                AddKeyFrame(ref keyFrame);
        }

        public void AddKeyFrame(ref AnimationKeyFrame keyFrame)
        {
            bool needSort = keyFrames_.Count > 0 ? keyFrames_[keyFrames_.Count - 1].time_ > keyFrame.time_ : false;
            keyFrames_.Add(keyFrame);
            if (needSort)
                keyFrames_.Sort(CompareKeyFrames);
        }

        public void InsertKeyFrame(int index, ref AnimationKeyFrame keyFrame)
        {
            keyFrames_.Insert(index, keyFrame);
            keyFrames_.Sort(CompareKeyFrames);
        }

        public void RemoveKeyFrame(int index)
        {
            keyFrames_.RemoveAt(index);
        }

        public void RemoveAllKeyFrames()
        {
            keyFrames_.Clear();
        }

        public ref AnimationKeyFrame GetKeyFrame(int index)
        {
            if (index < keyFrames_.Count)
                return ref keyFrames_.At(index);
            else
                return ref AnimationKeyFrame.Null;
        }

        public void GetKeyFrameIndex(float time, ref int index)
        {
            if (time < 0.0f)
                time = 0.0f;

            if (index >= keyFrames_.Count)
                index = keyFrames_.Count - 1;

            // Check for being too far ahead
            while (index > 0 && time < keyFrames_[index].time_)
                --index;

            // Check for being too far behind
            while (index < keyFrames_.Count - 1 && time >= keyFrames_[index + 1].time_)
                ++index;
        }

    };

    /// %Animation trigger point.
    [DataContract]
    public struct AnimationTriggerPoint
    {
        /// Trigger time.
        public float time_;
        /// Trigger data.
        public object data_;
    };

    public class Animation : Resource
    {
        public const byte CHANNEL_POSITION = 0x1;
        public const byte CHANNEL_ROTATION = 0x2;
        public const byte CHANNEL_SCALE = 0x4;

        /// Animation name.
        [DataMember]
        public StringID AnimationName { get; set; }

        /// Animation length. 
        [DataMember]
        public float Length { get => length_; set => length_ = Math.Max(value, 0.0f); }
        float length_;

        /// Animation tracks.
        [DataMember]
        public Dictionary<StringID, AnimationTrack> Tracks { get; set; } = new Dictionary<StringID, AnimationTrack>();

        /// Animation trigger points.
        [DataMember]
        public List<AnimationTriggerPoint> Triggers { get; set; } = new List<AnimationTriggerPoint>();


        static int CompareTriggers(AnimationTriggerPoint lhs, AnimationTriggerPoint rhs)
        {
            return Math.Sign(lhs.time_ - rhs.time_);
        }

        public AnimationTrack GetTrack(StringID name)
        {
            if (Tracks.TryGetValue(name, out var track))
            {
                return track;
            }

            return null;
        }

        public AnimationTriggerPoint? GetTrigger(int index)
        {
            return index < Triggers.Count ? new AnimationTriggerPoint?(Triggers[index]) : null;
        }

        public override bool Load(File source)
        {
            int memoryUse = Unsafe.SizeOf<Animation>();

            // Check ID
            if (source.ReadFileID() != "UANI")
            {
             //   Log.Error(source.Name + " is not a valid animation file");
                return false;
            }

            // Read name and length
            AnimationName = source.ReadCString();
            //animationNameHash_ = animationName_;
            length_ = source.Read<float>();
            Tracks.Clear();

            int tracks = source.Read<int>();
            memoryUse += tracks * Unsafe.SizeOf<AnimationTrack>();

            // Read tracks
            for (int i = 0; i < tracks; ++i)
            {
                AnimationTrack newTrack = CreateTrack(source.ReadCString());
                newTrack.channelMask_ = source.Read<byte>();

                int keyFrames = source.Read<int>();
                newTrack.keyFrames_ = new FastList<AnimationKeyFrame>(keyFrames);
                memoryUse += keyFrames * Unsafe.SizeOf<AnimationKeyFrame>();

                // Read keyframes of the track
                for (int j = 0; j < keyFrames; ++j)
                {
                    AnimationKeyFrame newKeyFrame = new AnimationKeyFrame();
                    newKeyFrame.time_ = source.Read<float>();
                    if ((newTrack.channelMask_ & CHANNEL_POSITION) != 0)
                        newKeyFrame.position_ = source.Read<Vector3>();
                    if((newTrack.channelMask_ & CHANNEL_ROTATION) != 0)
                    {
                        Quat r = source.Read<Quat>();
                        newKeyFrame.rotation_ = new Quaternion(r.X, r.Y, r.Z, r.W);// source.Read<Quaternion>();
                    }

                    if ((newTrack.channelMask_ & CHANNEL_SCALE) != 0)
                        newKeyFrame.scale_ = source.Read<Vector3>();
                    newTrack.AddKeyFrame(ref newKeyFrame);
                }
            }

            //MemoryUse = memoryUse;
            return true;
        }


        public AnimationTrack CreateTrack(StringID name)
        {
            /// \todo When tracks / keyframes are created dynamically, memory use is not updated
            AnimationTrack oldTrack = GetTrack(name);
            if (oldTrack != null)
                return oldTrack;

            AnimationTrack newTrack = new AnimationTrack { Name = name };
            Tracks[name] = newTrack;
            // newTrack.nameHash_ = nameHash;
            return newTrack;
        }

        public bool RemoveTrack(StringID name)
        {
            if (Tracks.ContainsKey(name))
            {
                Tracks.Remove(name);
                return true;
            }
            else
                return false;
        }

        public void RemoveAllTracks()
        {
            Tracks.Clear();
        }

        public void SetTrigger(int index, AnimationTriggerPoint trigger)
        {
            if (index == Triggers.Count)
            {
                AddTrigger(trigger);
            }
            else if (index < Triggers.Count)
            {
                Triggers[index] = trigger;
                Triggers.Sort(CompareTriggers);
            }
        }

        public void AddTrigger(AnimationTriggerPoint trigger)
        {
            Triggers.Add(trigger);
            Triggers.Sort(CompareTriggers);
        }

        public void AddTrigger(float time, bool timeIsNormalized, object data)
        {
            AnimationTriggerPoint newTrigger;
            newTrigger.time_ = timeIsNormalized ? time * length_ : time;
            newTrigger.data_ = data;
            Triggers.Add(newTrigger);

            Triggers.Sort(CompareTriggers);
        }

        public void RemoveTrigger(int index)
        {
            if (index < Triggers.Count)
                Triggers.RemoveAt(index);
        }

        public void RemoveAllTriggers()
        {
            Triggers.Clear();
        }
        /*
        void SetNumTriggers(unsigned num)
        {
            triggers_.Resize(num);
        }

        SharedPtr<Animation> Clone(String cloneName) const
    {
        SharedPtr<Animation> ret(new Animation(context_));

        ret->SetName(cloneName);
        ret->SetAnimationName(animationName_);
        ret->length_ = length_;
        ret->tracks_ = tracks_;
        ret->triggers_ = triggers_;
        ret->CopyMetadata(*this);
        ret->SetMemoryUse(GetMemoryUse());

        return ret;
    }*/
        /*
        AnimationTrack GetTrack(int index)
        {
            if (index >= GetNumTracks())
                return null;

            int j = 0;
            for (HashMap<StringHash, AnimationTrack>::Iterator i = tracks_.Begin(); i != tracks_.End(); ++i)
            {
                if (j == index)
                    return &i->second_;

                ++j;
            }

            return null;
        }*/

    }
}
