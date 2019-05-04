using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace SharpGame
{
    public enum GeometryType : byte
    {
        GEOM_STATIC = 0,
        GEOM_SKINNED = 1,
        GEOM_INSTANCED = 2,
        GEOM_BILLBOARD = 3,
        GEOM_DIRBILLBOARD = 4,
        GEOM_TRAIL_FACE_CAMERA = 5,
        GEOM_TRAIL_BONE = 6,
        MAX_GEOMETRYTYPES = 7,
        // This is not a real geometry type for VS, but used to mark objects that do not desire to be instanced
        GEOM_STATIC_NOINSTANCING = 7,
    }

    /// Rendering frame update parameters.
    public struct FrameInfo
    {
        /// Frame number.
        public int frameNumber_;
        /// Time elapsed since last frame.
        public float timeStep_;
        /// Viewport size.
        public Int2 viewSize_;
        /// Camera being used.
        public Camera camera_;
    };

    /// Source data for a 3D geometry draw call.
    public struct SourceBatch
    {
        /// Distance from camera.
        public float distance_;
        /// Geometry.
        public Geometry geometry_;
        /// Material.
        public Material material_;
        /// World transform(s). For a skinned model, these are the bone transforms.
        public IntPtr worldTransform_;
        /// Number of world transforms.
        public int numWorldTransforms_;
        /// Per-instance data. If not null, must contain enough data to fill instancing buffer.
        public IntPtr instancingData_;
        /// %Geometry type.
        public GeometryType geometryType_;
    };

    public unsafe abstract class Drawable : Component
    {
        public const uint DRAWABLE_GEOMETRY = 0x1;
        public const uint DRAWABLE_LIGHT = 0x2;
        public const uint DRAWABLE_ZONE = 0x4;
        public const uint DRAWABLE_GEOMETRY2D = 0x8;
        public const uint DRAWABLE_ANY = 0xff;

        [IgnoreDataMember]
        public int Index { get; set; } = -1;

        /// World-space bounding box.
        [IgnoreDataMember]
        public ref BoundingBox WorldBoundingBox
        {
            get
            {
                if (worldBoundingBoxDirty_)
                {
                    OnWorldBoundingBoxUpdate();
                    worldBoundingBoxDirty_ = false;
                }

                return ref worldBoundingBox_;
            }
        }
        protected BoundingBox worldBoundingBox_;

        /// Local-space bounding box.
        protected BoundingBox boundingBox_;

        /// Draw call source data.
        [IgnoreDataMember]
        public SourceBatch[] Batches => batches_;
        protected SourceBatch[] batches_ = new SourceBatch[0];

        /// Drawable flags.
        protected byte drawableFlags_;
        /// Bounding box dirty flag.
        protected bool worldBoundingBoxDirty_;
        /// Shadowcaster flag.
        protected bool castShadows_;
        /// Current distance to camera.
        protected float distance_;
        /// LOD scaled distance.
        protected float lodDistance_;
        /// Draw distance.
        protected float drawDistance_;
        /// Shadow distance.
        protected float shadowDistance_;
        /// LOD bias.
        protected float lodBias_ = 1.0f;
        /// Last visible frame number.
        protected int viewFrameNumber_;

        public Drawable()
        {
        }

        protected virtual void SetNumGeometries(int num)
        {            
            Array.Resize(ref batches_, num);
        }

        public override void OnSetEnabled()
        {
            bool enabled = IsEnabledEffective();

            if(enabled && Index == -1)
                AddToScene();
            else if(!enabled && Index != -1)
                RemoveFromScene();
        }

        public override void OnNodeSet(Node node)
        {
            if(node != null)
                node.AddListener(this);
        }

        public override void OnSceneSet(Scene scene)
        {
            if(scene != null)
                AddToScene();
            else
                RemoveFromScene();
        }

        public override void OnMarkedDirty(Node node)
        {
            worldBoundingBoxDirty_ = true;
            /*
            if(!updateQueued_ && octant_)
                octant_->GetRoot()->QueueUpdate(this);

            // Mark zone assignment dirty when transform changes
            if(node == node_)
                zoneDirty_ = true;*/
        }

        public void MarkForUpdate()
        {/*
            if (!updateQueued_ && octant_)
                octant_->GetRoot()->QueueUpdate(this);*/
        }

        void AddToScene()
        {
            // Do not add to octree when disabled
            if(!IsEnabledEffective())
                return;
           
            Scene scene = Scene;
            if(scene != null)
            {
                scene.InsertDrawable(this);
            }
            else
            {
                // We have a mechanism for adding detached nodes to an octree manually, so do not log this error
                Log.Error("Node is detached from scene, drawable will not render");
            }
        }

        void RemoveFromScene()
        {
            if(Index >= 0)
            {
                Scene scene = Scene;
                /*
                Octree* octree = octant_->GetRoot();
                if(updateQueued_)
                    octree->CancelUpdate(this);*/

                // Perform subclass specific deinitialization if necessary
                OnRemoveFromOctree();

                scene.RemoveDrawable(this);
            }
        }

        protected virtual void OnRemoveFromOctree()
        {
        }

        public virtual Geometry GetLodGeometry(int batchIndex, int level)
        {
            // By default return the visible batch geometry
            if (batchIndex < batches_.Length)
                return batches_[batchIndex].geometry_;
            else
                return null;
        }

        protected abstract void OnWorldBoundingBoxUpdate();

        public virtual void Update(ref FrameInfo frameInfo)
        {
            viewFrameNumber_ = frameInfo.frameNumber_;
        }

        public virtual void UpdateBatches(ref FrameInfo frame)
        {
            ref BoundingBox worldBoundingBox = ref WorldBoundingBox;
            IntPtr worldTransform = node_.worldTransform_;
            distance_ = frame.camera_.GetDistance(worldBoundingBox.Center);

            for (int i = 0; i< batches_.Length; ++i)
            {
                batches_[i].distance_ = distance_;
                batches_[i].worldTransform_ = worldTransform;
            }

            float scale = Vector3.Dot(worldBoundingBox.Size, MathUtil.DotScale);
            float newLodDistance = frame.camera_.GetLodDistance(distance_, scale, lodBias_);

            if (newLodDistance != lodDistance_)
                lodDistance_ = newLodDistance;
        }

        public virtual void UpdateGeometry(ref FrameInfo frameInfo)
        {
        }

        public virtual void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
            if (debug && IsEnabledEffective())
                debug.AddBoundingBox(ref WorldBoundingBox, Color.Green, depthTest, false);
        }


    }
}
