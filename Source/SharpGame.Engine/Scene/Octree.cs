using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class Octree : Component, ISpacePartitioner
    {
        const float DEFAULT_OCTREE_SIZE = 1000.0f;
        const int DEFAULT_OCTREE_LEVELS = 8;
        const int NUM_OCTANTS = 8;

        [IgnoreDataMember]
        public Octant Root { get; }

        [IgnoreDataMember]
        public int NumLevels => numLevels_;
        int numLevels_;

        /// Drawable objects that require update.
        FastList<Drawable> drawableUpdates_ = new FastList<Drawable>();
        /// Drawable objects that were inserted during threaded update phase.
        FastList<Drawable> threadedDrawableUpdates_ = new FastList<Drawable>();
        /// Mutex for octree reinsertions.
        object octreeMutex_ = new object();
        /// Ray query temporary list of drawables.
        FastList<Drawable> rayQueryDrawables_ = new FastList<Drawable>();

        List<Octant> freeList = new List<Octant>();

        public Octree()
        {
            var bbox = new BoundingBox(-DEFAULT_OCTREE_SIZE, DEFAULT_OCTREE_SIZE);
            Root = new Octant(in bbox, 0, null, this, -1);
            numLevels_ = DEFAULT_OCTREE_LEVELS;
        }

        protected override void Destroy()
        {
            // Reset root pointer from all child octants now so that they do not move their drawables to root
            drawableUpdates_.Clear();
            Root.ResetRoot();

            base.Destroy();
        }

        internal Octant GetOctant(in BoundingBox box, int level, Octant parent, Octree root, int index)
        {
            if (!freeList.Empty())
            {
                var octant = freeList.Pop();

                octant.level_ = level;
                octant.parent_ = parent;
                octant.index_ = index;

                octant.Initialize(in box);
            }

            return new Octant(in box, level, parent, root, index);
        }

        internal void Free(Octant octant)
        {
            if(octant != null)
            {
                octant.Free();

                freeList.Add(octant);
            }
        }

        /// Update octree size.
        void UpdateOctreeSize() { SetSize(in Root.WorldBoundingBox, numLevels_); }

        public void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
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

        public void Update(FrameInfo frame)
        {
            Scene scene = Scene;
            // Let drawables update themselves before reinsertion. This can be used for animation
            if (!drawableUpdates_.Empty())
            {

                // Perform updates in worker threads. Notify the scene that a threaded update is going on and components
                // (for example physics objects) should not perform non-threadsafe work when marked dirty

                scene.BeginThreadedUpdate();
                Parallel.ForEach(drawableUpdates_, drawable => drawable.Update(in frame));
                scene.EndThreadedUpdate();
            }

            // If any drawables were inserted during threaded update, update them now from the main thread
            if (!threadedDrawableUpdates_.Empty())
            {

                foreach (Drawable drawable in threadedDrawableUpdates_)
                {
                    if (drawable)
                    {
                        drawable.Update(in frame);
                        drawableUpdates_.Add(drawable);
                    }
                }

                threadedDrawableUpdates_.Clear();
            }

            // Notify drawable update being finished. Custom animation (eg. IK) can be done at this point

            scene.SendEvent(new SceneDrawableUpdateFinished { scene = scene, timeStep = frame.timeStep });

            // Reinsert drawables that have been moved or resized, or that have been newly added to the octree and do not sit inside
            // the proper octant yet
            if (!drawableUpdates_.Empty())
            {
                foreach (Drawable drawable in drawableUpdates_)
                {
                    drawable.updateQueued_ = false;
                    Octant octant = drawable.octant;
                    ref BoundingBox box = ref drawable.WorldBoundingBox;

                    // Skip if no octant or does not belong to this octree anymore
                    if (octant == null || octant.Root != this)
                        continue;

                    // Skip if still fits the current octant
                    if (/*drawable->IsOccludee() &&*/ octant.CullingBox.Contains(ref box) == Intersection.InSide && octant.CheckDrawableFit(in box))
                        continue;

                    InsertDrawable(drawable);

                }
            }

            drawableUpdates_.Clear();
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

        public void GetDrawables(OctreeQuery query, Action<Drawable> visitor)
        {
            Root.GetDrawablesInternal(query, false, visitor);
        }

        public void InsertDrawable(Drawable drawable)
        {
            Root.InsertDrawable(drawable);
        }

        public void RemoveDrawable(Drawable drawable)
        {
            Root.RemoveDrawable(drawable);
        }

        static int CompareDrawables(Drawable lhs, Drawable rhs)
        {
            return lhs.sortValue < rhs.sortValue ? -1 : lhs.sortValue > rhs.sortValue ? 1 : 0;
        }

        static int CompareRayQueryResults(RayQueryResult lhs, RayQueryResult rhs)
        {
            return lhs.distance_ < rhs.distance_ ? -1 : (lhs.distance_ > rhs.distance_ ? 1 : 0);
        }

        public void Raycast(ref RayOctreeQuery query)
        {
            query.result_.Clear();
            Root.GetDrawablesInternal(query);
            query.result_.Sort(CompareRayQueryResults);
        }

        public void RaycastSingle(ref RayOctreeQuery query)
        {
            query.result_.Clear();
            rayQueryDrawables_.Clear();
            Root.GetDrawablesOnlyInternal(query, rayQueryDrawables_);

            // Sort by increasing hit distance to AABB
            foreach (Drawable drawable in rayQueryDrawables_)
            {
                drawable.sortValue = (query.ray_.HitDistance(in drawable.WorldBoundingBox));
            }

            rayQueryDrawables_.Sort(CompareDrawables);

            // Then do the actual test according to the query, and early-out as possible
            float closestHit = float.PositiveInfinity;
            foreach (Drawable drawable in rayQueryDrawables_)
            {
                if (drawable.sortValue < Math.Min(closestHit, query.maxDistance_))
                {
                    int oldSize = query.result_.Count;
                    drawable.ProcessRayQuery(query, query.result_);
                    if (query.result_.Count > oldSize)
                        closestHit = Math.Min(closestHit, query.result_.Back().distance_);
                }
                else
                    break;
            }

            if (query.result_.Count > 1)
            {
                query.result_.Sort(CompareRayQueryResults);
                query.result_.Resize(1);
            }
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

        public void DrawDebugGeometry(bool depthTest)
        {
            var debug = GetComponent<DebugRenderer>();
            Root.DrawDebugGeometry(debug, depthTest);
        }


    }
}
