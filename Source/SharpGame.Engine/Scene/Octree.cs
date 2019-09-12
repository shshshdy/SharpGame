using System;
using System.Collections.Generic;
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
        FastList<Drawable> drawables_;
        /// Child octants.
        Octant[] children_;//[NUM_OCTANTS];
        /// World bounding box center.
        vec3 center_;
        /// World bounding box half size.
        vec3 halfSize_;
        /// Subdivision level.
        uint level_;
        /// Number of drawable objects in this octant and child octants.
        uint numDrawables_;
        /// Parent octant.
        Octant parent_;
        /// Octree root.
        Octree root_;
        /// Octant index relative to its siblings or ROOT_INDEX for root octant
        int index_;

        public Octant(in BoundingBox box, uint level, Octant parent, Octree root, int index)
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
        {/*
            // Remove the drawables (if any) from this octant to the root octant
            for (PODVector<Drawable*>::Iterator i = drawables_.Begin(); i != drawables_.End(); ++i)
            {
                (*i)->SetOctant(root_);
                root_->drawables_.Push(*i);
                root_->QueueUpdate(*i);
            }*/
            drawables_.Clear();
            numDrawables_ = 0;
        }

        for (int i = 0; i < NUM_OCTANTS; ++i)
            DeleteChild(i);
    }

        void Initialize(in BoundingBox box)
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

    void DeleteChild(int index)
    {
        //assert(index < NUM_OCTANTS);
        //delete children_[index];
        children_[index] = null;
    }

    void InsertDrawable(Drawable drawable)
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

    bool CheckDrawableFit(in  BoundingBox box)
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

void ResetRoot()
{
    root_ = null;
/*
    // The whole octree is being destroyed, just detach the drawables
    for (PODVector<Drawable*>::Iterator i = drawables_.Begin(); i != drawables_.End(); ++i)
        (*i)->SetOctant(null);

    for (auto & child : children_)
    {
        if (child)
            child->ResetRoot();
    }*/
}

        /// Add a drawable object to this octant.
        void AddDrawable(Drawable drawable)
        {
            drawable.Octant = this;
            drawables_.Add(drawable);
            IncDrawableCount();
        }

        /// Remove a drawable object from this octant.
        void RemoveDrawable(Drawable drawable, bool resetOctant = true)
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

            if (parent  != null)
                parent.DecDrawableCount();
        }

        /*
void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
{
    if (debug && debug->IsInside(worldBoundingBox_))
    {
        debug->AddBoundingBox(worldBoundingBox_, Color(0.25f, 0.25f, 0.25f), depthTest);

        for (auto & child : children_)
        {
            if (child)
                child->DrawDebugGeometry(debug, depthTest);
        }
    }
}

void GetDrawablesInternal(OctreeQuery query, bool inside)
{
    if (this != root_)
    {
    Intersection res = query.TestOctant(cullingBox_, inside);
    if (res == INSIDE)
        inside = true;
    else if (res == OUTSIDE)
    {
        // Fully outside, so cull this octant, its children & drawables
        return;
    }
}

    if (drawables_.Size())
    {
    auto** start = const_cast<Drawable**>(&drawables_[0]);
    Drawable** end = start + drawables_.Size();
    query.TestDrawables(start, end, inside);
}

    for (auto child : children_)
    {
    if (child)
        child->GetDrawablesInternal(query, inside);
}
}

void GetDrawablesInternal(RayOctreeQuery query)
{
    float octantDist = query.ray_.HitDistance(cullingBox_);
    if (octantDist >= query.maxDistance_)
        return;

    if (drawables_.Size())
    {
        auto** start = const_cast<Drawable**>(&drawables_[0]);
Drawable** end = start + drawables_.Size();

        while (start != end)
        {
            Drawable* drawable = *start++;

            if ((drawable->GetDrawableFlags() & query.drawableFlags_) && (drawable->GetViewMask() & query.viewMask_))
                drawable->ProcessRayQuery(query, query.result_);
        }
    }

    for (auto child : children_)
    {
        if (child)
            child->GetDrawablesInternal(query);
    }
}

void GetDrawablesOnlyInternal(RayOctreeQuery& query, PODVector<Drawable*>& drawables) const
{
    float octantDist = query.ray_.HitDistance(cullingBox_);
    if (octantDist >= query.maxDistance_)
        return;

    if (drawables_.Size())
    {
        auto** start = const_cast<Drawable**>(&drawables_[0]);
Drawable** end = start + drawables_.Size();

        while (start != end)
        {
            Drawable* drawable = *start++;

            if ((drawable->GetDrawableFlags() & query.drawableFlags_) && (drawable->GetViewMask() & query.viewMask_))
                drawables.Push(drawable);
        }
    }

    foreach (var child in children_)
    {
        if (child)
            child->GetDrawablesOnlyInternal(query, drawables);
    }
}*/

    }

    public class Octree
    {
        public Octant root;

        public int NumLevels => numLevels_;
        int numLevels_;
    }
}
