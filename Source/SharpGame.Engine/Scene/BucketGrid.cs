using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace SharpGame
{
    public class BucketGrid : Component, ISpacePartitioner
    {
        public RectangleF Region { get => m_Region; set => m_Region = value; }
        private RectangleF m_Region;
        public float MinHeight { get; set; }
        public float MaxHeight { get; set; }

        private readonly List<Drawable> CachedList = new List<Drawable>(); 
        private readonly float m_BucketWidth;
        private readonly float m_BucketHeight;
        private readonly int m_NumBucketsWidth;
        private readonly int m_NumBucketsHeight;

        private readonly FastList<Drawable>[] m_Buckets;
        private readonly BoundingBox[] m_BoundingBoxes;

        /// <summary>
        /// The number of objects contained in the <see cref="BucketGrid{T}"/>
        /// </summary>
        [IgnoreDataMember]
        public int Count
        {
            get { return m_Buckets.Where(c => c != null).Sum(c => c.Count); }
        }

        DefaultObjectPool<FastList<Drawable>> defaultPool;

        public BucketGrid(BoundingBox region, int numBucketsWidth, int numBucketsHeight)
            : this(new RectangleF(region.Minimum.X, region.Minimum.Z, region.Size.X, region.Size.Z), numBucketsWidth, numBucketsHeight)
        {
            MinHeight = region.Minimum.Y;
            MaxHeight = region.Maximum.Y;

            m_BoundingBoxes = new BoundingBox[numBucketsWidth * numBucketsHeight];

            for (int i = 0; i < numBucketsWidth; i++)
            {
                for (int j = 0; j < numBucketsHeight; i++)
                {
                    ref BoundingBox bbox = ref m_BoundingBoxes[i + j * numBucketsWidth];
                    vec3 min = new vec3(m_BucketWidth * (i + 1), -1000, m_BucketHeight * (j + 1));
                    vec3 max = new vec3(m_BucketWidth * (i + 1), 1000, m_BucketHeight * (j + 1));
                    bbox.Define(ref min, ref max);
                }
            }
        }

        /// <summary>
        /// 2D Mode
        /// </summary>
        /// <param name="region"></param>
        /// <param name="numBucketsWidth"></param>
        /// <param name="numBucketsHeight"></param>
        public BucketGrid(RectangleF region, int numBucketsWidth, int numBucketsHeight)
        {
            m_Region = region;
            m_Buckets = new FastList<Drawable>[numBucketsWidth * numBucketsHeight];
            m_NumBucketsWidth = numBucketsWidth;
            m_NumBucketsHeight = numBucketsHeight;
            m_BucketWidth = region.Width / m_NumBucketsWidth;
            m_BucketHeight = region.Height / m_NumBucketsHeight;

            var defalutPolicy = new DefaultPooledObjectPolicy<FastList<Drawable>>();
            defaultPool = new DefaultObjectPool<FastList<Drawable>>(defalutPolicy);

        }

        /// <summary>
        /// Updates the <see cref="BucketGrid{T}"/> by adding and/or
        /// removing any items passed to <see cref="Add"/> or <see cref="Remove"/>
        /// and by updating the grid to take into account objects that have moved
        /// </summary>
        public void Update()
        {
            // Make sure all objects are in the right buckets
            for (var i = 0; i < m_Buckets.Length; i++)
            {
                var bucket = m_Buckets[i];
                if (bucket == null)
                    continue;

                for (var j = bucket.Count - 1; j >= 0; j--)
                {
                    var idx = FindBucketIndex(bucket[j].WorldCenter);
                    if (idx == i) continue;
                    if (m_Buckets[idx] == null) m_Buckets[idx] = defaultPool.Get();
                    m_Buckets[idx].Add(bucket[j]);
                    bucket.RemoveAt(j);
                }
            }

        }
        
        #region Non-Thread-Safe Queries

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the query is not thread safe.
        /// </summary>
        public Drawable[] GetObjectsInRange(vec2 pos, float range = float.MaxValue)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
#endif
            CachedList.Clear();
            AllNearestNeighborSearch(pos, range, CachedList);
            return CachedList.ToArray();
        }

        /// <summary>
        /// Gets all objects within the given <see cref="FloatRect"/>.
        /// This version of the query is not thread safe.
        /// </summary>
        public Drawable[] GetObjectsInRect(RectangleF rect)
        {
            CachedList.Clear();
            ObjectsInRectSearch(rect, CachedList);
            return CachedList.ToArray();
        }

        #endregion

        #region Thread-Safe Queries

        /// <summary>
        /// Gets the closest object to the given position.
        /// This version of the query is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public Drawable GetClosestObject(vec2 pos, float maxDistance = float.MaxValue)
        {
#if DEBUG
            if (maxDistance < 0f)
                throw new ArgumentException("Range cannot be negative");
#endif
            return NearestNeighborSearch(pos, maxDistance);
        }

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the query is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetObjectsInRange(vec2 pos, float range, IList<Drawable> results)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
            if (results == null)
                throw new ArgumentException("Results list cannot be null");
#endif
            AllNearestNeighborSearch(pos, range, results);
        }

        /// <summary>
        /// Gets all objects within the given <see cref="FloatRect"/>.
        /// This version of the query is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetObjectsInRect(RectangleF rect, IList<Drawable> results)
        {
#if DEBUG
            if (results == null)
                throw new ArgumentException("Results list cannot be null");
#endif
            ObjectsInRectSearch(rect, results);
        }

        #endregion

        static float SquaredLength(vec3 v, vec2 v1)
        {
            float xx = v.X - v1.X;
            float yy = v.Z - v1.Y;
            return (xx * xx) + (yy * yy);
        }

        private Drawable NearestNeighborSearch(vec2 pos, float range)
        {
            Drawable closest = null;
            var idx = FindBucketIndex(pos);

            var bucketRangeX = (int) (range / m_BucketWidth) + 1;
            if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
            var bucketRangeY = (int) (range / m_BucketHeight) + 1;
            if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

            for (int d = 0; d <= Math.Max(bucketRangeX, bucketRangeY); d++)
            {
                int dx = Math.Min(d, bucketRangeX);
                int dy = Math.Min(d, bucketRangeY);

                foreach (int nextIdx in BucketsAtRange(idx, dx, dy))
                {
                    // If index is out of range
                    if (nextIdx < 0 || nextIdx >= m_Buckets.Length)
                        continue;

                    if (m_Buckets[nextIdx] == null)
                        continue;

                    var bucket = m_Buckets[nextIdx];
                    for (int k = 0; k < bucket.Count; k++)
                    {
                        var ds = SquaredLength(bucket[k].WorldCenter, pos);
                        if (ds < range * range)
                        {
                            closest = bucket[k];
                            range = (float)Math.Sqrt(ds);
                        }
                    }

                    bucketRangeX = (int)(range / m_BucketWidth) + 1;
                    if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
                    bucketRangeY = (int)(range / m_BucketHeight) + 1;
                    if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;
                }
            }

            return closest;
        }

        private void AllNearestNeighborSearch(vec2 pos, float range, IList<Drawable> results)
        {
            var idx = FindBucketIndex(pos);

            var bucketRangeX = (int)(range / m_BucketWidth) + 1;
            if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
            var bucketRangeY = (int)(range / m_BucketHeight) + 1;
            if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

            for (int i = -bucketRangeX; i <= bucketRangeX; i++)
            {
                for (int j = -bucketRangeY; j <= bucketRangeY; j++)
                {
                    var nextIdx = idx + (i + (j * m_NumBucketsWidth));
                    // If index is out of range
                    if (nextIdx < 0 || nextIdx >= m_Buckets.Length)
                        continue;

                    if (m_Buckets[nextIdx] == null)
                        continue;

                    var bucket = m_Buckets[nextIdx];
                    for (int n = 0; n < bucket.Count; n++)
                    {
                        var ds = SquaredLength(bucket[n].WorldCenter, pos);
                        if (ds > range * range)
                            continue;

                        results.Add(bucket[n]);
                    }
                }
            }
        }

        private void ObjectsInRectSearch(RectangleF rect, ICollection<Drawable> results)
        {
            var idx = FindBucketIndex(rect.Center);

            var range = glm.length(rect.Center - rect.Location);

            var bucketRangeX = (int)(range / m_BucketWidth) + 1;
            if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
            var bucketRangeY = (int)(range / m_BucketHeight) + 1;
            if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

            for (int i = -bucketRangeX; i <= bucketRangeX; i++)
            {
                for (int j = -bucketRangeY; j <= bucketRangeY; j++)
                {
                    var nextIdx = idx + (i + (j * m_NumBucketsWidth));
                    // If index is out of range
                    if (nextIdx < 0 || nextIdx >= m_Buckets.Length)
                        continue;

                    if (m_Buckets[nextIdx] == null)
                        continue;

                    var bucket = m_Buckets[nextIdx];
                    for (int n = 0; n < bucket.Count; n++)
                    {
                        if (!rect.Contains(bucket[n].WorldCenter.X, bucket[n].WorldCenter.Z))
                            continue;

                        results.Add(bucket[n]);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the rectangle of indices the given 
        /// dx and dy away from the given center
        /// </summary>
        private IEnumerable<int> BucketsAtRange(int center, int dx, int dy)
        {
            // Top row
            for (int i = -dx; i <= dx; i++)
            {
                yield return center + (i - (dy * m_NumBucketsWidth));
            }

            // Bottom row
            if (dy != 0)
            {
                for (int i = -dx; i <= dx; i++)
                {
                    yield return center + (i + (dy * m_NumBucketsWidth));
                }
            }

            // Left column
            for (int j = -dy + 1; j <= dy - 1; j++)
            {
                if (dx == 0 && j == 0)
                    continue;
                yield return center + (-dx + (j * m_NumBucketsWidth));
            }

            // right column
            if (dx != 0)
            {
                for (int j = -dy + 1; j <= dy - 1; j++)
                {
                    if (dx == 0 && j == 0)
                        continue;
                    yield return center + (dx + (j * m_NumBucketsWidth));
                }
            }
        }

        [MethodImpl((MethodImplOptions)0x100)]
        private int FindBucketIndex(vec2 pos)
        {
            var fromLeft = pos.X - m_Region.Left;
            var x = MathUtil.Clamp((int)(fromLeft / m_BucketWidth), 0, m_NumBucketsWidth - 1);

            var fromTop = pos.Y - m_Region.Top;
            var y = MathUtil.Clamp((int)(fromTop / m_BucketHeight), 0, m_NumBucketsHeight - 1);

            return x + (y * m_NumBucketsWidth);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        private int FindBucketIndex(vec3 pos)
        {
            var fromLeft = pos.X - m_Region.Left;
            var x = MathUtil.Clamp((int)(fromLeft / m_BucketWidth), 0, m_NumBucketsWidth - 1);

            var fromTop = pos.Z - m_Region.Top;
            var y = MathUtil.Clamp((int)(fromTop / m_BucketHeight), 0, m_NumBucketsHeight - 1);

            return x + (y * m_NumBucketsWidth);
        }

        public void InsertDrawable(Drawable drawable)
        {
            var idx = FindBucketIndex(drawable.WorldCenter);
            if (m_Buckets[idx] == null) m_Buckets[idx] = defaultPool.Get();
            m_Buckets[idx].Add(drawable);           
        }

        public void RemoveDrawable(Drawable drawable)
        {
            var idx = FindBucketIndex(drawable.WorldCenter);
            if (m_Buckets[idx] == null)
            {
                Log.Error("Error drawable index.");
                return;
            }

            m_Buckets[idx].FastRemove(drawable);

            if (m_Buckets[idx].Count == 0)
            {
                defaultPool.Return(m_Buckets[idx]);
                m_Buckets[idx] = null;
            }
        }
        
        public void GetDrawables(SceneQuery query, Action<Drawable> drawables)
        {
            throw new NotImplementedException();
        }

        public void Raycast(ref RayOctreeQuery query)
        {
            throw new NotImplementedException();
        }

        public void RaycastSingle(ref RayOctreeQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
