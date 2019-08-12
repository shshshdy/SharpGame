using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SharpGame
{
    /// Control data for an animation.
    public class AnimationControl
    {
        /// Animation resource name.
        public String name_;
        public int hash_;
        /// Animation speed.
        public float speed_ = 1.0f;
        /// Animation target weight.
        public float targetWeight_;
        /// Animation weight fade time, 0 if no fade.
        public float fadeTime_;
        /// Animation autofade on stop -time, 0 if disabled.
        public float autoFadeTime_;
        /// Set time command time-to-live.
        public float setTimeTtl_;
        /// Set weight command time-to-live.
        public float setWeightTtl_;
        /// Set time command.
        public ushort setTime_;
        /// Set weight command.
        public byte setWeight_;
        /// Set time command revision.
        public byte setTimeRev_;
        /// Set weight command revision.
        public byte setWeightRev_;
        /// Sets whether this should automatically be removed when it finishes playing.
        public bool removeOnCompletion_ = true;
    }

    public class AnimationController : Component
    {
        /// Animation control structures.
        List<AnimationControl> animations_ = new List<AnimationControl>();
        /// Node hierarchy mode animation states.
        List<AnimationState> nodeAnimationStates_ = new List<AnimationState>();

        const byte CTRL_LOOPED = 0x1;
        const byte CTRL_STARTBONE = 0x2;
        const byte CTRL_AUTOFADE = 0x4;
        const byte CTRL_SETTIME = 0x08;
        const byte CTRL_SETWEIGHT = 0x10;
        const byte CTRL_REMOVEONCOMPLETION = 0x20;
        const byte CTRL_ADDITIVE = 0x40;
        const float EXTRA_ANIM_FADEOUT_TIME = 0.1f;
        const float COMMAND_STAY_TIME = 0.25f;
        const uint MAX_NODE_ANIMATION_STATES = 256;

        public override void OnSetEnabled()
        {
            Scene scene = Scene;
            if (scene)
            {
                if (IsEnabledEffective())
                    this.Subscribe<ScenePostUpdate>(scene, HandleScenePostUpdate);
                else
                    this.Unsubscribe<ScenePostUpdate>(scene, HandleScenePostUpdate);
            }
        }

        public virtual void Update(float timeStep)
        {
            // Loop through animations
            for (int i = 0; i < animations_.Count;)
            {
                AnimationControl ctrl = animations_[i];
                AnimationState state = GetAnimationState(ctrl.hash_);
                bool remove = false;

                if (!state)
                    remove = true;
                else
                {
                    // Advance the animation
                    if (ctrl.speed_ != 0.0f)
                        state.AddTime(ctrl.speed_ * timeStep);

                    float targetWeight = ctrl.targetWeight_;
                    float fadeTime = ctrl.fadeTime_;

                    // If non-looped animation at the end, activate autofade as applicable
                    if (!state.IsLooped && state.Time >= state.Length && ctrl.autoFadeTime_ > 0.0f)
                    {
                        targetWeight = 0.0f;
                        fadeTime = ctrl.autoFadeTime_;
                    }

                    // Process weight fade
                    float currentWeight = state.Weight;
                    if (currentWeight != targetWeight)
                    {
                        if (fadeTime > 0.0f)
                        {
                            float weightDelta = 1.0f / fadeTime * timeStep;
                            if (currentWeight < targetWeight)
                                currentWeight = Math.Min(currentWeight + weightDelta, targetWeight);
                            else if (currentWeight > targetWeight)
                                currentWeight = Math.Max(currentWeight - weightDelta, targetWeight);
                            state.SetWeight(currentWeight);
                        }
                        else
                            state.SetWeight(targetWeight);
                    }

                    // Remove if weight zero and target weight zero
                    if (state.Weight == 0.0f && (targetWeight == 0.0f || fadeTime == 0.0f) && ctrl.removeOnCompletion_)
                        remove = true;
                }

                // Decrement the command time-to-live values
                if (ctrl.setTimeTtl_ > 0.0f)
                    ctrl.setTimeTtl_ = Math.Max(ctrl.setTimeTtl_ - timeStep, 0.0f);
                if (ctrl.setWeightTtl_ > 0.0f)
                    ctrl.setWeightTtl_ = Math.Max(ctrl.setWeightTtl_ - timeStep, 0.0f);

                if (remove)
                {
                    if (state)
                        RemoveAnimationState(state);
                    animations_.RemoveAt(i);
                }
                else
                    ++i;
            }

            // Node hierarchy animations need to be applied manually
            foreach (var state in nodeAnimationStates_)
            {
                state.Apply();
            }

        }

        public bool Play(String name, byte layer, bool looped, float fadeInTime)
        {
            // Get the animation resource first to be able to get the canonical resource name
            // (avoids potential adding of duplicate animations)
            Animation newAnimation = Resources.Instance.Load<Animation>(name);
            if (!newAnimation)
                return false;

            // Check if already exists
            int index = FindAnimation(newAnimation.AnimationName, out var state);

            if (!state)
            {
                state = AddAnimationState(newAnimation);
                if (!state)
                    return false;
            }

            if (index == -1)
            {
                AnimationControl newControl = new AnimationControl();
                newControl.name_ = newAnimation.AnimationName;
                newControl.hash_ = newAnimation.NameHash;
                animations_.Push(newControl);
                index = animations_.Count - 1;
            }

            state.SetLayer(layer);
            state.SetLooped(looped);
            animations_[index].targetWeight_ = 1.0f;
            animations_[index].fadeTime_ = fadeInTime;
            return true;
        }

        public bool PlayExclusive(String name, byte layer, bool looped, float fadeTime)
        {
            bool success = Play(name, layer, looped, fadeTime);

            // Fade other animations only if successfully started the new one
            if (success)
                FadeOthers(name, 0.0f, fadeTime);

            return success;
        }

        public bool Stop(String name, float fadeOutTime)
        {
            int index = FindAnimation(name, out var state);
            if (index != -1)
            {
                animations_[index].targetWeight_ = 0.0f;
                animations_[index].fadeTime_ = fadeOutTime;
            }

            return index != -1 || state != null;
        }

        public void StopLayer(byte layer, float fadeOutTime)
        {
            bool needUpdate = false;
            foreach (var i in animations_)
            {
                AnimationState state = GetAnimationState(i.hash_);
                if (state && state.Layer == layer)
                {
                    i.targetWeight_ = 0.0f;
                    i.fadeTime_ = fadeOutTime;
                    needUpdate = true;
                }
            }

        }

        public void StopAll(float fadeOutTime)
        {
            if (animations_.Count > 0)
            {
                for (int i = 0; i < animations_.Count;)
                {
                    AnimationControl ctrl = animations_[i];
                    {
                        ctrl.targetWeight_ = 0.0f;
                        ctrl.fadeTime_ = fadeOutTime;
                    }

                }
            }
        }
        public bool Fade(String name, float targetWeight, float fadeTime)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;

            animations_[index].targetWeight_ = MathUtil.Clamp(targetWeight, 0.0f, 1.0f);
            animations_[index].fadeTime_ = fadeTime;
            return true;
        }

        public bool FadeOthers(string name, float targetWeight, float fadeTime)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;

            byte layer = state.Layer;

            bool needUpdate = false;
            for (int i = 0; i < animations_.Count; ++i)
            {
                if (i != index)
                {
                    AnimationControl control = animations_[i];
                    AnimationState otherState = GetAnimationState(control.hash_);
                    if (otherState && otherState.Layer == layer)
                    {
                        control.targetWeight_ = MathUtil.Clamp(targetWeight, 0.0f, 1.0f);
                        control.fadeTime_ = fadeTime;
                        needUpdate = true;
                    }
                }
            }

            return true;
        }

        public bool SetLayer(string name, byte layer)
        {
            AnimationState state = GetAnimationState(name);
            if (!state)
                return false;

            state.SetLayer(layer);
            return true;
        }

        public bool SetStartBone(string name, string startBoneName)
        {
            // Start bone can only be set in model mode
            AnimatedModel model = GetComponent<AnimatedModel>();
            if (!model)
                return false;

            AnimationState state = model.GetAnimationState(name);
            if (!state)
                return false;

            Bone bone = model.Skeleton.GetBone(startBoneName);
            state.SetStartBone(bone);

            return true;
        }

        public bool SetTime(string name, float time)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;

            time = MathUtil.Clamp(time, 0.0f, state.Length);
            state.SetTime(time);
            // Prepare "set time" command for network replication
            animations_[index].setTime_ = (ushort)(time / state.Length * 65535.0f);
            animations_[index].setTimeTtl_ = COMMAND_STAY_TIME;
            ++animations_[index].setTimeRev_;
            return true;
        }

        public bool SetSpeed(string name, float speed)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;


            animations_[index].speed_ = speed;
            return true;
        }

        public bool SetWeight(string name, float weight)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;


            weight = MathUtil.Clamp(weight, 0.0f, 1.0f);
            state.SetWeight(weight);
            // Prepare "set weight" command for network replication
            animations_[index].setWeight_ = (byte)(weight * 255.0f);
            animations_[index].setWeightTtl_ = COMMAND_STAY_TIME;
            ++animations_[index].setWeightRev_;
            // Cancel any ongoing weight fade
            animations_[index].targetWeight_ = weight;
            animations_[index].fadeTime_ = 0.0f;

            return true;
        }

        public bool SetRemoveOnCompletion(string name, bool removeOnCompletion)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;

            animations_[index].removeOnCompletion_ = removeOnCompletion;

            return true;
        }

        public bool SetLooped(string name, bool enable)
        {
            AnimationState state = GetAnimationState(name);
            if (!state)
                return false;

            state.SetLooped(enable);

            return true;
        }

        public bool SetBlendMode(string name, AnimationBlendMode mode)
        {
            AnimationState state = GetAnimationState(name);
            if (!state)
                return false;

            state.SetBlendMode(mode);

            return true;
        }

        public bool SetAutoFade(string name, float fadeOutTime)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;

            animations_[index].autoFadeTime_ = Math.Max(fadeOutTime, 0.0f);

            return true;
        }

        public bool IsPlaying(string name)
        {
            int index = FindAnimation(name, out var state);
            return index != -1;
        }

        public bool IsPlaying(byte layer)
        {
            foreach (var i in animations_)
            {
                AnimationState state = GetAnimationState(i.hash_);
                if (state && state.Layer == layer)
                    return true;
            }

            return false;
        }

        public bool IsFadingIn(string name)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;

            return animations_[index].fadeTime_ > 0 && animations_[index].targetWeight_ > state.Weight;
        }

        public bool IsFadingOut(string name)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;

            return (animations_[index].fadeTime_ > 0 && animations_[index].targetWeight_ < state.Weight)
                   || (!state.IsLooped && state.Time >= state.Length && animations_[index].autoFadeTime_ > 0);
        }

        public bool IsAtEnd(string name)
        {
            int index = FindAnimation(name, out var state);
            if (index == -1 || !state)
                return false;
            else
                return state.Time >= state.Length;
        }

        public byte GetLayer(string name)
        {
            AnimationState state = GetAnimationState(name);
            return (byte)(state ? state.Layer : 0);
        }

        public Bone GetStartBone(string name)
        {
            AnimationState state = GetAnimationState(name);
            return state ? state.StartBone : null;
        }

        public string GetStartBoneName(string name)
        {
            Bone bone = GetStartBone(name);
            return (bone != null) ? bone.name_ : string.Empty;
        }

        public float GetTime(string name)
        {
            AnimationState state = GetAnimationState(name);
            return state ? state.Time : 0.0f;
        }

        public float GetWeight(string name)
        {
            AnimationState state = GetAnimationState(name);
            return state ? state.Weight : 0.0f;
        }

        public bool IsLooped(string name)
        {
            AnimationState state = GetAnimationState(name);
            return state ? state.IsLooped : false;
        }

        public AnimationBlendMode GetBlendMode(string name)
        {
            AnimationState state = GetAnimationState(name);
            return state ? state.BlendMode : AnimationBlendMode.ABM_LERP;
        }

        public float GetLength(string name)
        {
            AnimationState state = GetAnimationState(name);
            return state ? state.Length : 0.0f;
        }

        public float GetSpeed(string name)
        {
            int index = FindAnimation(name, out var state);
            return index != -1 ? animations_[index].speed_ : 0.0f;
        }

        float GetFadeTarget(string name)
        {
            int index = FindAnimation(name, out var state);
            return index != -1 ? animations_[index].targetWeight_ : 0.0f;
        }

        public float GetFadeTime(string name)
        {
            int index = FindAnimation(name, out var state);
            return index != -1 ? animations_[index].targetWeight_ : 0.0f;
        }

        public float GetAutoFade(string name)
        {
            int index = FindAnimation(name, out var state);
            return index != -1 ? animations_[index].autoFadeTime_ : 0.0f;
        }

        public bool GetRemoveOnCompletion(string name)
        {
            int index = FindAnimation(name, out var state);
            return index != -1 ? animations_[index].removeOnCompletion_ : false;
        }

        public AnimationState GetAnimationState(string name)
        {
            return GetAnimationState(name.GetHashCode());
        }

        public AnimationState GetAnimationState(int nameHash)
        {
            // Model mode
            AnimatedModel model = GetComponent<AnimatedModel>();
            if (model)
                return model.GetAnimationState(nameHash);

            // Node hierarchy mode
            foreach (var i in nodeAnimationStates_)
            {
                Animation animation = i.Animation;
                if (animation.NameHash == nameHash)// || animation.AnimationNameHash == nameHash)
                    return i;
            }

            return null;
        }

        public override void OnSceneSet(Scene scene)
        {
            if (scene && IsEnabledEffective())
                this.Subscribe<ScenePostUpdate>(scene, HandleScenePostUpdate);
            else if (!scene)
                this.Unsubscribe<ScenePostUpdate>(scene, HandleScenePostUpdate);
        }

        AnimationState AddAnimationState(Animation animation)
        {
            if (!animation)
                return null;

            // Model mode
            AnimatedModel model = GetComponent<AnimatedModel>();
            if (model)
                return model.AddAnimationState(animation);

            // Node hierarchy mode
            AnimationState newState = new AnimationState(node_, animation);
            nodeAnimationStates_.Push(newState);
            return newState;
        }

        void RemoveAnimationState(AnimationState state)
        {
            if (!state)
                return;

            // Model mode
            AnimatedModel model = GetComponent<AnimatedModel>();
            if (model)
            {
                model.RemoveAnimationState(state);
                return;
            }

            // Node hierarchy mode
            for (int i = 0; i < nodeAnimationStates_.Count; i++)
            {
                if (nodeAnimationStates_[i] == state)
                {
                    nodeAnimationStates_.RemoveAt(i);
                    return;
                }
            }
        }

        int FindAnimation(string name, out AnimationState state)
        {
            int nameHash = FileUtil.GetInternalPath(name).GetHashCode();

            // Find the AnimationState
            state = GetAnimationState(nameHash);
            if (state)
            {
                // Either a resource name or animation name may be specified. We store resource names, so correct the hash if necessary
                nameHash = state.Animation.NameHash;
            }

            // Find the internal control structure           
            for (int i = 0; i < animations_.Count; ++i)
            {
                if (animations_[i].hash_ == nameHash)
                {
                    return i;                   
                }
            }

            return -1;
        }

        void HandleScenePostUpdate(ScenePostUpdate e)
        {
            Update(e.timeStep);
        }



    }

}