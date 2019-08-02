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
        public Vector3 Position
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
        public ref Vector3 PositionRef => ref position_;
        protected Vector3 position_ = Vector3.Zero;

        [DataMember(Order = 9)]
        public Quaternion Rotation
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
        public ref Quaternion RotationRef => ref rotation_;
        protected Quaternion rotation_ = Quaternion.Identity;

        [DataMember(Order = 10)]
        public Vector3 Scaling
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
        public ref Vector3 ScalingRef => ref scaling_;
        protected Vector3 scaling_ = Vector3.One;

        [IgnoreDataMember]
        public Node Parent => parent_;
        protected Node parent_;

        [IgnoreDataMember]
        public Scene Scene { get { return scene_; } set { scene_ = value; } }
        protected Scene scene_;

        [IgnoreDataMember]
        public bool Dirty => dirty_;

        [IgnoreDataMember]
        public Matrix Transform
        {
            get
            {
                return Matrix.Transformation(ref position_, ref rotation_, ref scaling_);
            }
        }

        [IgnoreDataMember]
        public Vector3 WorldPosition
        {
            get
            {
                return WorldTransform.TranslationVector;
            }
        }

        [IgnoreDataMember]
        public ref Quaternion WorldRotation
        {
            get
            {
                if(dirty_)
                    UpdateWorldTransform();

                return ref worldRotation_;
            }
        }
        protected Quaternion worldRotation_;

        [IgnoreDataMember]
        public unsafe ref Matrix WorldTransform
        {
            get
            {
                if(dirty_)
                    UpdateWorldTransform();

#if UNMANAGED_MATRIX
                return ref Unsafe.AsRef<Matrix>((void*)worldTransform_);
#else
                return ref worldTransform_;
#endif
            }
        }

#if UNMANAGED_MATRIX
        [IgnoreDataMember]
        public IntPtr worldTransform_ = NativePool<Matrix>.Shared.Acquire();
#else
        [IgnoreDataMember]
        public Matrix worldTransform_;
