using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public struct SceneUpdate
    {
        public Scene scene;
        public float timeStep;
    }

    public struct ScenePostUpdate
    {
        public Scene scene;
        public float timeStep;
    }

    public struct SceneSubsystemUpdate
    {
        public Scene scene;
        public float timeStep;
    }

    [DataContract]
    public class Scene : Node, ISpacePartitioner
    {
        protected ISpacePartitioner spacePartitioner;

        internal List<Drawable> drawables = new List<Drawable>();

        internal bool threadedUpdate_ = false;
        public bool IsThreadedUpdate => threadedUpdate_;

        public Scene()
        {
            NodeAdded(this);

            this.Subscribe<Update>(e => Update(e.timeDelta));
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
            
            foreach (var c in node.ComponentList)
            {
                ComponentAdded(c);
            }

            foreach (var c in node.Children)
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

            foreach (var c in node.ComponentList)
            {
                ComponentRemoved(c);
            }

            foreach (var c in node.Children)
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
                if(component is ISpacePartitioner)
                {
                    spacePartitioner = component as ISpacePartitioner;
                    OnAttachAccumutor(spacePartitioner);
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
                if(component == spacePartitioner)
                {
                    spacePartitioner = null;
                }
            }
        }

        void OnAttachAccumutor(ISpacePartitioner accum)
        {
            foreach(Drawable drawable in drawables)
            {
                accum.InsertDrawable(drawable);
            }
        }
        
        public void InsertDrawable(Drawable drawable)
        {
            Debug.Assert(drawable.index == -1);

            drawables.Add(drawable);

            if (spacePartitioner != null)
            {
                spacePartitioner.InsertDrawable(drawable);
            }

            drawable.index = drawables.Count - 1;           
        }

        public void RemoveDrawable(Drawable drawable)
        {
            Debug.Assert(drawable.index != -1);

            if(spacePartitioner != null)
            {
                spacePartitioner.RemoveDrawable(drawable);
            }

            if (drawables.Count > 0 && drawable.index < drawables.Count - 1)
            {
                Drawable last = drawables[drawables.Count - 1];
                drawables.FastRemove(drawable.index);
                last.index = drawable.index;
            }
            else
            {
                drawables.FastRemove(drawable.index);
            }

            drawable.index = -1;

        }
        
        public void GetDrawables(OctreeQuery query, Action<Drawable> drawables)
        {
            if(spacePartitioner != null)
            {
                spacePartitioner.GetDrawables(query, drawables);
                return;
            }

            foreach(Drawable d in this.drawables)
            {
                drawables(d);
            }
        }

        public void Raycast(ref RayOctreeQuery query)
        {
            if(spacePartitioner != null)
            {
                spacePartitioner.Raycast(ref query);
                return;
            }

            throw new NotImplementedException();
        }

        public void RaycastSingle(ref RayOctreeQuery query)
        {
            if(spacePartitioner != null)
            {
                spacePartitioner.RaycastSingle(ref query);
                return;
            }

            throw new NotImplementedException();
        }

        public void Update(float timeStep)
        {
            SendEvent(new SceneUpdate { scene = this, timeStep = timeStep });

            SendEvent(new SceneSubsystemUpdate { scene = this, timeStep = timeStep });

            SendEvent(new ScenePostUpdate { scene = this, timeStep = timeStep });
        }

    }
}
