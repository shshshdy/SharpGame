using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{

    public class Octant
    {
        public const int NUM_OCTANTS = 8;

        /// World bounding box.
        BoundingBox worldBoundingBox_;
        /// Bounding box used for drawable object fitting.
        BoundingBox cullingBox_;
        /// Drawable objects.
        internal FastList<Drawable> drawables_ = new FastList<Drawable>();
        /// Child octants.
        FixedArray8<Octant> children_;
        /// World bounding box center.
        vec3 center_;
        /// World bounding box half size.
        vec3 halfSize_;
        /// Subdivision level.
        internal int level_;
        /// Number of drawable objects in this octant and child octants.
        internal int numDrawables_;
        /// Parent octant.
        internal Octant parent_;
        /// Octree root.
        Octree root_;
        /// Octant index relative to its siblings or ROOT_INDEX for root octant
        internal int index_;

        public Octant(in BoundingBox box, int level, Octant parent, Octree root, int index)
        {
            level_ = level;
            parent_ = parent;
            root_ = root;
            index_ = index;

            Initialize(box);
        }

        public void Free()
        {
            if (root_ != null)
            {
                // Remove the drawables (if any) from this octant to the root octant
                foreach (Drawable drawable in drawables_)
                {
                    drawable.octant = root_.Root;
                    root_.Root.drawables_.Add(drawable);
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
            cullingBox_ = new BoundingBox(worldBoundingBox_.min - halfSize_, worldBoundingBox_.max + halfSize_);
        }

        public Octant GetOrCreateChild(int index)
        {
            if (children_[index] != null)
                return children_[index];

            vec3 newMin = worldBoundingBox_.min;
            vec3 newMax = worldBoundingBox_.max;
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

            children_[index] = root_.GetOctant(new BoundingBox(newMin, newMax), level_ + 1, this, root_, index);
            return children_[index];
        }

        public void DeleteChild(int index)
        {
            //assert(index < NUM_OCTANTS);     
            root_.Free(children_[index]);
            children_[index] = null;
        }

        public void InsertDrawable(Drawable drawable)
        {
            ref BoundingBox box = ref drawable.WorldBoundingBox;

            // If root octant, insert all non-occludees here, so that octant occlusion does not hide the drawable.
            // Also if drawable is outside the root octant bounds, insert to root
            bool insertHere;
            if (this == root_.Root)
                insertHere = /*!drawable->IsOccludee() ||*/ cullingBox_.Contains(box) != Intersection.InSide || CheckDrawableFit(box);
            else
                insertHere = CheckDrawableFit(box);

            if (insertHere)
            {
                Octant oldOctant = drawable.octant;
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

        public bool CheckDrawableFit(in BoundingBox box)
        {
            vec3 boxSize = box.Size;

            // If max split level, size always OK, otherwise check that box is at least half size of octant
            if (level_ >= root_.NumLevels || boxSize.x >= halfSize_.x || boxSize.y >= halfSize_.y ||
                boxSize.z >= halfSize_.z)
                return true;
            // Also check if the box can not fit a child octant's culling box, in that case size OK (must insert here)
            else
            {
                if (box.min.x <= worldBoundingBox_.min.x - 0.5f * halfSize_.x ||
                    box.max.x >= worldBoundingBox_.max.x + 0.5f * halfSize_.x ||
                    box.min.y <= worldBoundingBox_.min.y - 0.5f * halfSize_.y ||
                    box.max.y >= worldBoundingBox_.max.y + 0.5f * halfSize_.y ||
                    box.min.z <= worldBoundingBox_.min.z - 0.5f * halfSize_.z ||
                    box.max.z >= worldBoundingBox_.max.z + 0.5f * halfSize_.z)
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
                drawable.octant = null;

            foreach (var child in children_)
            {
                child?.ResetRoot();
            }
        }

        /// Add a drawable object to this octant.
        public void AddDrawable(Drawable drawable)
        {
            drawable.octant = this;
            drawables_.Add(drawable);
            IncDrawableCount();
        }

        /// Remove a drawable object from this octant.
        public void RemoveDrawable(Drawable drawable, bool resetOctant = true)
        {
            if (drawables_.Remove(drawable))
            {
                if (resetOctant)
                    drawable.octant = null;
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

        internal void GetDrawablesInternal(OctreeQuery query, bool inside, Action<Drawable> visitor)
        {
            if (this != root_.Root)
            {
                Intersection res = query.TestOctant(cullingBox_, inside);
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
                query.TestDrawables(drawables_.AsSpan(), inside, visitor);
            }

            foreach (var child in children_)
            {
                child?.GetDrawablesInternal(query, inside, visitor);
            }
        }

        internal void GetDrawablesInternal(RayOctreeQuery query)
        {
            if (!query.ray_.Intersects(cullingBox_, out float octantDist))
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

        internal void GetDrawablesOnlyInternal(RayOctreeQuery query, FastList<Drawable> drawables)
        {
            if (!query.ray_.Intersects(cullingBox_, out float octantDist))
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

}
