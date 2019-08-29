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
    public class Scene : Node, IDrawableAccumulator
    {
        protected IDrawableAccumulator accumulator_;
        protected List<Drawable> drawables_ = new List<Drawable>();
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
                if(component is IDrawableAccumulator)
                {
                    accumulator_ = component as IDrawableAccumulator;
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
                //    OnDetachAccumutor(accumulator_);
                    accumulator_ = null;
                }
            }
        }

        void OnAttachAccumutor(IDrawableAccumulator accum)
        {
            foreach(Drawable drawable in drawables_)
            {
                accum.InsertDrawable(drawable);
            }
        }

//         void OnDetachAccumutor(IDrawableAccumulator accum)
//         {
//             foreach(Drawable drawable in accum)
//             {
//                 InsertDrawable(drawable);
//             }
//         }

        public void InsertDrawable(Drawable drawable)
        {
            Debug.Assert(drawable.index == -1);

            drawables_.Add(drawable);

            if (accumulator_ != null)
            {
                accumulator_.InsertDrawable(drawable);
            }
            else
            {
                drawable.index = drawables_.Count - 1;
            }
        }

        public void RemoveDrawable(Drawable drawable)
        {
            Debug.Assert(drawable.index != -1);

            if(accumulator_ != null)
            {
                accumulator_.RemoveDrawable(drawable);
            }
            else
            {
                if(drawables_.Count > 0 && drawable.index < drawables_.Count - 1)
                {
                    Drawable last = drawables_[drawables_.Count - 1];                
                    drawables_.FastRemove(drawable.index);
                    last.index = drawable.index;               
                }
                else
                {
                    drawables_.FastRemove(drawable.index);
                }

                drawable.index = -1;
            }
        }
        
        public void GetDrawables(ISceneQuery query, Action<Drawable> drawables)
        {
            if(accumulator_ != null)
            {
                accumulator_.GetDrawables(query, drawables);
                return;
            }

            foreach(Drawable d in drawables_)
            {
                drawables(d);
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

        public void Update(float timeStep)
        {
            this.SendEvent(new SceneUpdate { scene = this, timeStep = timeStep });

            this.SendEvent(new SceneSubsystemUpdate { scene = this, timeStep = timeStep });

            this.SendEvent(new ScenePostUpdate { scene = this, timeStep = timeStep });
        }

    }
}
