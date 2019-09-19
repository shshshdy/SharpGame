using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public abstract class OctreeQuery
    {
        public uint viewMask;
        public uint drawableFlags;

        public abstract void TestDrawables(Span<Drawable> start, bool inside, Action<Drawable> visitor);
        public abstract Intersection TestOctant(ref BoundingBox box, bool inside);     

    }

    public class FrustumOctreeQuery : OctreeQuery
    {
        /// Frustum.
        public Camera camera;

        public FrustumOctreeQuery()
        {
        }

        public FrustumOctreeQuery(Camera camera, uint drawableFlags, uint viewMask)
        {
            Init(camera, drawableFlags, viewMask);
        }

        public void Init(Camera camera, uint drawableFlags, uint viewMask)
        {
            this.camera = camera;
            this.viewMask = viewMask;
            this.drawableFlags = viewMask;
        }

        public override void TestDrawables(Span<Drawable> start, bool inside, Action<Drawable> visitor)
        {
            foreach(Drawable drawable in start)
            {
                if ((drawable.DrawableFlags & drawableFlags) != 0 && (drawable.ViewMask & viewMask) != 0)
                {
                    if (inside || camera.Frustum.Intersects(ref drawable.WorldBoundingBox))
                        visitor(drawable);
                }
            }
        }

        public override Intersection TestOctant(ref BoundingBox box, bool inside)
        {
            if (inside)
                return Intersection.InSide;
            else
                return camera.Frustum.Contains(ref box);
        }
    }

    /// %Frustum octree query for shadowcasters.
    public class ShadowCasterOctreeQuery : FrustumOctreeQuery
    {
        public override void TestDrawables(Span<Drawable> start, bool inside, Action<Drawable> visitor)
        {
            foreach (Drawable drawable in start)
            {
                if ((drawable.DrawableFlags & drawableFlags) != 0 && (drawable.ViewMask & viewMask) != 0)
                {
                    if (inside || camera.Frustum.Intersects(ref drawable.WorldBoundingBox))
                        visitor(drawable);
                }
            }
        }
    }

    public interface ISpacePartitioner
    {
        void InsertDrawable(Drawable drawable);
        void RemoveDrawable(Drawable drawable);

        void Update(FrameInfo frame);

        /// Return drawable objects by a query.
        void GetDrawables(OctreeQuery query, Action<Drawable> visitor);

        /// Return drawable objects by a ray query.
        void Raycast(ref RayOctreeQuery query);
        /// Return the closest drawable object by a ray query.
        void RaycastSingle(ref RayOctreeQuery query);

        void DrawDebugGeometry(bool depthTest);
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
        public vec3 position_;
        /// Hit normal in world space. Negation of ray direction if per-triangle data not available.
        public vec3 normal_;
        /// Hit texture position
        public vec2 textureUV_;
        /// Distance from ray origin.
        public float distance_;
        /// Drawable.
        public Drawable drawable_;
        /// Scene node.
        public Node node_;
        /// Drawable specific subobject if applicable.
        public uint subObject_;
        
    }

    public struct RayOctreeQuery
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
