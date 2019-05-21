using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{

    /// AnimatedModel bone hierarchy created.
    public struct BoneHierarchyCreated 
    {
        public Node Node { get; set; }
    };

    /// AnimatedModel animation trigger.
    public struct AnimationTrigger 
    {
        public Node Node { get; set; }
        public Animation Animation { get; set; }
        public StringID Name { get; set; }
        public float Time { get; set; }
        public object Data { get; set; }

    };

    /// AnimatedModel animation finished or looped.
    public struct AnimationFinished 
    {
        public Node Node { get; set; }
        public Animation Animation { get; set; }
        public StringID Name { get; set; }
        public bool Looped { get; set; }

    };


}
