#define UNMANAGED_MATRIX 


namespace SharpGame
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text;


    public partial class Node
    {
        [DataMember(Order = 8)]
        public vec3 Position
        {
            get
            {
                return position_;
            }

            set
            {
                position_ = value;

                MarkDirty();
            }
        }

        [IgnoreDataMember]
        public ref vec3 PositionRef => ref position_;
        protected vec3 position_ = vec3.Zero;

        [DataMember(Order = 9)]
        public quat Rotation
        {
            get
            {
                return rotation_;
            }

            set
            {
                rotation_ = value;

                MarkDirty();
            }
        }
        [IgnoreDataMember]
        public ref quat RotationRef => ref rotation_;
        protected quat rotation_ = quat.Identity;

        [IgnoreDataMember]
        public vec3 EulerAngles { get => rotation_.EulerAngles; set => rotation_ = glm.quat(value); }

        public void SetDirection(in vec3 direction)
        {
            Rotation = glm.rotation(vec3.Forward, direction);
        }

        [DataMember(Order = 10)]
        public vec3 Scaling
        {
            get
            {
                return scaling_;
            }

            set
            {
                scaling_ = value;

                MarkDirty();
            }
        }
        [IgnoreDataMember]
        public ref vec3 ScalingRef => ref scaling_;
        protected vec3 scaling_ = vec3.One;

        [IgnoreDataMember]
        public Node Parent => parent_;
        protected Node parent_;

        [IgnoreDataMember]
        public Scene Scene { get { return scene_; } set { scene_ = value; } }
        protected Scene scene_;

        [IgnoreDataMember]
        public bool Dirty => dirty_;

        [IgnoreDataMember]
        public mat4 Transform
        {
            get
            {
                return glm.transformation(ref position_, ref rotation_, ref scaling_);
            }
        }

        [IgnoreDataMember]
        public vec3 WorldPosition
        {
            get
            {
                return WorldTransform.TranslationVector;
            }
        }

        [IgnoreDataMember]
        public ref quat WorldRotation
        {
            get
            {
                if(dirty_)
                    UpdateWorldTransform();

                return ref worldRotation_;
            }
        }

        protected quat worldRotation_;
        public void SetWorldRotation(quat rotation)
        {
            Rotation = ((parent_ == scene_ || !parent_) ? rotation :  glm.inverse(parent_.WorldRotation) * rotation);
        }

        [IgnoreDataMember]
        public unsafe ref mat4 WorldTransform
        {
            get
            {
                if(dirty_)
                    UpdateWorldTransform();

#if UNMANAGED_MATRIX
                return ref Unsafe.AsRef<mat4>((void*)worldTransform_);
#else
                return ref worldTransform_;
#endif
            }
        }

#if UNMANAGED_MATRIX
        [IgnoreDataMember]
        public IntPtr worldTransform_ = NativePool<mat4>.Shared.Acquire();
#else
        [IgnoreDataMember]
        public Matrix worldTransform_;
#endif
        public void SetTransform(vec3 position, quat rotation)
        {
            position_ = position;
            rotation_ = rotation;
            MarkDirty();
        }

        public void SetTransform(vec3 position, quat rotation, float scale)
        {
            SetTransform(position, rotation, new vec3(scale, scale, scale));
        }

        public void SetTransform(vec3 position, quat rotation, vec3 scale)
        {
            position_ = position;
            rotation_ = rotation;
            scaling_ = scale;
            MarkDirty();
        }
        
        public void SetTransformSilent(vec3 position, quat rotation, vec3 scale)
        {
            position_ = position;
            rotation_ = rotation;
            scaling_ = scale;
        }

        public void SetTransform(mat4 matrix)
        {
            vec3 v1 = vec3.Zero;
            vec4 v2 = vec4.Zero;
            glm.decompose(ref matrix, ref scaling_, ref rotation_, ref position_, ref v1, ref v2);
            MarkDirty();
        }

        public void SetWorldPosition(vec3 position)
        {
            Position = ((parent_ == scene_ || parent_ == null) ? position : parent_.WorldToLocal(position));
        }

        public void SetWorldDirection(vec3 direction)
        {
            vec3 localDirection = (parent_ == scene_ || parent_ == null) ? direction : glm.inverse(parent_.WorldRotation) * direction;
            Rotation = glm.rotation(vec3.Forward, localDirection);
        }

        public void SetWorldScale(float scale)
        {
            SetWorldScale(new vec3(scale, scale, scale));
        }

        public void SetWorldScale(vec3 scale)
        {
            //Scaling = ((parent_ == scene_ || !parent_) ? scale : scale / parent_.WorldScaling);
        }

        public void SetWorldTransform(vec3 position, quat rotation)
        {
            SetWorldPosition(position);
            SetWorldRotation(rotation);
        }

        public void SetWorldTransform(vec3 position, quat rotation, float scale)
        {
            SetWorldPosition(position);
            SetWorldRotation(rotation);
            SetWorldScale(scale);
        }

        public void SetWorldTransform(vec3 position, quat rotation, vec3 scale)
        {
            SetWorldPosition(position);
            SetWorldRotation(rotation);
            SetWorldScale(scale);
        }

        public void Translate(vec3 delta, TransformSpace space = TransformSpace.LOCAL)
        {
            switch(space)
            {
                case TransformSpace.LOCAL:
                    // Note: local space translation disregards local scale for scale-independent movement speed
                    position_ += (rotation_ * delta);
                    break;

                case TransformSpace.PARENT:
                    position_ += delta;
                    break;

                case TransformSpace.WORLD:
                    position_ += (parent_ == scene_ || parent_ == null) ? delta : parent_.WorldToLocal(delta);
                    break;
            }

            MarkDirty();

        }

        public void Rotate(quat delta, TransformSpace space = TransformSpace.LOCAL)
        {
            switch(space)
            {
                case TransformSpace.LOCAL:
                    rotation_ = (  rotation_ * delta);
                    rotation_ = glm.normalize(rotation_);
                    break;

                case TransformSpace.PARENT:
                    rotation_ = (rotation_ * delta);
                    rotation_ = glm.normalize(rotation_);
                    break;

                case TransformSpace.WORLD:
                    if(parent_ == scene_ || parent_ == null)
                    {
                        rotation_ = (delta * rotation_);
                        rotation_ = glm.normalize(rotation_);
                    }
                    else
                    {
                        quat worldRotation = WorldRotation;
                        rotation_ = rotation_ * glm.inverse(worldRotation) * delta * worldRotation;
                    }
                    break;
            }

            MarkDirty();

        }

        public void RotateAround(vec3 point, quat delta, TransformSpace space = TransformSpace.LOCAL)
        {
            vec3 parentSpacePoint = vec3.Zero;
            quat oldRotation = rotation_;

            switch (space)
            {
                case TransformSpace.LOCAL:
                    parentSpacePoint = Transform * point;
                    rotation_ = glm.normalize(rotation_ * delta);
                    break;

                case TransformSpace.PARENT:
                    parentSpacePoint = point;
                    rotation_ = glm.normalize(delta * rotation_);
                    break;

                case TransformSpace.WORLD:
                    if (parent_ == scene_ || !parent_)
                    {
                        parentSpacePoint = point;
                        rotation_ = glm.normalize(delta * rotation_);
                    }
                    else
                    {
                        parentSpacePoint = glm.inverse(parent_.WorldTransform) * point;
                        quat worldRotation = WorldRotation;
                        rotation_ = rotation_ * glm.inverse(worldRotation) * delta * worldRotation;
                    }
                    break;
            }

            vec3 oldRelativePos = glm.inverse(oldRotation) * (position_ - parentSpacePoint);
            position_ = rotation_ * oldRelativePos + parentSpacePoint;

            MarkDirty();

        }


        public void Yaw(float angle, TransformSpace space)
        {
            Rotate(new quat(angle, vec3.Up), space);
        }

        public void Pitch(float angle, TransformSpace space)
        {
            Rotate(new quat(angle, vec3.Right), space);
        }

        public void Roll(float angle, TransformSpace space)
        {
            Rotate(new quat(angle, vec3.Forward), space);
        }

        public bool LookAt(vec3 target) => LookAt(target, TransformSpace.LOCAL);
        public bool LookAt(vec3 target, TransformSpace space) => LookAt(target, vec3.Up, space);
        public bool LookAt(vec3 target, vec3 up, TransformSpace space)
        {
            vec3 worldSpaceTarget = vec3.Zero;

            switch(space)
            {
                case TransformSpace.LOCAL:
                    worldSpaceTarget = WorldTransform * target;
                    break;

                case TransformSpace.PARENT:
                    worldSpaceTarget = (parent_ == scene_ || parent_ == null) ? target : parent_.LocalToWorld(target);
                    break;

                case TransformSpace.WORLD:
                    worldSpaceTarget = target;
                    break;
            }

            vec3 lookDir = worldSpaceTarget - WorldPosition;

            // Check if target is very close, in that case can not reliably calculate lookat direction
            if(lookDir.Equals(vec3.Zero))
                return false;

            quat newRotation = glm.quatLookDirection(lookDir, up);

            //if (!newRotation.FromLookRotation(lookDir, up))
            //    return false;
            //= glm.quatLookDirection(lookDir, up);           

            SetWorldRotation(newRotation);
            return true;
        }

        public void Scale(float scale)
        {
            Scale(new vec3(scale, scale, scale));
        }

        public void Scale(vec3 scale)
        {
            scaling_ *= scale;
            MarkDirty();
        }

        public vec3 LocalToWorld(vec3 position)
        {
            return WorldTransform* position;
        }

        public vec3 WorldToLocal(vec3 position)
        {
            mat4 mat = glm.inverse(WorldTransform);
            return mat * position;
        }

        public void MarkDirty()
        {
            Node cur = this;
            for(; ; )
            {
                if(cur.dirty_)
                    return;

                cur.dirty_ = true;

                if(listeners_ != null)
                {
                    // Notify listener components first, then mark child nodes
                    foreach(Component c in listeners_)
                    {
                        c.OnMarkedDirty(cur);
                    }
                }

                var i = cur.children_.GetEnumerator();
                if(i.MoveNext())
                {
                    Node next = i.Current;
                    while(i.MoveNext())
                        i.Current.MarkDirty();

                    cur = next;
                }
                else
                    return;
            }
        }

        unsafe void UpdateWorldTransform()
        {
            mat4 xform = Transform;

            // Assume the root node (scene) has identity transform
            if(parent_ == scene_ || parent_ == null)
            {
#if UNMANAGED_MATRIX
                Unsafe.Write((void*)worldTransform_, xform);
#else
                worldTransform_ = xform;
#endif
                worldRotation_ = rotation_;
            }
            else
            {
#if UNMANAGED_MATRIX
                //Matrix m = xform * parent_.WorldTransform;
                //Unsafe.Write((void*)worldTransform_, m);
                mat4.Multiply(ref parent_.WorldTransform, ref xform, out Unsafe.AsRef<mat4>((void*)worldTransform_));               

#else
                worldTransform_ =  parent_.WorldTransform * xform;
#endif
                worldRotation_ =  parent_.WorldRotation *rotation_;
            }

            dirty_ = false;
        }



    }
}
