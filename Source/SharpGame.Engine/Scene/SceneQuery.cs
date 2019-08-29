using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public interface ISceneQuery
    {
        Intersection TestOctant(ref BoundingBox box, bool inside);
        /// Intersection test for drawables.
        void TestDrawables(Span<Drawable> start, bool inside);
    }

    public struct OctreeQuery : ISceneQuery
    {
        public void TestDrawables(Span<Drawable> start, bool inside)
        {
        }

        public Intersection TestOctant(ref BoundingBox box, bool inside)
        {
            return Intersection.InSide;
        }

    }

    public  struct FrustumOctreeQuery : ISceneQuery
    {
        /// Frustum.
        public Camera camera;
        public RenderView view;
        public uint viewMask;
        public uint drawableFlags;

        public void TestDrawables(Span<Drawable> start, bool inside)
        {
        }

        public Intersection TestOctant(ref BoundingBox box, bool inside)
        {
            if (inside)
                return Intersection.InSide;
            else
                return camera.Frustum.Contains(ref box);
        }
    }

    public interface IDrawableAccumulator
    {
        void InsertDrawable(Drawable drawable);
        void RemoveDrawable(Drawable drawable);

        /// Return drawable objects by a query.
        void GetDrawables(ISceneQuery query, Action<Drawable> drawables);

        /// Return drawable objects by a ray query.
        void Raycast(ref RayQuery query);
        /// Return the closest drawable object by a ray query.
        void RaycastSingle(ref RayQuery query);
    }

    /// Graphics raycast detail level.
    public enum RayQueryLevel
    {
        RAY_AABB = 0,
        RAY_OBB,
        RAY_TRIANGLE,
        RAY_TRIANGLE_UV
    };

    public struct RayQueryResult
    {
        /// Hit position in world space.
        public Vector3 position_;
        /// Hit normal in world space. Negation of ray direction if per-triangle data not available.
        public Vector3 normal_;
        /// Hit texture position
        public Vector2 textureUV_;
        /// Distance from ray origin.
        public float distance_;
        /// Drawable.
        public Drawable drawable_;
        /// Scene node.
        public Node node_;
        /// Drawable specific subobject if applicable.
        public uint subObject_;
        
    }

    public struct RayQuery
    {
        /// Result vector reference.
        public List<RayQueryResult> result_;
        /// Ray.
        public Ray ray_;
        /// Drawable flags to include.
        public byte drawableFlags_;
        /// Drawable layers to include.
        public uint viewMask_;
        /// Maximum ray distance.
        public float maxDistance_;
        /// Raycast detail level.
        public RayQueryLevel level_;
    }

}
