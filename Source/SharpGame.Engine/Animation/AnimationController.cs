using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    /// Control data for an animation.
    public class AnimationControl
    {
        /// Animation resource name.
        String name_;
        /// Animation speed.
        float speed_ = 1.0f;
        /// Animation target weight.
        float targetWeight_;
        /// Animation weight fade time, 0 if no fade.
        float fadeTime_;
        /// Animation autofade on stop -time, 0 if disabled.
        float autoFadeTime_;
        /// Set time command time-to-live.
        float setTimeTtl_;
        /// Set weight command time-to-live.
        float setWeightTtl_;
        /// Set time command.
        ushort setTime_;
        /// Set weight command.
        byte setWeight_;
        /// Set time command revision.
        byte setTimeRev_;
        /// Set weight command revision.
        byte setWeightRev_;
        /// Sets whether this should automatically be removed when it finishes playing.
        bool removeOnCompletion_ = true;
    }

    public class AnimationController : Component
    {
    }
}
