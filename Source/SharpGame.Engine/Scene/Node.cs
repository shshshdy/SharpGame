using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public enum TransformSpace
    {
        LOCAL = 0,
        PARENT,
        WORLD
    };

    [DataContract]
    public partial class Node : Object, IEnumerable<Component>
    {
        #region ATTRIBUTE

        [DataMember(Order = 0)]
        public int ID { get; set; }

        [DataMember(Order = 1)]
        public StringID Name { get; set; }

        [DataMember(Order = 2)]
        public bool Enabled { get; set; } = true;
        protected bool enabled_ = true;

        [DataMember(Order = 3)]
        public uint Flags { get => flags_; set => SetFlags(value, false); }
        uint flags_;

        [DataMember(Order = 4)]
        public byte Layer { get => layer_; set => SetLayer(value, false); }
        byte layer_ = 0;

        [DataMember(Order = 11)]
        public List<Component> ComponentList => components_;
        protected List<Component> components_ = new List<Component>();
        
        [DataMember(Order = 12)]
        public List<Node> Children => children_;
        protected List<Node> children_ = new List<Node>();


        #endregion


        protected bool dirty_ = true;

        /// Node listeners
        List<Component> listeners_;

        public Node()
        {
        }

        public Node(string name, vec3 pos = default, vec3 euler = default)
        {
            Name = name;
            Position = pos;
            EulerAngles = euler;
        }

        public void SetFlags(uint flags, bool recurse)
        {
            flags_ = flags;
        }

        public void SetLayer(byte layer, bool recurse)
        {
            layer_ = layer;
        }

        public int GetNumChildren(bool recursive)
        {
            if (!recursive)
                return children_.Count;
            else
            {
                int allChildren = children_.Count;
                foreach (Node c in children_)
                    allChildren += c.GetNumChildren(true);

                return allChildren;
            }
        }

        public void GetChildren(List<Node> dest, bool recursive)
        {
            dest.Clear();

            if (!recursive)
            {
                dest.AddRange(children_);
            }
            else
                GetChildrenRecursive(dest);
        }

        public List<Node> GetChildren(bool recursive)
        {
            List<Node> dest = new List<Node>();
            GetChildren(dest, recursive);
            return dest;
        }

        public Node CreateChild(string name)
        {
            Node newNode = new Node
            {
                Name = name
            };
            AddChild(newNode);
            return newNode;
        }

        public Node CreateChild(string name, vec3 pos = default, vec3 euler = default)
        {
            Node newNode = new Node
            {
                Name = name,
                Position = pos,
                Rotation = glm.quat(euler)
            };
            AddChild(newNode);
            return newNode;
        }

        public void AddChild(Node node)
        {
            // Check for illegal or redundant parent assignment
            if (node == null || node == this || node.parent_ == this)
                return;
            // Check for possible cyclic parent assignment
            if (IsChildOf(node))
                return;

            // Keep a shared ptr to the node while transferring
            Node nodeShared = node;
            Node oldParent = node.parent_;
            if (oldParent != null)
            {
                // If old parent is in different scene, perform the full removal
                if (oldParent.Scene != scene_)
                    oldParent.RemoveChild(node);
                else
                {
                    oldParent.children_.Remove(nodeShared);
                }
            }

            // Add to the child vector, then add to the scene if not added yet
            children_.Add(nodeShared);
            if (scene_ != null && node.Scene != scene_)
                scene_.NodeAdded(node);

            node.parent_ = this;
            node.MarkDirty();
        }

        public void RemoveChild(Node node)
        {
            if (node == null)
                return;

            for (int i = 0; i < children_.Count; i++)
            {
                if (children_[i] == node)
                {
                    RemoveChild(i);
                    return;
                }
            }

        }

        void RemoveChild(int i)
        {
            Node child = children_[i];
            child.parent_ = null;
            child.MarkDirty();
            if (scene_ != null)
                scene_.NodeRemoved(child);

            children_.RemoveAt(i);
        //    child.Release();
        }

        public void RemoveAllChildren()
        {
            RemoveChildren(true);
        }

        public void RemoveChildren(bool recursive)
        {
            for (int i = children_.Count - 1; i >= 0; --i)
            {
                Node childNode = children_[i];

                if (recursive)
                    childNode.RemoveChildren(true);

                RemoveChild(i);
            }

        }

        public bool IsChildOf(Node node)
        {
            Node parent = parent_;
            while (parent != null)
            {
                if (parent == node)
                    return true;
                parent = parent.parent_;
            }

            return false;
        }

        public Node GetChild(StringID nameHash, bool recursive)
        {
            foreach (Node c in children_)
            {
                if (c.Name == nameHash)
                    return c;

                if (recursive)
                {
                    Node node = c.GetChild(nameHash, true);
                    if (node)
                        return node;
                }
            }

            return null;
        }

        public void GetChildrenRecursive(List<Node> dest)
        {
            foreach (Node c in children_)
            {
                dest.Add(c);
                if (c.children_.Count > 0)
                    c.GetChildrenRecursive(dest);
            }
        }

        public void GetChildrenWithComponentRecursive(List<Node> dest, Type type)
        {
            foreach (Node c in children_)
            {
                if (c.HasComponent(type))
                    dest.Add(c);
                if (c.children_.Count > 0)
                    c.GetChildrenWithComponentRecursive(dest, type);
            }
        }

        public void GetComponentsRecursive<T>(List<T> dest) where T : Component
        {
            foreach (Component c in components_)
            {
                if (c.GetType() == typeof(T))
                    dest.Add(c as T);
            }

            foreach (Node c in children_)
            {
                c.GetComponentsRecursive(dest);
            }
        }

        public void GetComponentsRecursive(List<Component> dest, Type type)
        {
            foreach (Component c in components_)
            {
                if (c.GetType() == type)
                    dest.Add(c);
            }

            foreach (Node c in children_)
            {
                c.GetComponentsRecursive(dest, type);
            }
        }

        public T CreateComponent<T>() where T : Component, new()
        {
            T t = new T();
            AddComponent(t);
            return t;
        }

        public T GetOrCreateComponent<T>() where T : Component, new()
        {
            T t = GetComponent<T>();
            if (!t)
            {
                t = new T();
                AddComponent(t);
            }
            return t;
        }

        public T GetComponent<T>(bool recursive = false) where T : Component
            => GetComponent(typeof(T), recursive) as T;

        public void GetComponents<T>(List<T> dest, bool recursive = false) where T : Component
        {
            dest.Clear();

            if (!recursive)
            {
                foreach (var i in components_)
                {
                    if (i.GetType() == typeof(T))
                        dest.Add(i as T);
                }
            }
            else
                GetComponentsRecursive(dest);
        }

        public void GetComponents(List<Component> dest, Type type, bool recursive = false)
        {
            dest.Clear();

            if (!recursive)
            {
                foreach(var i in components_)
                {
                    if (i.GetType() == type)
                        dest.Add(i);
                }
            }
            else
                GetComponentsRecursive(dest, type);
        }

        public bool HasComponent(Type type)
        {
            foreach (Component c in components_)
            {
                if (c.GetType() == type)
                    return true;
            }
            return false;
        }

        public Component GetComponent(Type type, bool recursive = false)
        {
            foreach (Component c in components_)
            {
                if (c.GetType() == type)
                    return c;
            }

            if (recursive)
            {
                foreach (var c in children_)
                {
                    Component component = c.GetComponent(type, true);
                    if (component)
                        return component;
                }
            }

            return null;
        }

        public Component GetParentComponent(Type type, bool fullTraversal)
        {
            Node current = Parent;
            while (current)
            {
                Component soughtComponent = current.GetComponent(type);
                if (soughtComponent)
                    return soughtComponent;

                if (fullTraversal)
                    current = current.Parent;
                else
                    break;
            }

            return null;
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var c = new T();
            AddComponent(c);
            return c;
        }

        public void AddComponent(Component component)
        {
            components_.Add(component);

            if (component.Node != null)
                Log.Warn("Component " + component.GetType() + " already belongs to a node!");

            component.SetNode(this);

            if (scene_ != null)
            {
                scene_.ComponentAdded(component);
            }

            component.OnMarkedDirty(this);
        }

        public void RemoveComponent(Component component)
        {
            for (int i = 0; i < components_.Count; i++)
            {
                if (components_[i] == component)
                {
                    RemoveComponent(i);
                    break;
                }
            }
        }

        public void RemoveComponent(int index)
        {
            Component component = components_[index];
            RemoveListener(component);

            if (scene_ != null)
                scene_.ComponentRemoved(component);

            component.SetNode(null);
            components_.RemoveAt(index);
            //component.Release();
        }

        public void RemoveAllComponents()
        {
            for (int i = components_.Count - 1; i >= 0; --i)
            {
                RemoveComponent(i);
            }
        }

        public void Remove()
        {
            if (parent_)
                parent_.RemoveChild(this);
        }

        public void AddListener(Component component)
        {
            if (component == null)
                return;

            if (listeners_ != null)
            {
                if (listeners_.Contains(component))
                {
                    return;
                }
            }
            else
            {
                listeners_ = new List<Component>();
            }

            listeners_.Add(component);
            // If the node is currently dirty, notify immediately
            if (dirty_)
                component.OnMarkedDirty(this);
        }

        public void RemoveListener(Component component)
        {
            listeners_?.Remove(component);
        }

        internal void ResetScene()
        {
            ID = 0;
            scene_ = null;
        }

        protected override void Destroy()
        {
            base.Destroy();

            RemoveAllChildren();
            RemoveAllComponents();

            // Remove from the scene
            if (scene_ != null)
                scene_.NodeRemoved(this);

            NativePool<mat4>.Shared.Release(worldTransform_);
        }

        public void Add(Component component)
        {
            AddComponent(component);
        }

        public void Add(Node child)
        {
            AddChild(child);
        }

        public IEnumerator<Component> GetEnumerator()
        {
            return ComponentList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ComponentList.GetEnumerator();
        }
    }


}
