using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    [DataContract]
    public class Scene : Node, ISceneAccumulator
    {
        const int FIRST_LOCAL_ID = 0x01000000;
        const int LAST_LOCAL_ID = int.MaxValue;
        
        protected Dictionary<int, Node> localNodes_ = new Dictionary<int, Node>();
        /// Next free local node ID.
        protected int localNodeID_;
        protected ISceneAccumulator accumulator_;
        protected List<Drawable> drawables_ = new List<Drawable>();
        public Scene()
        {
            ID = GetFreeNodeID();
            NodeAdded(this);
        }

        public Node GetNode(int id)
        {
            Node ret;
            if (localNodes_.TryGetValue(id, out ret))
            {
                return ret;
            }

            return null;
        }
        
        public int GetFreeNodeID()
        {
            for (;;)
            {
                int ret = localNodeID_;
                if (localNodeID_ < LAST_LOCAL_ID)
                    ++localNodeID_;
                else
                    localNodeID_ = FIRST_LOCAL_ID;

                if (!localNodes_.ContainsKey(ret))
                    return ret;
            }

        }
        
        public void NodeAdded(Node node)
        {
            if (node == null || node.Scene == this)
                return;

            // Remove from old scene first
            Scene oldScene = node.Scene;
            if (oldScene != null)
                oldScene.NodeRemoved(node);

            node.Scene = this;

            // If the new node has an ID of zero (default), assign a replicated ID now
            int id = node.ID;
            if (id == 0)
            {
                id = GetFreeNodeID();
                node.ID = id;
            }

            // If node with same ID exists, remove the scene reference from it and overwrite with the new node

            Node existNode = GetNode(id);
            if (existNode != null && existNode != this)
            {
                NodeRemoved(existNode);
            }

            localNodes_[id] = node;
            
            foreach (var c in node.ComponentList)
            {
                ComponentAdded(c);
            }

            foreach (var c in node.ChildList)
            {
                NodeAdded(c);
            }

        }
        
        public void NodeRemoved(Node node)
        {
            if (node == null || node.Scene != this)
            {
                return;
            }

            int id = node.ID;        
            localNodes_.Remove(id);

            foreach (var c in node.ComponentList)
            {
                ComponentRemoved(c);
            }

            foreach (var c in node.ChildList)
            {
                NodeRemoved(c);
            }

            node.ResetScene();

        }

        public void ComponentAdded(Component component)
        {
            if (component == null)
                return;
                 
            component.OnSceneSet(this);

            if(component.Node == this)
            {
                if(component is ISceneAccumulator)
                {
                    accumulator_ = component as ISceneAccumulator;
                    OnAttachAccumutor(accumulator_);
                }
            }
        }

        public void ComponentRemoved(Component component)
        {
            if(component == null)
                return;

            component.OnSceneSet(null);

            if(component.Node == this)
            {
                if(component == accumulator_)
                {
                    OnDetachAccumutor(accumulator_);
                    accumulator_ = null;
                }
            }
        }

        void OnAttachAccumutor(ISceneAccumulator accum)
        {
            foreach(Drawable drawable in drawables_)
            {
                accum.InsertDrawable(drawable);
            }
        }

        void OnDetachAccumutor(ISceneAccumulator accum)
        {
            foreach(Drawable drawable in accum)
            {
                InsertDrawable(drawable);
            }
        }

        public void InsertDrawable(Drawable drawable)
        {
            Debug.Assert(drawable.Index == -1);

            if(accumulator_ != null)
            {
                accumulator_.InsertDrawable(drawable);
            }
            else
            {
                drawables_.Add(drawable);
                drawable.Index = drawables_.Count - 1;
            }
        }

        public void RemoveDrawable(Drawable drawable)
        {
            Debug.Assert(drawable.Index != -1);

            if(accumulator_ != null)
            {
                accumulator_.RemoveDrawable(drawable);
            }
            else
            {
                if(drawables_.Count > 0 && drawable.Index < drawables_.Count - 1)
                {
                    Drawable last = drawables_[drawables_.Count - 1];                
                    drawables_.FastRemove(drawable.Index);
                    last.Index = drawable.Index;               
                }
                else
                {
                    drawables_.FastRemove(drawable.Index);
                }

                drawable.Index = -1;
            }
        }
        
        public IEnumerator<Drawable> GetEnumerator()
        {
            if(accumulator_ != null)
            {
                return accumulator_.GetEnumerator();
            }

            return drawables_.GetEnumerator();
        }

        public void GetDrawables(ISceneQuery query, IList<Drawable> drawables)
        {
            if(accumulator_ != null)
            {
                accumulator_.GetDrawables(query, drawables);
                return;
            }

            foreach(Drawable d in drawables_)
            {
                drawables.Add(d);
            }
        }

        public void Raycast(ref RayQuery query)
        {
            if(accumulator_ != null)
            {
                accumulator_.Raycast(ref query);
                return;
            }

            throw new NotImplementedException();
        }

        public void RaycastSingle(ref RayQuery query)
        {
            if(accumulator_ != null)
            {
                accumulator_.RaycastSingle(ref query);
                return;
            }

            throw new NotImplementedException();
        }

        public void Update()
        {

        }

    }
}
