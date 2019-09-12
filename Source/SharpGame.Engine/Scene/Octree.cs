using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class Octree : Component
    {
        const float DEFAULT_OCTREE_SIZE = 1000.0f;
        const int DEFAULT_OCTREE_LEVELS = 8;
        const int NUM_OCTANTS = 8;

        public Octant Root { get; }

        public int NumLevels => numLevels_;
        int numLevels_;

        /// Drawable objects that require update.
        FastList<Drawable> drawableUpdates_;
        /// Drawable objects that were inserted during threaded update phase.
        FastList<Drawable> threadedDrawableUpdates_;
        /// Mutex for octree reinsertions.
        object octreeMutex_;
        /// Ray query temporary list of drawables.
        FastList<Drawable> rayQueryDrawables_;

        List<Octant> freeList = new List<Octant>();

        public Octree()
        {
            var bbox = new BoundingBox(-DEFAULT_OCTREE_SIZE, DEFAULT_OCTREE_SIZE);
            Root = new Octant(in bbox, 0, null, this, -1);
            numLevels_ = DEFAULT_OCTREE_LEVELS;
            // If the engine is running headless, subscribe to RenderUpdate events for manually updating the octree
            // to allow raycasts and animation update
            //if (!GetSubsystem<Graphics>())
            //    SubscribeToEvent(E_RENDERUPDATE, URHO3D_HANDLER(Octree, HandleRenderUpdate));
        }

        protected override void Destroy()
        {
            // Reset root pointer from all child octants now so that they do not move their drawables to root
            drawableUpdates_.Clear();
            Root.ResetRoot();

            base.Destroy();

        }

        public Octant GetOctant(in BoundingBox box, int level, Octant parent, Octree root, int index)
        {
            if(!freeList.Empty())
            {
                var octant = freeList.Pop();

                octant.level_ = level;
                octant.parent_ = parent;
                octant.index_ = index;

                octant.Initialize(in box);
            }

            return new Octant(in box, level, parent, root, index);
        }

        public void Free(Octant octant)
        {
            octant.Free();

            freeList.Add(octant);
        }

        /// Update octree size.
        void UpdateOctreeSize() { SetSize(in Root.WorldBoundingBox, numLevels_); }

        void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
            if (debug)
            {
                Root.DrawDebugGeometry(debug, depthTest);
            }
        }

        public void SetSize(in BoundingBox box, int numLevels)
        {
            // If drawables exist, they are temporarily moved to the root
            for (int i = 0; i < NUM_OCTANTS; ++i)
            {
                Root.DeleteChild(i);
            }

            Root.Initialize(box);
            Root.numDrawables_ = Root.drawables_.Count;
            numLevels_ = Math.Max(numLevels, 1);
        }

        public void Update(ref FrameInfo frame)
        {
        }

        public void AddManualDrawable(Drawable drawable)
        {
            if (!drawable || drawable.octant != null)
                return;

            Root.AddDrawable(drawable);
        }

        public void RemoveManualDrawable(Drawable drawable)
        {
            if (!drawable)
                return;

            Octant octant = drawable.octant;
            if (octant != null && octant.Root == this)
                octant.RemoveDrawable(drawable);
        }

        void GetDrawables(OctreeQuery query)
        {
            query.result.Clear();
            Root.GetDrawablesInternal(query, false);
        }

        void Raycast(RayOctreeQuery query)
        {
            query.result_.Clear();
            Root.GetDrawablesInternal(query);
            query.result_.Sort((lhs,rhs) => lhs.distance_ < rhs.distance_ ? -1 : (lhs.distance_ < rhs.distance_ ? 0 : 1));
        }

        void RaycastSingle(RayOctreeQuery query)
        {
            /*
query.result_.Clear();
    rayQueryDrawables_.Clear();
    GetDrawablesOnlyInternal(query, rayQueryDrawables_);

    // Sort by increasing hit distance to AABB
    for (PODVector<Drawable*>::Iterator i = rayQueryDrawables_.Begin(); i != rayQueryDrawables_.End(); ++i)
    {
        Drawable* drawable = *i;
drawable->SetSortValue(query.ray_.HitDistance(drawable->GetWorldBoundingBox()));
    }

    Sort(rayQueryDrawables_.Begin(), rayQueryDrawables_.End(), CompareDrawables);

// Then do the actual test according to the query, and early-out as possible
float closestHit = M_INFINITY;
    for (PODVector<Drawable*>::Iterator i = rayQueryDrawables_.Begin(); i != rayQueryDrawables_.End(); ++i)
    {
        Drawable* drawable = *i;
        if (drawable->GetSortValue() < Min(closestHit, query.maxDistance_))
        {
            unsigned oldSize = query.result_.Size();
drawable->ProcessRayQuery(query, query.result_);
            if (query.result_.Size() > oldSize)
                closestHit = Min(closestHit, query.result_.Back().distance_);
        }
        else
            break;
    }

    if (query.result_.Size() > 1)
    {
        Sort(query.result_.Begin(), query.result_.End(), CompareRayQueryResults);
query.result_.Resize(1);
    }*/
        }

        public void QueueUpdate(Drawable drawable)
        {
            Scene scene = Scene;
            if (scene && scene.IsThreadedUpdate)
            {
                lock (octreeMutex_)
                {
                    threadedDrawableUpdates_.Add(drawable);
                }
            }
            else
                drawableUpdates_.Add(drawable);

            drawable.updateQueued_ = true;
        }

        public void CancelUpdate(Drawable drawable)
        {
            // This doesn't have to take into account scene being in threaded update, because it is called only
            // when removing a drawable from octree, which should only ever happen from the main thread.
            drawableUpdates_.Remove(drawable);
            drawable.updateQueued_ = false;
        }

        void DrawDebugGeometry(bool depthTest)
        {
            var debug = GetComponent<DebugRenderer>();
            Root.DrawDebugGeometry(debug, depthTest);
        }

    }
}
