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

    public struct SceneDrawableUpdateFinished
    {
        public Scene scene;
        public float timeStep;
    }

    [DataContract]
    public class Scene : Node
    {
        public ISpacePartitioner SpacePartitioner { get; private set; }

        internal List<Drawable> drawables = new List<Drawable>();

        internal bool threadedUpdate_ = false;
        public bool IsThreadedUpdate => threadedUpdate_;
        List<Component> delayedDirtyComponents_ = new List<Component>();

        public Scene()
        {
            NodeAdded(this);

            this.Subscribe((in Update e) => Update(e.timeDelta));
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
                    SpacePartitioner = component as ISpacePartitioner;
                    OnAttachAccumutor(SpacePartitioner);
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
                if(component == SpacePartitioner)
                {
                    SpacePartitioner = null;
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

            if (SpacePartitioner != null)
            {
                SpacePartitioner.InsertDrawable(drawable);
            }

            drawable.index = drawables.Count - 1;           
        }

        public void RemoveDrawable(Drawable drawable)
        {
            Debug.Assert(drawable.index != -1);

            if(SpacePartitioner != null)
            {
                SpacePartitioner.RemoveDrawable(drawable);
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
        
        public void GetDrawables(OctreeQuery query, Action<Drawable> visitor)
        {
            if(SpacePartitioner != null)
            {
                SpacePartitioner.GetDrawables(query, visitor);
                return;
            }

            foreach(Drawable d in this.drawables)
            {
                visitor(d);
            }
        }

        public void Raycast(ref RayOctreeQuery query)
        {
            if(SpacePartitioner != null)
            {
                SpacePartitioner.Raycast(ref query);
                return;
            }

            throw new NotImplementedException();
        }

        public void RaycastSingle(ref RayOctreeQuery query)
        {
            if(SpacePartitioner != null)
            {
                SpacePartitioner.RaycastSingle(ref query);
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

        public void BeginThreadedUpdate()
        {
            threadedUpdate_ = true;
        }

        public void EndThreadedUpdate()
        {
            if (!threadedUpdate_)
                return;

            threadedUpdate_ = false;

            if (!delayedDirtyComponents_.Empty())
            {
                foreach(var c in delayedDirtyComponents_)
                {
                    c.OnMarkedDirty(c.Node);
                }

                delayedDirtyComponents_.Clear();
            }
        }

        public void DelayedMarkedDirty(Component component)
        {
            delayedDirtyComponents_.Push(component);
        }

        public void RenderUpdate(FrameInfo frameInfo)
        {
            Profiler.BeginSample("UpdateDrawable");
            SpacePartitioner?.Update(frameInfo);
            Profiler.EndSample();

        }

    }
}
