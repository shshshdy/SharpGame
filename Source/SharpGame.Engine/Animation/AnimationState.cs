using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    /// %Animation blending mode.
    public enum AnimationBlendMode
    {
        // Lerp blending (default)
        ABM_LERP = 0,
        // Additive blending based on difference from bind pose
        ABM_ADDITIVE
    };

    /// %Animation instance per-track data.
    public struct AnimationStateTrack
    {
        /// Animation track.
        public AnimationTrack track_;
        /// Bone pointer.
        public Bone bone_;
        /// Scene node pointer.
        public Node node_;
        /// Blending weight.
        public float weight_;
        /// Last key frame.
        public int keyFrame_;

        public AnimationStateTrack(AnimationTrack animationTrack)
        {
            track_ = animationTrack;
            bone_ = null;
            node_ = null;
            weight_ = 1.0f;
            keyFrame_ = 0;
        }
    };

    public class AnimationState : DisposeBase
    {
        public Animation Animation => animation_;

        /// Return whether weight is nonzero.
        public bool IsEnabled => weight_ > 0.0f;

        /// Return whether looped.
        public bool IsLooped => looped_;

        /// Return blending weight.
        public float Weight => weight_;

        /// Return blending mode.
        public AnimationBlendMode BlendMode => blendingMode_;

        /// Return time position.
        public float Time => time_;

        public AnimatedModel Model => model_;

        public Node Node => node_;

        public Bone StartBone => model_ != null ? startBone_ : null;

        public float Length => animation_ != null ? animation_.Length : 0.0f;

        public byte Layer => layer_;

        /// Animated model (model mode.)
        AnimatedModel model_;
        /// Root scene node (node hierarchy mode.)
        Node node_;
        /// Animation.
        Animation animation_;
        /// Start bone.
        Bone startBone_;
        /// Per-track data.
        FastList<AnimationStateTrack> stateTracks_ = new FastList<AnimationStateTrack>();
        /// Looped flag.
        bool looped_ = false;
        /// Blending weight.
        float weight_ = 1.0f;
        /// Time position.
        float time_ = 0.0f;
        /// Blending layer.
        byte layer_ = 0;
        /// Blending mode.
        AnimationBlendMode blendingMode_ = AnimationBlendMode.ABM_LERP;

        public AnimationState(AnimatedModel model, Animation animation)
        {
            // Set default start bone (use all tracks.) 
            model_ = model;
            animation_ = animation;
            SetStartBone(null);
        }

        public AnimationState(Node node, Animation animation)
        {
            node_ = node;
            animation_ = animation;
            if(animation_)
            {
                // Setup animation track to scene node mapping
                if(node_)
                {
                    Dictionary<StringID, AnimationTrack> tracks = animation_.Tracks;
                    stateTracks_.Clear();

                    foreach(var i in tracks)
                    {
                        StringID nameHash = i.Value.Name;
                        AnimationStateTrack stateTrack = new AnimationStateTrack(i.Value);

                        if(node_.Name == nameHash || tracks.Count == 1)
                            stateTrack.node_ = node_;
                        else
                        {
                            Node targetNode = node_.GetChild(nameHash, true);
                            if(targetNode)
                                stateTrack.node_ = targetNode;
                            else
                                Log.Warn("Node " + i.Value.Name + " not found for node animation " + animation_.FileName);
                        }

                        if(stateTrack.node_)
                            stateTracks_.Add(stateTrack);
                    }
                }
            }
        }

        public void SetStartBone(Bone startBone)
        {
            if(!model_ || !animation_)
                return;

            Skeleton skeleton = model_.Skeleton;
            if(startBone == null)
            {
                Bone rootBone = skeleton.RootBone;
                if(rootBone == null)
                    return;
                startBone = rootBone;
            }

            // Do not reassign if the start bone did not actually change, and we already have valid bone nodes
            if(startBone == startBone_ && stateTracks_.Count > 0)
                return;

            startBone_ = startBone;

            Dictionary<StringID, AnimationTrack> tracks = animation_.Tracks;
            stateTracks_.Clear();

            if(startBone.node_ == null)
                return;

            foreach(var i in tracks)
            {
                AnimationStateTrack stateTrack = new AnimationStateTrack(i.Value);
                // Include those tracks that are either the start bone itself, or its children
                Bone trackBone = null;
                StringID nameHash = i.Value.Name;

                if(nameHash == startBone.name_)
                    trackBone = startBone;
                else
                {
                    Node trackBoneNode = startBone.node_.GetChild(nameHash, true);
                    if(trackBoneNode)
                        trackBone = skeleton.GetBone(nameHash);
                }

                if(trackBone != null && trackBone.node_)
                {
                    stateTrack.bone_ = trackBone;
                    stateTrack.node_ = trackBone.node_;
                    stateTracks_.Add(stateTrack);
                }
            }

            model_.MarkAnimationDirty();
        }

        public void SetLooped(bool looped)
        {
            looped_ = looped;
        }

        public void SetWeight(float weight)
        {
            // Weight can only be set in model mode. In node animation it is hardcoded to full
            if(model_)
            {
                weight = MathUtil.Clamp(weight, 0.0f, 1.0f);
                if(weight != weight_)
                {
                    weight_ = weight;
                    model_.MarkAnimationDirty();
                }
            }
        }

        public void SetBlendMode(AnimationBlendMode mode)
        {
            if(model_)
            {
                if(blendingMode_ != mode)
                {
                    blendingMode_ = mode;
                    model_.MarkAnimationDirty();
                }
            }
        }

        public void SetTime(float time)
        {
            if(!animation_)
                return;

            time = MathUtil.Clamp(time, 0.0f, animation_.Length);
            if(time != time_)
            {
                time_ = time;
                if(model_)
                    model_.MarkAnimationDirty();
            }
        }

        public void SetBoneWeight(int index, float weight, bool recursive)
        {
            if(index >= stateTracks_.Count)
                return;

            weight = MathUtil.Clamp(weight, 0.0f, 1.0f);

            if(weight != stateTracks_[index].weight_)
            {
                stateTracks_.At(index).weight_ = weight;
                if(model_)
                    model_.MarkAnimationDirty();
            }

            if(recursive)
            {
                Node boneNode = stateTracks_[index].node_;
                if(boneNode)
                {
                    List<Node> children = boneNode.Children;
                    for(int i = 0; i < children.Count; ++i)
                    {
                        int childTrackIndex = GetTrackIndex(children[i]);
                        if(childTrackIndex != -1)
                            SetBoneWeight(childTrackIndex, weight, true);
                    }
                }
            }
        }

        public void SetBoneWeight(StringID name, float weight, bool recursive)
        {
            SetBoneWeight(GetTrackIndex(name), weight, recursive);
        }

        public void AddWeight(float delta)
        {
            if(delta == 0.0f)
                return;

            SetWeight(Weight + delta);
        }


        public void AddTime(float delta)
        {
            if(!animation_ || (!model_ && !node_))
                return;

            float length = animation_.Length;
            if(delta == 0.0f || length == 0.0f)
                return;

            bool sendFinishEvent = false;

            float oldTime = Time;
            float time = oldTime + delta;
            if(looped_)
            {
                while(time >= length)
                {
                    time -= length;
                    sendFinishEvent = true;
                }
                while(time < 0.0f)
                {
                    time += length;
                    sendFinishEvent = true;
                }
            }

            SetTime(time);

            if(!looped_)
            {
                if(delta > 0.0f && oldTime < length && Time == length)
                    sendFinishEvent = true;
                else if(delta < 0.0f && oldTime > 0.0f && Time == 0.0f)
                    sendFinishEvent = true;
            }

            // Process finish event
            if(sendFinishEvent)
            {
                Node senderNode = model_ ? model_.Node : node_;
                AnimationFinished eventData = new AnimationFinished
                {
                    Node = senderNode,
                    Animation = animation_,
                    Name = animation_.AnimationName,
                    Looped = looped_
                };

                // Note: this may cause arbitrary deletion of animation states, including the one we are currently processing
                senderNode.SendEvent(ref eventData);
                //if (senderNode.Expired() || self.Expired())
                //    return;
            }

            // Process animation triggers
            if(animation_.Triggers.Count > 0)
            {
                bool wrap = false;

                if(delta > 0.0f)
                {
                    if(oldTime > time)
                    {
                        oldTime -= length;
                        wrap = true;
                    }
                }

                if(delta < 0.0f)
                {
                    if(time > oldTime)
                    {
                        time -= length;
                        wrap = true;
                    }
                }

                if(oldTime > time)
                    MathUtil.Swap(ref oldTime, ref time);

                List<AnimationTriggerPoint> triggers = animation_.Triggers;
                foreach(var i in triggers)
                {
                    float frameTime = i.time_;
                    if(looped_ && wrap)
                        frameTime = frameTime % length;

                    if(oldTime <= frameTime && time > frameTime)
                    {
                        Node senderNode = model_ ? model_.Node : node_;

                        AnimationTrigger eventData = new AnimationTrigger
                        {
                            Node = senderNode,
                            Animation = animation_,
                            Name = animation_.AnimationName,
                            Time = i.time_,
                            Data = i.data_
                        };

                        // Note: this may cause arbitrary deletion of animation states, including the one we are currently processing
                        senderNode.SendEvent(ref eventData);
                        //if (senderNode.Expired() || self.Expired())
                        //    return;
                    }
                }
            }
        }

        public void SetLayer(byte layer)
        {
            if(layer != layer_)
            {
                layer_ = layer;
                if(model_)
                    model_.MarkAnimationOrderDirty();
            }
        }

        public float GetBoneWeight(int index)
        {
            return index < stateTracks_.Count ? stateTracks_[index].weight_ : 0.0f;
        }
        
        public float GetBoneWeight(StringID nameHash)
        {
            return GetBoneWeight(GetTrackIndex(nameHash));
        }

        public int GetTrackIndex(StringID name)
        {
            for(int i = 0; i < stateTracks_.Count; ++i)
            {
                Node node = stateTracks_[i].node_;
                if(node && node.Name == name)
                    return i;
            }

            return -1;
        }

        public int GetTrackIndex(Node node)
        {
            for(int i = 0; i < stateTracks_.Count; ++i)
            {
                if(stateTracks_[i].node_ == node)
                    return i;
            }

            return -1;
        }

        public void Apply()
        {
            if(!animation_ || !IsEnabled)
                return;

            if(model_)
                ApplyToModel();
            else
                ApplyToNodes();
        }


        void ApplyToModel()
        {
            for(int i = 0; i < stateTracks_.Count; i++)
            {
                ref AnimationStateTrack stateTrack = ref stateTracks_.At(i);
                float finalWeight = weight_ * stateTrack.weight_;

                // Do not apply if zero effective weight or the bone has animation disabled
                if (Equals(finalWeight, 0.0f) || !stateTrack.bone_.animated_)
                    continue;

                ApplyTrack(ref stateTrack, finalWeight, true);
            }
        }

        void ApplyToNodes()
        {
            // When applying to a node hierarchy, can only use full weight (nothing to blend to)
            for(int i = 0; i < stateTracks_.Count; i++)
            {
                ref AnimationStateTrack stateTrack = ref stateTracks_.At(i);
                ApplyTrack(ref stateTrack, 1.0f, false);
            }
        }

        void ApplyTrack(ref AnimationStateTrack stateTrack, float weight, bool silent)
        {
            AnimationTrack track = stateTrack.track_;
            Node node = stateTrack.node_;

            if(track.keyFrames_.Count == 0 || !node)
                return;

            ref int frame = ref stateTrack.keyFrame_;
            track.GetKeyFrameIndex(time_, ref frame);

            // Check if next frame to interpolate to is valid, or if wrapping is needed (looping animation only)
            int nextFrame = frame + 1;
            bool interpolate = true;
            if(nextFrame >= track.keyFrames_.Count)
            {
                if(!looped_)
                {
                    nextFrame = frame;
                    interpolate = false;
                }
                else
                    nextFrame = 0;
            }

            ref AnimationKeyFrame keyFrame = ref track.keyFrames_.At(frame);
            byte channelMask = track.channelMask_;

            vec3 newPosition = vec3.Zero;
            quat newRotation = quat.Identity;
            vec3 newScale = vec3.Zero;

            if(interpolate)
            {
                ref AnimationKeyFrame nextKeyFrame = ref track.keyFrames_.At(nextFrame);
                float timeInterval = nextKeyFrame.time_ - keyFrame.time_;
                if(timeInterval < 0.0f)
                    timeInterval += animation_.Length;
                float t = timeInterval > 0.0f ? (time_ - keyFrame.time_) / timeInterval : 1.0f;

                if((channelMask & Animation.CHANNEL_POSITION) != 0)
                    glm.lerp(in keyFrame.position_, in nextKeyFrame.position_, t, out newPosition);
                if ((channelMask & Animation.CHANNEL_ROTATION) != 0)
                    newRotation = glm.slerp(keyFrame.rotation_, nextKeyFrame.rotation_, t);
                    //glm.slerp(in keyFrame.rotation_, in nextKeyFrame.rotation_, t, out newRotation);
                if ((channelMask & Animation.CHANNEL_SCALE) != 0)
                    glm.lerp(in keyFrame.scale_, in nextKeyFrame.scale_, t, out newScale);
            }
            else
            {
                if((channelMask & Animation.CHANNEL_POSITION) != 0)
                    newPosition = keyFrame.position_;
                if((channelMask & Animation.CHANNEL_ROTATION) != 0)
                    newRotation = keyFrame.rotation_;
                if((channelMask & Animation.CHANNEL_SCALE) != 0)
                    newScale = keyFrame.scale_;
            }

            if(blendingMode_ == AnimationBlendMode.ABM_ADDITIVE) // not ABM_LERP
            {
                if((channelMask & Animation.CHANNEL_POSITION) != 0)
                {
                    vec3 delta = newPosition - stateTrack.bone_.initialPosition_;
                    newPosition = node.Position + delta * weight;
                }
                if((channelMask & Animation.CHANNEL_ROTATION) != 0)
                {
                    quat delta =glm.inverse(stateTrack.bone_.initialRotation_)* newRotation;
                    newRotation = glm.normalize(node.Rotation * delta);
                   // newRotation.Normalize();
                    if (!Equals(weight, 1.0f))
                        newRotation = glm.slerp(node.RotationRef, newRotation, weight);
                        //quat.Slerp(in node.RotationRef, in newRotation, weight, out newRotation);
                }
                if((channelMask & Animation.CHANNEL_SCALE) != 0)
                {
                    vec3 delta = newScale - stateTrack.bone_.initialScale_;
                    newScale = node.Scaling + delta * weight;
                }
            }
            else
            {
                if(!Equals(weight, 1.0f)) // not full weight
                {
                    if((channelMask & Animation.CHANNEL_POSITION) != 0)
                        glm.lerp(in node.PositionRef, in newPosition, weight, out newPosition);
                    if ((channelMask & Animation.CHANNEL_ROTATION) != 0)
                        //quat.Slerp(in node.RotationRef, in newRotation, weight, out newRotation);
                        newRotation = glm.slerp(node.RotationRef, newRotation, weight);
                    if ((channelMask & Animation.CHANNEL_SCALE) != 0)
                        glm.lerp(in node.ScalingRef, in newScale, weight, out newScale);
                }
            }

            if(silent)
            {
                if((channelMask & Animation.CHANNEL_POSITION) != 0)
                    node.PositionRef = newPosition;
                if((channelMask & Animation.CHANNEL_ROTATION) != 0)
                    node.RotationRef = newRotation;
                if((channelMask & Animation.CHANNEL_SCALE) != 0)
                    node.ScalingRef = newScale;
            }
            else
            {
                if((channelMask & Animation.CHANNEL_POSITION) != 0)
                    node.Position = newPosition;
                if((channelMask & Animation.CHANNEL_ROTATION) != 0)
                    node.Rotation = newRotation;
                if((channelMask & Animation.CHANNEL_SCALE) != 0)
                    node.Scaling = newScale;
            }
        }






    }
}
