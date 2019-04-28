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

    public class QuadtreeQuery : ISceneQuery
    {
        public void TestDrawables(Span<Drawable> start, bool inside)
        {
            throw new NotImplementedException();
        }

        public Intersection TestOctant(ref BoundingBox box, bool inside)
        {
            throw new NotImplementedException();
        }
    }

    public interface ISceneAccumulator
    {
        void InsertDrawable(Drawable drawable);
        void RemoveDrawable(Drawable drawable);

        IEnumerator<Drawable> GetEnumerator();

        /// Return drawable objects by a query.
        void GetDrawables(ISceneQuery query, IList<Drawable> drawables);

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
        Vector3 position_;
        /// Hit normal in world space. Negation of ray direction if per-triangle data not available.
        Vector3 normal_;
        /// Hit texture position
        Vector2 textureUV_;
        /// Distance from ray origin.
        float distance_;
        /// Drawable.
        Drawable drawable_;
        /// Scene node.
        Node node_;
        /// Drawable specific subobject if applicable.
        uint subObject_;
        
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
