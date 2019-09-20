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
        Static = 0,
        Skinned = 1,
        Instanced = 2,
        Billboard = 3,
        DirBillboard = 4,
        TrailFaceCamera = 5,
        TrailBone = 6,
        Count = 7,
        // This is not a real geometry type for VS, but used to mark objects that do not desire to be instanced
        StaticNoInstancing = 7,
    }

    /// Rendering frame update parameters.
    public struct FrameInfo
    {
        /// Frame number.
        public int frameNumber;
        /// Time elapsed since last frame.
        public float timeStep;
        /// Viewport size.
        public Int2 viewSize;
        /// Camera being used.
        public Camera camera;
    };

    /// Source data for a 3D geometry draw call.
    public class SourceBatch
    {
        /// Distance from camera.
        public float distance;
        /// Geometry.
        public Geometry geometry;
        /// Material.
        public Material material;
        /// World transform(s). For a skinned model, these are the bone transforms.
        public IntPtr worldTransform;
        /// Number of world transforms.
        public int numWorldTransforms;
        /// Per-instance data. If not null, must contain enough data to fill instancing buffer.
        //public IntPtr instancingData;

        public uint offset;

        public int frameNum;
        /// %Geometry type.
        public GeometryType geometryType;
    };

    public class Drawable : Component
    {
        public const byte DRAWABLE_GEOMETRY = 0x1;
        public const byte DRAWABLE_LIGHT = 0x2;
        public const byte DRAWABLE_ZONE = 0x4;
        public const byte DRAWABLE_GEOMETRY2D = 0x8;
        public const byte DRAWABLE_ANY = 0xff;

        public GeometryType GeometryType { get; set; }

        [IgnoreDataMember]
        internal int index = -1;

        internal Octant octant;


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


        /// World-space bounding box.
        [IgnoreDataMember]
        public ref vec3 WorldCenter
        {
            get
            {
                if (worldBoundingBoxDirty_)
                {
                    OnWorldBoundingBoxUpdate();
                    worldBoundingBoxDirty_ = false;
                }

                return ref worldCenter_; ;
            }
        }
        protected vec3 worldCenter_;
        /// Local-space bounding box.
        protected BoundingBox boundingBox_;

        /// Draw call source data.
        [IgnoreDataMember]
        public SourceBatch[] Batches => batches;
        protected SourceBatch[] batches = new SourceBatch[0];

        /// Drawable flags.
        public byte DrawableFlags { get => drawableFlags_; set => drawableFlags_ = value; }
        protected byte drawableFlags_ = DRAWABLE_GEOMETRY;

        /// Bounding box dirty flag.
        protected bool worldBoundingBoxDirty_ = true;
        /// Shadowcaster flag.
        public bool CastShadows { get => castShadows_; set => castShadows_ = value; }
        protected bool castShadows_;

        protected uint viewMask_ = 0xffffffff;
        public uint ViewMask { get => viewMask_; set => viewMask_ = value; }

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

        internal bool updateQueued_ = false;
        internal float sortValue;

        public Drawable()
        {
        }

        public virtual void SetNumGeometries(int num)
        {
            Array.Resize(ref batches, num);

            for (int i = 0; i < num; i++)
            {
                if (batches[i] == null)
                {
                    batches[i] = new SourceBatch
                    {
                        geometryType = GeometryType
                    };
                }
            }
        }

        public void SetGeometry(int index, Geometry geometry)
        {
            batches[index].geometry = geometry;
            batches[index].worldTransform = node_.worldTransform_;
            batches[index].numWorldTransforms = 1;
        }

        public void SetMaterial(Material mat)
        {
            foreach(var batch in batches)
            {
                batch.material = mat;
            }
        }

        public bool SetMaterial(int index, Material mat)
        {
            if (index >= batches.Length)
            {
                Log.Error("Material index out of bounds");
                return false;
            }

            batches[index].material = mat;
            return true;
        }

        public Material GetMaterial(int idx)
        {
            return idx < batches.Length ? batches[idx].material : null;
        }

        public override void OnSetEnabled()
        {
            bool enabled = IsEnabledEffective();

            if(enabled && index == -1)
                AddToScene();
            else if(!enabled && index != -1)
                RemoveFromScene();
        }

        /// Process octree raycast. May be called from a worker thread.
        public virtual void ProcessRayQuery(RayOctreeQuery query, List<RayQueryResult> results)
        {
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

            if (!updateQueued_ && octant != null)
                octant.Root.QueueUpdate(this);
        }

        public void MarkForUpdate()
        {
            if (!updateQueued_ && octant != null)
                octant.Root.QueueUpdate(this);
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
            Scene scene = Scene;
            if (scene)
            {
                if (octant != null)
                {

                    Octree octree = octant.Root;
                    if (updateQueued_)
                        octree.CancelUpdate(this);
                }

                scene.RemoveDrawable(this);
            }
        }

        public virtual Geometry GetLodGeometry(int batchIndex, int level)
        {
            // By default return the visible batch geometry
            if (batchIndex < batches.Length)
                return batches[batchIndex].geometry;
            else
                return null;
        }

        protected virtual void OnWorldBoundingBoxUpdate()
        {
            worldBoundingBox_ = boundingBox_.Transformed(node_.WorldTransform);
            worldCenter_ = worldBoundingBox_.Center;
        }

        public virtual void Update(in FrameInfo frameInfo)
        {
            viewFrameNumber_ = frameInfo.frameNumber;
        }

        public virtual void UpdateBatches(in FrameInfo frame)
        {
            ref BoundingBox worldBoundingBox = ref WorldBoundingBox;
            IntPtr worldTransform = node_.worldTransform_;
            distance_ = frame.camera.GetDistance(worldBoundingBox.Center);

            for (int i = 0; i< batches.Length; ++i)
            {
                batches[i].distance = distance_;
                batches[i].worldTransform = worldTransform;
            }

            float scale = vec3.Dot(worldBoundingBox.Size, (vec3)MathUtil.DotScale);
            float newLodDistance = frame.camera.GetLodDistance(distance_, scale, lodBias_);

            if (newLodDistance != lodDistance_)
                lodDistance_ = newLodDistance;
        }

        public virtual void UpdateGeometry(in FrameInfo frameInfo)
        {
        }

        public virtual void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
            if (debug && IsEnabledEffective())
                debug.AddBoundingBox(ref WorldBoundingBox, Color.Green, depthTest, false);
        }


    }
}
