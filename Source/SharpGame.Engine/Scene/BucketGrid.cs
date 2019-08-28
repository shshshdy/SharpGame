using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGame
{
    public class BucketGrid<T> where T : Drawable
    {
        private readonly List<T> CachedList = new List<T>(); 
        private readonly float m_BucketWidth;
        private readonly float m_BucketHeight;
        private readonly int m_NumBucketsWidth;
        private readonly int m_NumBucketsHeight;

        private readonly Queue<T> m_PendingInsertion;
        private readonly Queue<T> m_PendingRemoval;

        private readonly RectangleF m_Region;
        private readonly List<T>[] m_Buckets;

        /// <summary>
        /// The number of objects contained in the <see cref="BucketGrid{T}"/>
        /// </summary>
        public int Count
        {
            get { return m_Buckets.Where(c => c != null).Sum(c => c.Count); }
        }

        // TODO: uint param type
        public BucketGrid(RectangleF region, int numBucketsWidth, int numBucketsHeight)
        {
            m_Region = region;
            m_Buckets = new List<T>[numBucketsWidth * numBucketsHeight];

            m_NumBucketsWidth = numBucketsWidth;
            m_NumBucketsHeight = numBucketsHeight;

            m_BucketWidth = region.Width / m_NumBucketsWidth;
            m_BucketHeight = region.Height / m_NumBucketsHeight;

            m_PendingInsertion = new Queue<T>();
            m_PendingRemoval = new Queue<T>();
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
                    var idx = bucket[j].index;// FindBucketIndex(bucket[j].Position);
                    if (idx == i) continue;
                    if (m_Buckets[idx] == null) m_Buckets[idx] = new List<T>();
                    m_Buckets[idx].Add(bucket[j]);
                    bucket.RemoveAt(j);
                }
            }

            lock (m_PendingInsertion)
            {
                while (m_PendingInsertion.Count > 0)
                {
                    var obj = m_PendingInsertion.Dequeue();
                    var idx = obj.index;//FindBucketIndex(obj.Position);
                    if (m_Buckets[idx] == null) m_Buckets[idx] = new List<T>();
                    m_Buckets[idx].Add(obj);
                }
            }

            lock (m_PendingRemoval)
            {
                while (m_PendingRemoval.Count > 0)
                {
                    var obj = m_PendingRemoval.Dequeue();
                    var idx = obj.index;//FindBucketIndex(obj.Position);
                    if (m_Buckets[idx] == null) m_Buckets[idx] = new List<T>();
                    m_Buckets[idx].Remove(obj);
                    if (m_Buckets[idx].Count == 0) m_Buckets[idx] = null;
                }
            }
        }

        /// <summary>
        /// Adds the given <see cref="Transformable"/> to the BucketGrid.
        /// Internal BucketGrid is not updated until the next call to Update.
        /// </summary>
        public void Add(T t)
        {
#if DEBUG
            if (t == null)
                throw new ArgumentException("Cannot add a null object to the BucketGrid");
#endif
            lock (m_PendingInsertion)
                m_PendingInsertion.Enqueue(t);
        }

        /// <summary>
        /// Removes the given <see cref="Transformable"/> from the BucketGrid.
        /// Internal BucketGrid is not updated until the next call to Update.
        /// </summary>
        public void Remove(T t)
        {
#if DEBUG
            if (t == null)
                throw new ArgumentException("Cannot remove a null object from the BucketGrid");
#endif
            lock (m_PendingRemoval)
                m_PendingRemoval.Enqueue(t);
        }

        #region Non-Thread-Safe Queries

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the query is not thread safe.
        /// </summary>
        public T[] GetObjectsInRange(Vector2 pos, float range = float.MaxValue)
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
        public T[] GetObjectsInRect(RectangleF rect)
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
        public T GetClosestObject(Vector2 pos, float maxDistance = float.MaxValue)
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
        public void GetObjectsInRange(Vector2 pos, float range, IList<T> results)
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
        public void GetObjectsInRect(RectangleF rect, IList<T> results)
        {
#if DEBUG
            if (results == null)
                throw new ArgumentException("Results list cannot be null");
#endif
            ObjectsInRectSearch(rect, results);
        }

        #endregion

        static float SquaredLength(Vector3 v, Vector2 v1)
        {
            float xx = v.X - v1.X;
            float yy = v.Z - v1.Y;
            return (xx * xx) + (yy * yy);
        }

        private T NearestNeighborSearch(Vector2 pos, float range)
        {
            T closest = null;
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

        private void AllNearestNeighborSearch(Vector2 pos, float range, IList<T> results)
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

        private void ObjectsInRectSearch(RectangleF rect, ICollection<T> results)
        {
            var idx = FindBucketIndex(rect.Center);

            var range = (rect.Center - rect.Location).Length();

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

        private int FindBucketIndex(Vector2 pos)
        {
            // TODO: what happens if pos is out of bounds?

            var fromLeft = pos.X - m_Region.Left;
            var x = (int)(fromLeft / m_BucketWidth);

            var fromTop = pos.Y - m_Region.Top;
            var y = (int)(fromTop / m_BucketHeight);

            return x + (y * m_NumBucketsWidth);
        }
    }
}
