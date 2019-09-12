using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public struct OctantChildran : IEnumerable<Octant>
    {
        public Octant c1;
        public Octant c2;
        public Octant c3;
        public Octant c4;
        public Octant c5;
        public Octant c6;
        public Octant c7;
        public Octant c8;

        public Octant this[int index]
        {
            get
            {
                return Unsafe.Add(ref c1, index);
            }
            set
            {
                Unsafe.Add(ref c1, index) = value;
            }
        }

        public IEnumerator<Octant> GetEnumerator()
        {
            yield return c1;
            yield return c2;
            yield return c3;
            yield return c4;
            yield return c5;
            yield return c6;
            yield return c7;
            yield return c8;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return c1;
            yield return c2;
            yield return c3;
            yield return c4;
            yield return c5;
            yield return c6;
            yield return c7;
            yield return c8;
        }
    }

    public class Octant
    {
        public const int NUM_OCTANTS = 8;

        /// World bounding box.
        BoundingBox worldBoundingBox_;
        /// Bounding box used for drawable object fitting.
        BoundingBox cullingBox_;
        /// Drawable objects.
        internal FastList<Drawable> drawables_;
        /// Child octants.
        //Octant[] children_;//[NUM_OCTANTS];
        OctantChildran children_;
        /// World bounding box center.
        vec3 center_;
        /// World bounding box half size.
        vec3 halfSize_;
        /// Subdivision level.
        int level_;
        /// Number of drawable objects in this octant and child octants.
        internal int numDrawables_;
        /// Parent octant.
        Octant parent_;
        /// Octree root.
        Octree root_;
        /// Octant index relative to its siblings or ROOT_INDEX for root octant
        int index_;

        public Octant(in BoundingBox box, int level, Octant parent, Octree root, int index)
        {
            level_ = level;
            parent_ = parent;
            root_ = root;
            index_ = index;
            Initialize(box);
        }

        ~Octant()
        {
            if (root_ != null)
            {
                // Remove the drawables (if any) from this octant to the root octant
                foreach (Drawable drawable in drawables_)
                {
                    drawable.Octant = root_.root;
                    root_.root.drawables_.Add(drawable);
                    root_.QueueUpdate(drawable);
                }

                drawables_.Clear();
                numDrawables_ = 0;
            }

            for (int i = 0; i < NUM_OCTANTS; ++i)
                DeleteChild(i);
        }

        /// Return world-space bounding box.
        public ref BoundingBox WorldBoundingBox => ref worldBoundingBox_;

        /// Return bounding box used for fitting drawable objects.
        public ref BoundingBox CullingBox => ref cullingBox_;

        /// Return subdivision level.
        public int Level => level_;

        /// Return parent octant.
        public Octant Parent => parent_;

        /// Return octree root.
        public Octree Root => root_;

        /// Return number of drawables.
        public int NumDrawables => numDrawables_;

        /// Return true if there are no drawable objects in this octant and child octants.
        public bool IsEmpty => numDrawables_ == 0;

        public void Initialize(in BoundingBox box)
        {
            worldBoundingBox_ = box;
            center_ = box.Center;
            halfSize_ = 0.5f * box.Size;
            cullingBox_ = new BoundingBox(worldBoundingBox_.Minimum - halfSize_, worldBoundingBox_.Maximum + halfSize_);
        }

        public Octant GetOrCreateChild(int index)
        {
            if (children_[index] != null)
                return children_[index];

            vec3 newMin = worldBoundingBox_.Minimum;
            vec3 newMax = worldBoundingBox_.Maximum;
            vec3 oldCenter = worldBoundingBox_.Center;

            if ((index & 1u) != 0)
                newMin.x = oldCenter.x;
            else
                newMax.x = oldCenter.x;

            if ((index & 2u) != 0)
                newMin.y = oldCenter.y;
            else
                newMax.y = oldCenter.y;

            if ((index & 4u) != 0)
                newMin.z = oldCenter.z;
            else
                newMax.z = oldCenter.z;

            children_[index] = new Octant(new BoundingBox(newMin, newMax), level_ + 1, this, root_, index);
            return children_[index];
        }

        public void DeleteChild(int index)
        {
            //assert(index < NUM_OCTANTS);
            //delete children_[index];
            //to do:pool
            children_[index] = null;
        }

        public void InsertDrawable(Drawable drawable)
        {
            ref BoundingBox box = ref drawable.WorldBoundingBox;

            // If root octant, insert all non-occludees here, so that octant occlusion does not hide the drawable.
            // Also if drawable is outside the root octant bounds, insert to root
            bool insertHere;
            if (this == root_.root)
                insertHere = /*!drawable->IsOccludee() ||*/ cullingBox_.IsInside(ref box) != Intersection.InSide || CheckDrawableFit(box);
            else
                insertHere = CheckDrawableFit(box);

            if (insertHere)
            {
                Octant oldOctant = drawable.Octant;
                if (oldOctant != this)
                {
                    // Add first, then remove, because drawable count going to zero deletes the octree branch in question
                    AddDrawable(drawable);
                    if (oldOctant != null)
                        oldOctant.RemoveDrawable(drawable, false);
                }
            }
            else
            {
                vec3 boxCenter = box.Center;
                int x = (boxCenter.x < center_.x ? 0 : 1);
                int y = (boxCenter.y < center_.y ? 0 : 2);
                int z = (boxCenter.z < center_.z ? 0 : 4);

                GetOrCreateChild(x + y + z).InsertDrawable(drawable);
            }
        }

        bool CheckDrawableFit(in BoundingBox box)
        {
            vec3 boxSize = box.Size;

            // If max split level, size always OK, otherwise check that box is at least half size of octant
            if (level_ >= root_.NumLevels || boxSize.x >= halfSize_.x || boxSize.y >= halfSize_.y ||
                boxSize.z >= halfSize_.z)
                return true;
            // Also check if the box can not fit a child octant's culling box, in that case size OK (must insert here)
            else
            {
                if (box.Minimum.x <= worldBoundingBox_.Minimum.x - 0.5f * halfSize_.x ||
                    box.Maximum.x >= worldBoundingBox_.Maximum.x + 0.5f * halfSize_.x ||
                    box.Minimum.y <= worldBoundingBox_.Minimum.y - 0.5f * halfSize_.y ||
                    box.Maximum.y >= worldBoundingBox_.Maximum.y + 0.5f * halfSize_.y ||
                    box.Minimum.z <= worldBoundingBox_.Minimum.z - 0.5f * halfSize_.z ||
                    box.Maximum.z >= worldBoundingBox_.Maximum.z + 0.5f * halfSize_.z)
                    return true;
            }

            // Bounding box too small, should create a child octant
            return false;
        }

        public void ResetRoot()
        {
            root_ = null;

            // The whole octree is being destroyed, just detach the drawables
            foreach (var drawable in drawables_)
                drawable.Octant = null;

            foreach (var child in children_)
            {
                child?.ResetRoot();
            }
        }

        /// Add a drawable object to this octant.
        public void AddDrawable(Drawable drawable)
        {
            drawable.Octant = this;
            drawables_.Add(drawable);
            IncDrawableCount();
        }

        /// Remove a drawable object from this octant.
        public void RemoveDrawable(Drawable drawable, bool resetOctant = true)
        {
            if (drawables_.Remove(drawable))
            {
                if (resetOctant)
                    drawable.Octant = null;
                DecDrawableCount();
            }
        }

        /// Increase drawable object count recursively.
        void IncDrawableCount()
        {
            ++numDrawables_;
            if (parent_ != null)
                parent_.IncDrawableCount();
        }

        /// Decrease drawable object count recursively and remove octant if it becomes empty.
        void DecDrawableCount()
        {
            Octant parent = parent_;

            --numDrawables_;
            if (0 == numDrawables_)
            {
                if (parent != null)
                    parent.DeleteChild(index_);
            }

            if (parent != null)
                parent.DecDrawableCount();
        }

        public void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
            if (debug && debug.IsInside(ref worldBoundingBox_))
            {
                debug.AddBoundingBox(ref worldBoundingBox_, new Color(0.25f, 0.25f, 0.25f), depthTest);

                foreach (var child in children_)
                {
                    child?.DrawDebugGeometry(debug, depthTest);
                }
            }
        }

        internal void GetDrawablesInternal(OctreeQuery query, bool inside)
        {
            if (this != root_.root)
            {
                Intersection res = query.TestOctant(ref cullingBox_, inside);
                if (res == Intersection.InSide)
                    inside = true;
                else if (res == Intersection.OutSide)
                {
                    // Fully outside, so cull this octant, its children & drawables
                    return;
                }
            }

            if (drawables_.Count > 0)
            {
                query.TestDrawables(drawables_.AsSpan(), inside);
            }

            foreach (var child in children_)
            {
                child?.GetDrawablesInternal(query, inside);
            }
        }

        internal void GetDrawablesInternal(RayOctreeQuery query)
        {
            if (!query.ray_.Intersects(ref cullingBox_, out float octantDist))
            {
                return;
            }

            if (drawables_.Count > 0)
            {
                foreach (Drawable drawable in drawables_)
                {
                    if ((drawable.DrawableFlags & query.drawableFlags_) != 0 && (drawable.ViewMask & query.viewMask_) != 0)
                        drawable.ProcessRayQuery(query, query.result_);
                }
            }

            foreach (var child in children_)
            {
                child?.GetDrawablesInternal(query);
            }
        }

        void GetDrawablesOnlyInternal(RayOctreeQuery query, FastList<Drawable> drawables)
        {
            if (!query.ray_.Intersects(ref cullingBox_, out float octantDist))
            {
                return;
            }

            if (drawables_.Count > 0)
            {
                foreach (Drawable drawable in drawables_)
                {
                    if ((drawable.DrawableFlags & query.drawableFlags_) != 0 && (drawable.ViewMask & query.viewMask_) != 0)
                        drawables.Add(drawable);
                }
            }


            foreach (var child in children_)
            {
                child?.GetDrawablesOnlyInternal(query, drawables);
            }
        }

    }

    public class Octree : Component
    {
        const float DEFAULT_OCTREE_SIZE = 1000.0f;
        const int DEFAULT_OCTREE_LEVELS = 8;
        const int NUM_OCTANTS = 8;

        public Octant root;

        public int NumLevels => numLevels_;
        int numLevels_;
        /// Update octree size.
        //void UpdateOctreeSize() { SetSize(worldBoundingBox_, numLevels_); }

        /// Drawable objects that require update.
        FastList<Drawable> drawableUpdates_;
        /// Drawable objects that were inserted during threaded update phase.
        FastList<Drawable> threadedDrawableUpdates_;
        /// Mutex for octree reinsertions.
        object octreeMutex_;
        /// Ray query temporary list of drawables.
        FastList<Drawable> rayQueryDrawables_;

        public Octree()
        {
            var bbox = new BoundingBox(-DEFAULT_OCTREE_SIZE, DEFAULT_OCTREE_SIZE);
            root = new Octant(in bbox, 0, null, this, -1);
            numLevels_ = DEFAULT_OCTREE_LEVELS;
            // If the engine is running headless, subscribe to RenderUpdate events for manually updating the octree
            // to allow raycasts and animation update
            //if (!GetSubsystem<Graphics>())
            //    SubscribeToEvent(E_RENDERUPDATE, URHO3D_HANDLER(Octree, HandleRenderUpdate));
        }

        ~Octree()
        {
            // Reset root pointer from all child octants now so that they do not move their drawables to root
            drawableUpdates_.Clear();
            root.ResetRoot();
        }

        void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
            if (debug)
            {
                root.DrawDebugGeometry(debug, depthTest);
            }
        }

        public void SetSize(in BoundingBox box, int numLevels)
        {

            // If drawables exist, they are temporarily moved to the root
            for (int i = 0; i < NUM_OCTANTS; ++i)
            {
                root.DeleteChild(i);
            }

            root.Initialize(box);
            root.numDrawables_ = root.drawables_.Count;
            numLevels_ = Math.Max(numLevels, 1);
        }

        public void Update(ref FrameInfo frame)
        {
        }

        public void AddManualDrawable(Drawable drawable)
        {
            if (!drawable || drawable.Octant != null)
                return;

            root.AddDrawable(drawable);
        }

        public void RemoveManualDrawable(Drawable drawable)
        {
            if (!drawable)
                return;

            Octant octant = drawable.Octant;
            if (octant != null && octant.Root == this)
                octant.RemoveDrawable(drawable);
        }

        void GetDrawables(OctreeQuery query)
        {
            query.result.Clear();
            root.GetDrawablesInternal(query, false);
        }

        void Raycast(RayOctreeQuery query)
        {
            query.result_.Clear();
            root.GetDrawablesInternal(query);
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
            root.DrawDebugGeometry(debug, depthTest);
        }

    }
}
