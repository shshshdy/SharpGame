using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    [DataContract]
    public class Component : Object
    {
        [IgnoreDataMember]
        public Node Node => node_;
        protected Node node_;

        [IgnoreDataMember]
        public Scene Scene => node_ == null ? null : node_.Scene;

        [DataMember(Order = 1)]
        public bool Enabled { get { return enabled_; } set { SetEnabled(value); } }        
        protected bool enabled_ = true;

        public void SetNode(Node node)
        {
            node_ = node;
            OnNodeSet(node_);
        }

        public void SetEnabled(bool enable)
        {
            if (enable != enabled_)
            {
                enabled_ = enable;
                OnSetEnabled();
            }
        }

        public bool IsEnabledEffective()
        {
            return enabled_ && node_ != null && node_.Enabled;
        }

        public T GetComponent<T>() where T : Component => GetComponent(typeof(T)) as T;

        public Component GetComponent(Type type)
        {
            return node_ ? node_.GetComponent(type) : null;
        }

        public void GetComponents<T>(List<T> dest) where T : Component
        {
            if (node_)
                node_.GetComponents(dest);
            else
                dest.Clear();
        }

        public void GetComponents(List<Component> dest, Type type)
        {
            if (node_)
                node_.GetComponents(dest, type);
            else
                dest.Clear();
        }

        public void Remove()
        {
            if (node_ != null)
                node_.RemoveComponent(this);
        }

        public virtual void OnSetEnabled()
        {
        }

        public virtual void OnNodeSet(Node node)
        {
        }

        public virtual void OnSceneSet(Scene scene)
        {
        }

        public virtual void OnMarkedDirty(Node node)
        {
        }

        public virtual void OnNodeSetEnabled(Node node)
        {
        }

    }
}
