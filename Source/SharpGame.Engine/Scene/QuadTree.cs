using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace SharpGame
{
    using FloatRect = RectangleF;

    public class QuadTree<T> : IEnumerable<T>
        where T : Drawable
    {
        private const int NORTH_WEST = 0;
        private const int NORTH_EAST = 1;
        private const int SOUTH_WEST = 2;
        private const int SOUTH_EAST = 3;

        // To avoid memory allocation, we define statics collection to be re-used for scratch work
        // Note that these are not used in function chains claiming to be thread safe
        private static readonly List<T> CachedList = new List<T>();

        private readonly Queue<T> m_PendingInsertion;
        private readonly Queue<T> m_PendingRemoval;

        private FloatRect m_Region;
        private readonly List<T> m_Objects;

        private QuadTree<T> m_NorthWest;
        private QuadTree<T> m_NorthEast;
        private QuadTree<T> m_SouthWest;
        private QuadTree<T> m_SouthEast;

        private bool m_Leaf = true;

        private int m_Count;

        /// <summary>
        /// The minimum size of a leaf QuadTree.
        /// <see cref="NumObjects"/> will not be 
        /// respected for leafs of this size.
        /// </summary>
        public static float MinSize = 1;

        /// <summary>
        /// The maximum number of objects to add to a 
        /// leaf node before splitting. Will be ignored 
        /// for nodes of size <see cref="MinSize"/>.
        /// </summary>
        public static int NumObjects = 1;

        public int Count => m_Count;

        private QuadTree(FloatRect bounds, T obj)
        {
            m_Region = bounds;
            m_Objects = new List<T>(NumObjects) { obj };
            m_Count = 1;
        }

        public QuadTree(FloatRect bounds)
        {
            m_PendingInsertion = new Queue<T>();
            m_PendingRemoval = new Queue<T>();

            m_Region = bounds;
            m_Objects = new List<T>(NumObjects);
        }

        /// <summary>
        /// Adds the given <see cref="Transformable"/> to the <see cref="QuadTree{T}"/>.
        /// The <see cref="QuadTree{T}"/> is not updated until the next call to <see cref="Update"/>.
        /// </summary>
        public void Add(T t)
        {
#if DEBUG
            if (t == null)
                throw new ArgumentException("Cannot add a null object to the QuadTree");
#endif

            lock (m_PendingInsertion)
                m_PendingInsertion.Enqueue(t);
        }

        /// <summary>
        /// Removes the given <see cref="Transformable"/> from the <see cref="QuadTree{T}"/>.
        /// The <see cref="QuadTree{T}"/> is not updated until the next call to <see cref="Update"/>.
        /// </summary>
        public void Remove(T t)
        {
#if DEBUG
            if (t == null)
                throw new ArgumentException("Cannot remove a null object to the QuadTree");
#endif
            lock (m_PendingRemoval)
                m_PendingRemoval.Enqueue(t);
        }

        /// <summary>
        /// Updates the <see cref="QuadTree{T}"/> by adding and/or
        /// removing any items passed to <see cref="Add"/> or <see cref="Remove"/>
        /// and by correcting the tree for objects that have moved
        /// </summary>
        public void Update()
        {
            CachedList.Clear();
            CorrectTree(CachedList);

#if DEBUG
            if (CachedList.Count > 0)
                throw new InvalidOperationException("An object has moved out of the tree completely");
#endif

            lock (m_PendingRemoval)
            {
                while (m_PendingRemoval.Count > 0)
                {
                    Delete(m_PendingRemoval.Dequeue());
                }
            }

            lock (m_PendingInsertion)
            {
                while (m_PendingInsertion.Count > 0)
                {
                    Insert(m_PendingInsertion.Dequeue());
                }
            }

            Prune();
        }

        /// <summary>
        /// Clears the tree. Modifies the <see cref="QuadTree{T}"/> immediately.
        /// </summary>
        public void Clear()
        {
            m_NorthWest = null;
            m_NorthEast = null;
            m_SouthWest = null;
            m_SouthEast = null;

            m_Leaf = true;
            m_Objects.Clear();

            lock (m_PendingInsertion)
            {
                m_PendingInsertion.Clear();
            }
            lock (m_PendingRemoval)
            {
                m_PendingRemoval.Clear();
            }
        }

        #region Non-Thread-Safe Queries

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the query is not thread safe.
        /// </summary>
        public T[] GetObjectsInRange(vec2 pos, float range = float.MaxValue)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
#endif
            CachedList.Clear();
            AllNearestNeighborsSearch(pos, range * range, CachedList);

            return CachedList.ToArray();
        }

        /// <summary>
        /// Gets all objects within the given <see cref="FloatRect"/>.
        /// This version of the query is not thread safe.
        /// </summary>
        public T[] GetObjectsInRect(FloatRect rect)
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
        public T GetClosestObject(vec2 pos, float maxDistance = float.MaxValue)
        {
#if DEBUG
            if (maxDistance < 0f)
                throw new ArgumentException("Range cannot be negative");
#endif
            T result = null;
            float distanceSquared = maxDistance * maxDistance;
            NearestNeighborSearch(pos, ref distanceSquared, ref result);
            return result;
        }

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the query is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetObjectsInRange(vec2 pos, float range, IList<T> results)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
            if (results == null)
                throw new ArgumentException("Results list cannot be null");
#endif
            AllNearestNeighborsSearch(pos, range * range, results);
        }

        /// <summary>
        /// Gets all objects within the given <see cref="FloatRect"/>.
        /// This version of the query is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetObjectsInRect(FloatRect rect, IList<T> results)
        {
#if DEBUG
            if (results == null)
                throw new ArgumentException("Results list cannot be null");
#endif
            ObjectsInRectSearch(rect, results);
        }

        #endregion

        #region Internal Queries

        static float SquaredLength(vec3 v, vec2 v1)
        {
            float xx = v.X - v1.x;
            float yy = v.Z - v1.y;
            return (xx * xx) + (yy * yy);
        }

        private void NearestNeighborSearch(vec2 pos, ref float distanceSquared, ref T closest)
        {
            if (m_Leaf)
            {
                for (int i = 0; i < m_Objects.Count; i++)
                {
                    T obj = m_Objects[i];

                    float ds = SquaredLength(obj.WorldCenter, pos);

                    if (ds > distanceSquared)
                        continue;

                    distanceSquared = ds;
                    closest = obj;
                }
            }
            else
            {
                // Search in order of closeness to the given position
                int quad = GetQuadrant(pos);
                switch (quad)
                {
                    case NORTH_WEST:
                        if (m_NorthWest != null && m_NorthWest.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_NorthWest.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_NorthEast != null && m_NorthEast.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_NorthEast.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_SouthWest != null && m_SouthWest.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_SouthWest.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_SouthEast != null && m_SouthEast.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_SouthEast.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        break;
                    case NORTH_EAST:
                        if (m_NorthEast != null && m_NorthEast.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_NorthEast.NearestNeighborSearch(pos ,ref distanceSquared, ref closest);
                        if (m_NorthWest != null && m_NorthWest.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_NorthWest.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_SouthEast != null && m_SouthEast.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_SouthEast.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_SouthWest != null && m_SouthWest.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_SouthWest.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        break;
                    case SOUTH_WEST:
                        if (m_SouthWest != null && m_SouthWest.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_SouthWest.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_SouthEast != null && m_SouthEast.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_SouthEast.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_NorthWest != null && m_NorthWest.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_NorthWest.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_NorthEast != null && m_NorthEast.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_NorthEast.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        break;
                    case SOUTH_EAST:
                        if (m_SouthEast != null && m_SouthEast.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_SouthEast.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_SouthWest != null && m_SouthWest.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_SouthWest.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_NorthEast != null && m_NorthEast.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_NorthEast.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        if (m_NorthWest != null && m_NorthWest.m_Region.SquaredDistance(pos) < distanceSquared)
                            m_NorthWest.NearestNeighborSearch(pos, ref distanceSquared, ref closest);
                        break;
                }
            }
        }

        private void AllNearestNeighborsSearch(vec2 pos, float distanceSquared, IList<T> results)
        {
            if (m_Leaf)
            {
                for (int i = 0; i < m_Objects.Count; i++)
                {
                    T obj = m_Objects[i];

                    float ds = SquaredLength(obj.WorldCenter, pos);

                    if (ds > distanceSquared)
                        continue;

                    results.Add(obj);
                }
                return;
            }

            if (m_NorthWest != null && m_NorthWest.m_Region.SquaredDistance(pos) < distanceSquared)
            {
                m_NorthWest.AllNearestNeighborsSearch(pos, distanceSquared, results);
            }
            if (m_SouthEast != null && m_SouthEast.m_Region.SquaredDistance(pos) < distanceSquared)
            {
                m_SouthEast.AllNearestNeighborsSearch(pos, distanceSquared, results);
            }
            if (m_NorthEast != null && m_NorthEast.m_Region.SquaredDistance(pos) < distanceSquared)
            {
                m_NorthEast.AllNearestNeighborsSearch(pos, distanceSquared, results);
            }
            if (m_SouthWest != null && m_SouthWest.m_Region.SquaredDistance(pos) < distanceSquared)
            {
                m_SouthWest.AllNearestNeighborsSearch(pos, distanceSquared, results);
            }
        }

        private void ObjectsInRectSearch(FloatRect rect, ICollection<T> results)
        {
            if (m_Leaf)
            {
                for (int i = 0; i < m_Objects.Count; i++)
                {
                    T obj = m_Objects[i];

                    if (!rect.Contains(obj.WorldBoundingBox.Center.X, obj.WorldBoundingBox.Center.Z))
                        return;

                    results.Add(obj);
                }
            }

            if (m_NorthWest != null && m_NorthWest.m_Region.Intersects(rect))
            {
                m_NorthWest.ObjectsInRectSearch(rect, results);
            }
            if (m_SouthEast != null && m_SouthEast.m_Region.Intersects(rect))
            {
                m_SouthEast.ObjectsInRectSearch(rect, results);
            }
            if (m_NorthEast != null && m_NorthEast.m_Region.Intersects(rect))
            {
                m_NorthEast.ObjectsInRectSearch(rect, results);
            }
            if (m_SouthWest != null && m_SouthWest.m_Region.Intersects(rect))
            {
                m_SouthWest.ObjectsInRectSearch(rect, results);
            }
        }

        #endregion

        /// <summary>
        /// Corrects the tree by verifying that objects are within the correct regions.
        /// Objects that have moved out of this level of the tree will be added to
        /// the given <see cref="List{T}"/>.
        /// </summary>
        /// <param name="removed">
        /// A list in which to add objects that have 
        /// moved out of this level of the tee
        /// </param>
        private void CorrectTree(List<T> removed)
        {
            // Remove objects from this level that are not in our region
            if (m_Leaf)
            {
                for (int i = m_Objects.Count - 1; i >= 0; i--)
                {
                    T obj = m_Objects[i];
                    if (obj.WorldCenter.X < m_Region.Left
                        || obj.WorldCenter.Z < m_Region.Top
                        || obj.WorldCenter.X >= (m_Region.Left + m_Region.Width)
                        || obj.WorldCenter.Z >= (m_Region.Top + m_Region.Height))
                    {
                        removed.Add(obj);
                        m_Objects.RemoveAt(i);
                        m_Count--;
                    }
                }
            }
            // Check children for objects that have moved
            // out of their regions (and possibly ours)
            else
            {
                // Reuse the removed list. Anything before this index
                // is not coming from one of our children
                int firstChildIndex = removed.Count;
                m_NorthWest?.CorrectTree(removed);
                m_NorthEast?.CorrectTree(removed);
                m_SouthWest?.CorrectTree(removed);
                m_SouthEast?.CorrectTree(removed);

                for (int i = removed.Count - 1; i >= firstChildIndex; i--)
                {
                    T obj = removed[i];

                    if (obj.WorldCenter.X < m_Region.Left
                        || obj.WorldCenter.Z < m_Region.Top
                        || obj.WorldCenter.X >= (m_Region.Left + m_Region.Width)
                        || obj.WorldCenter.Z >= (m_Region.Top + m_Region.Height))
                    {
                        // The object is being removed from this level of the tree
                        m_Count--;
                        continue;
                    }

                    removed.RemoveAt(i);
                    Insert(obj);
                    // Insert increases our count but the object is not
                    // not really being added to the tree it has just 
                    // been moved up from a lower level
                    m_Count--;
                }
            }
        }

        /// <summary>
        /// Inserts the given object into the tree. Assumes that the given
        /// object is within the trees <see cref="m_Region"/>.
        /// </summary>
        private void Insert(T obj)
        {
#if DEBUG
            if (!m_Region.Contains(obj.WorldCenter.X, obj.WorldCenter.Z))
                throw new InvalidOperationException("Insert was called with an object not in the correct region");
#endif
            if (!m_Leaf)
            {
                InsertIntoChildren(obj);
                m_Count++;
                return;
            }

            m_Objects.Add(obj);
            m_Count++;

            if (m_Objects.Count > NumObjects)
            {
                // If this region is large enough to be split
                if (m_Region.Width > MinSize && m_Region.Height > MinSize)
                    Split();
            }
        }

        /// <summary>
        /// Inserts the given object into the appropriate child node.
        /// Assumes the given object is within the trees <see cref="m_Region"/>
        /// </summary>
        private void InsertIntoChildren(T obj)
        {
            if (obj.WorldCenter.X < m_Region.Left + (m_Region.Width / 2))
            {
                if (obj.WorldCenter.Z < m_Region.Top + (m_Region.Height / 2))
                {
                    if (m_NorthWest == null)
                    {
                        m_Leaf = false;
                        m_NorthWest = new QuadTree<T>(
                            new FloatRect(m_Region.Left, m_Region.Top,
                                m_Region.Width / 2, m_Region.Height / 2),
                            obj);
                    }
                    else
                        m_NorthWest.Insert(obj);
                }
                else
                {
                    if (m_SouthWest == null)
                    {
                        m_Leaf = false;
                        m_SouthWest = new QuadTree<T>(
                            new FloatRect(m_Region.Left, m_Region.Top + m_Region.Height / 2,
                                m_Region.Width / 2, m_Region.Height / 2),
                            obj);
                    }
                    else
                        m_SouthWest.Insert(obj);
                }
            }
            else
            {
                if (obj.WorldCenter.Z < m_Region.Top + (m_Region.Height / 2))
                {
                    if (m_NorthEast == null)
                    {
                        m_Leaf = false;
                        m_NorthEast = new QuadTree<T>(
                            new FloatRect(m_Region.Left + m_Region.Width / 2, m_Region.Top,
                                m_Region.Width / 2, m_Region.Height / 2),
                            obj);
                    }
                    else
                        m_NorthEast.Insert(obj);
                }
                else
                {
                    if (m_SouthEast == null)
                    {
                        m_Leaf = false;
                        m_SouthEast = new QuadTree<T>(
                            new FloatRect(m_Region.Left + m_Region.Width / 2, m_Region.Top + m_Region.Height / 2,
                                m_Region.Width / 2, m_Region.Height / 2),
                            obj);
                    }
                    else
                        m_SouthEast.Insert(obj);
                }
            }
        }

        /// <summary>
        /// Delets the given object from the tree.
        /// Assumes the given object is within the trees <see cref="m_Region"/>
        /// </summary>
        private void Delete(T obj)
        {
#if DEBUG
            if (!m_Region.Contains(obj.WorldCenter.X, obj.WorldCenter.Z))
                throw new InvalidOperationException("Delete was called with an object not in the correct region");
#endif
            if (m_Leaf)
            {
                if (m_Objects.Remove(obj))
                    m_Count--;
                return;
            }

            if (obj.WorldCenter.X < m_Region.Left + (m_Region.Width / 2))
            {
                if (obj.WorldCenter.Z < m_Region.Top + (m_Region.Height / 2))
                {
                    m_NorthWest.Delete(obj);
                    m_Count--;
                }
                else
                {
                    m_SouthWest.Delete(obj);
                    m_Count--;
                }
            }
            else
            {
                if (obj.WorldCenter.Z < m_Region.Top + (m_Region.Height / 2))
                {
                    m_NorthEast.Delete(obj);
                    m_Count--;
                }
                else
                {
                    m_SouthEast.Delete(obj);
                    m_Count--;
                }
            }
        }

        /// <summary>
        /// Splits this level of the tree by inserting objects into lower levels
        /// </summary>
        private void Split()
        {
            for (int i = 0; i < m_Objects.Count; i++)
            {
                InsertIntoChildren(m_Objects[i]);
            }
            m_Objects.Clear();
        }

        /// <summary>
        /// Prunes the tree by deleting empty leaf nodes
        /// </summary>
        private void Prune()
        {
            if (m_Leaf) return;

            // Can we condense child nodes into this one?
            if (m_Count <= NumObjects)
            {
                if (m_NorthWest != null) { m_Objects.AddRange(m_NorthWest); m_NorthWest = null; }
                if (m_NorthEast != null) { m_Objects.AddRange(m_NorthEast); m_NorthEast = null; }
                if (m_SouthWest != null) { m_Objects.AddRange(m_SouthWest); m_SouthWest = null; }
                if (m_SouthEast != null) { m_Objects.AddRange(m_SouthEast); m_SouthEast = null; }
                m_Leaf = true;
                return;
            }

            bool anyChildrenAlive = false;
            if (m_NorthWest != null)
            {
                if (m_NorthWest.m_Count == 0)
                    m_NorthWest = null;
                else
                {
                    m_NorthWest.Prune();
                    anyChildrenAlive = true;
                }
            }
            if (m_NorthEast != null)
            {
                if (m_NorthEast.m_Count == 0)
                    m_NorthEast = null;
                else
                {
                    m_NorthEast.Prune();
                    anyChildrenAlive = true;
                }
            }
            if (m_SouthWest != null)
            {
                if (m_SouthWest.m_Count == 0)
                    m_SouthWest = null;
                else
                {
                    m_SouthWest.Prune();
                    anyChildrenAlive = true;
                }
            }
            if (m_SouthEast != null)
            {
                if (m_SouthEast.m_Count == 0)
                    m_SouthEast = null;
                else
                {
                    m_SouthEast.Prune();
                    anyChildrenAlive = true;
                }
            }

            m_Leaf = !anyChildrenAlive;
        }

        private int GetQuadrant(vec2 pos)
        {
            if (pos.x < m_Region.Left + (m_Region.Width / 2))
            {
                return pos.y < m_Region.Top + (m_Region.Height / 2) 
                    ? NORTH_WEST
                    : SOUTH_WEST;
            }
            else
            {
                return pos.y < m_Region.Top + (m_Region.Height / 2) 
                    ? NORTH_EAST
                    : SOUTH_EAST;
            }
        }

        public void GetAllRegions(List<FloatRect> regions)
        {
            regions.Add(m_Region);
            m_NorthWest?.GetAllRegions(regions);
            m_NorthEast?.GetAllRegions(regions);
            m_SouthWest?.GetAllRegions(regions);
            m_SouthEast?.GetAllRegions(regions);
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (m_Objects.Count > 0)
            {
                foreach (T obj in m_Objects)
                    yield return obj;
            }
            if (m_NorthWest != null)
            {
                foreach (T obj in m_NorthWest)
                    yield return obj;
            }
            if (m_NorthEast != null)
            {
                foreach (T obj in m_NorthEast)
                    yield return obj;
            }
            if (m_SouthWest != null)
            {
                foreach (T obj in m_SouthWest)
                    yield return obj;
            }
            if (m_SouthEast != null)
            {
                foreach (T obj in m_SouthEast)
                    yield return obj;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