#endif
        public void SetTransform(Vector3 position, Quaternion rotation)
        {
            position_ = position;
            rotation_ = rotation;
            MarkDirty();
        }

        public void SetTransform(Vector3 position, Quaternion rotation, float scale)
        {
            SetTransform(position, rotation, new Vector3(scale, scale, scale));
        }

        public void SetTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            position_ = position;
            rotation_ = rotation;
            scaling_ = scale;
            MarkDirty();
        }
        
        public void SetTransformSilent(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            position_ = position;
            rotation_ = rotation;
            scaling_ = scale;
        }

        public void SetTransform(Matrix matrix)
        {
            matrix.Decompose(out scaling_, out rotation_, out position_);
            MarkDirty();
        }

        void SetWorldPosition(Vector3 position)
        {
            Position = ((parent_ == scene_ || parent_ == null) ? position : parent_.WorldToLocal(position));
        }

        void SetWorldRotation(Quaternion rotation)
        {/*
            Rotation = ((parent_ == scene_ || parent_ == null) ? rotation : 
                    parent_.WorldRotation.Inverse() * rotation);*/
        }

        void SetWorldDirection(Vector3 direction)
        {/*
        Vector3 localDirection = (parent_ == scene_ || parent_ == null) ? direction : parent_.WorldRotation.Inverse() * direction;
        SetRotation(new Quaternion(Vector3.ForwardLH, localDirection));*/
        }

        void SetWorldScale(float scale)
        {
            SetWorldScale(new Vector3(scale, scale, scale));
        }

        void SetWorldScale(Vector3 scale)
        {
            //   SetScale((parent_ == scene_ || !parent_) ? scale : scale / parent_.WorldScale);
        }

        void SetWorldTransform(Vector3 position, Quaternion rotation)
        {
            SetWorldPosition(position);
            SetWorldRotation(rotation);
        }

        void SetWorldTransform(Vector3 position, Quaternion rotation, float scale)
        {
            SetWorldPosition(position);
            SetWorldRotation(rotation);
            SetWorldScale(scale);
        }

        void SetWorldTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SetWorldPosition(position);
            SetWorldRotation(rotation);
            SetWorldScale(scale);
        }

        public void Translate(Vector3 delta, TransformSpace space)
        {
            switch(space)
            {
                case TransformSpace.LOCAL:
                    // Note: local space translation disregards local scale for scale-independent movement speed
                    position_ += Vector3.Transform(delta, rotation_);
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

        public void Rotate(Quaternion delta, TransformSpace space)
        {
            switch(space)
            {
                case TransformSpace.LOCAL:
                    rotation_ = (delta * rotation_);
                    rotation_.Normalize();
                    break;

                case TransformSpace.PARENT:
                    rotation_ = (rotation_ * delta);
                    rotation_.Normalize();
                    break;

                case TransformSpace.WORLD:
                    if(parent_ == scene_ || parent_ == null)
                    {
                        rotation_ = (rotation_ * delta);
                        rotation_.Normalize();
                    }
                    else
                    {
                        //    Quaternion worldRotation = WorldRotation;
                        //    rotation_ = rotation_ * worldRotation.Inverse() * delta * worldRotation;
                    }
                    break;
            }

            MarkDirty();

        }

        void RotateAround(Vector3 point, Quaternion delta, TransformSpace space)
        {
            Vector3 parentSpacePoint;
            Quaternion oldRotation = rotation_;
#if false
    switch (space)
    {
        case TransformSpace.LOCAL:
            parentSpacePoint = GetTransform() * point;
            rotation_ = (rotation_ * delta).Normalized();
            break;

        case TransformSpace.PARENT:
            parentSpacePoint = point;
            rotation_ = (delta * rotation_).Normalized();
            break;

        case TransformSpace.WORLD:
            if (parent_ == scene_ || !parent_)
            {
                parentSpacePoint = point;
                rotation_ = (delta * rotation_).Normalized();
            }
            else
            {
                parentSpacePoint = parent_->GetWorldTransform().Inverse() * point;
                Quaternion worldRotation = GetWorldRotation();
                rotation_ = rotation_ * worldRotation.Inverse() * delta * worldRotation;
            }
            break;
    }

    Vector3 oldRelativePos = oldRotation.Inverse() * (position_ - parentSpacePoint);
    position_ = rotation_ * oldRelativePos + parentSpacePoint;

#endif
            MarkDirty();

        }


        public void Yaw(float angle, TransformSpace space)
        {
            Rotate(new Quaternion(angle, Vector3.Up), space);
        }

        public void Pitch(float angle, TransformSpace space)
        {
            Rotate(new Quaternion(angle, Vector3.Right), space);
        }

        public void Roll(float angle, TransformSpace space)
        {
            Rotate(new Quaternion(angle, Vector3.ForwardLH), space);
        }

        public bool LookAt(Vector3 target) => LookAt(target, TransformSpace.LOCAL);
        public bool LookAt(Vector3 target, TransformSpace space) => LookAt(target, Vector3.Up, space);
        public bool LookAt(Vector3 target, Vector3 up, TransformSpace space)
        {
            Vector3 worldSpaceTarget = Vector3.Zero;

            switch(space)
            {
                case TransformSpace.LOCAL:
                    Vector3.Transform(ref target, ref WorldTransform, out worldSpaceTarget);
                    break;

                case TransformSpace.PARENT:
                    worldSpaceTarget = (parent_ == scene_ || parent_ == null) ? target : parent_.LocalToWorld(target);
                    break;

                case TransformSpace.WORLD:
                    worldSpaceTarget = target;
                    break;
            }

            Vector3 lookDir = worldSpaceTarget - WorldPosition;
            // Check if target is very close, in that case can not reliably calculate lookat direction
            if(lookDir.Equals(Vector3.Zero))
                return false;

            Quaternion newRotation = Quaternion.LookAtLH(WorldPosition, worldSpaceTarget, up);
            // Do nothing if setting look rotation failed
            //if (!newRotation.FromLookRotation(lookDir, up))
            //    return false;

            WorldRotation = newRotation;
            return true;
        }

        public void Scale(float scale)
        {
            Scale(new Vector3(scale, scale, scale));
        }

        public void Scale(Vector3 scale)
        {
            scaling_ *= scale;
            MarkDirty();
        }

        public Vector3 LocalToWorld(Vector3 position)
        {
            Vector3.Transform(ref position, ref WorldTransform, out Vector3 ret);
            return ret;
        }

        public Vector3 LocalToWorld(Vector4 vector)
        {
            Vector4.Transform(ref vector, ref WorldTransform, out Vector4 ret);
            return (Vector3)ret;
        }

        public Vector3 WorldToLocal(Vector3 position)
        {
            Matrix mat = Matrix.Invert(WorldTransform);
            Vector3.Transform(ref position, ref mat, out Vector3 ret);
            return ret;
        }

        public Vector3 WorldToLocal(Vector4 vector)
        {
            Matrix mat = Matrix.Invert(WorldTransform);
            Vector4.Transform(ref vector, ref mat, out Vector4 ret);
            return (Vector3)ret;
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
            Matrix xform = Transform;

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
                Matrix.Multiply(ref xform, ref parent_.WorldTransform, out Unsafe.AsRef<Matrix>((void*)worldTransform_));               

#else
                worldTransform_ = xform * parent_.WorldTransform;
#endif
                worldRotation_ = rotation_ * parent_.WorldRotation;
            }

            dirty_ = false;
        }



    }
}
