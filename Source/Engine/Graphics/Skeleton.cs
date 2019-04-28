using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    /// %Bone in a skeleton.
    public class Bone : ICloneable
    {
        public const byte BONECOLLISION_NONE = 0x0;
        public const byte BONECOLLISION_SPHERE = 0x1;
        public const byte BONECOLLISION_BOX = 0x2;

        /// Bone name.
        public StringID name_;
        /// Parent bone index.
        public int parentIndex_;
        /// Reset position.
        public Vector3 initialPosition_;
        /// Reset rotation.
        public Quaternion initialRotation_ = Quaternion.Identity;
        /// Reset scale.
        public Vector3 initialScale_ = Vector3.One;
        /// Offset matrix.
        public Matrix offsetMatrix_;
        /// Animation enable flag.
        public bool animated_ = true;
        /// Supported collision types.
        public byte collisionMask_ = 0;
        /// Radius.
        public float radius_ = 0.0f;
        /// Local-space bounding box.
        public BoundingBox boundingBox_;
        /// Scene node.
        public Node node_;

        public object Clone()
        {
            Bone bone = new Bone();
            bone.name_ = name_;
            bone.parentIndex_ = parentIndex_;
            bone.initialPosition_ = initialPosition_;
            bone.initialRotation_ = initialRotation_;
            bone.initialScale_ = initialScale_;
            bone.offsetMatrix_ = offsetMatrix_;
            bone.animated_ = animated_;
            bone.collisionMask_ = collisionMask_;
            bone.radius_ = radius_;
            bone.boundingBox_ = boundingBox_;
            bone.node_ = node_;
            return bone;
        }
    };


    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Matrix3x4
    {
        public Vector4 Column1;
        public Vector4 Column2;
        public Vector4 Column3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Quat
    {
        public float W;
        public float X;
        public float Y;
        public float Z;
    }

    public class Skeleton
    {
        /// Bones.
        Bone[] bones_ = new Bone[0];
        /// Root bone index.
        uint rootBoneIndex_;
        public Bone[] Bones => bones_;
        public int NumBones => bones_.Length;

        public Bone RootBone => GetBone(rootBoneIndex_);

        public Bone GetBone(uint index)
        {
            return index < Bones.Length ? bones_[(int)index] : null;
        }

        public Bone GetBone(StringID name)
        {
            foreach(var bone in bones_)
            {
                if(bone.name_ == name)
                    return bone;
            }

            return null;
        }

        public bool Load(Stream source)
        {
            ClearBones();

            if (source.IsEof())
                return false;

            uint bones = source.Read<uint>();
            Array.Resize(ref bones_, (int)bones);

            for (uint i = 0; i < bones; ++i)
            {
                Bone newBone = new Bone();
                newBone.name_ = source.ReadCString();
                newBone.parentIndex_ = source.Read<int>();
                newBone.initialPosition_ = source.Read<Vector3>();

                Quat r = source.Read<Quat>();
                newBone.initialRotation_ = new Quaternion(r.X, r.Y, r.Z, r.W);
                newBone.initialScale_ = source.Read<Vector3>();

                Matrix3x4 temp = source.Read<Matrix3x4>();
                newBone.offsetMatrix_.Column1 = temp.Column1;
                newBone.offsetMatrix_.Column2 = temp.Column2;
                newBone.offsetMatrix_.Column3 = temp.Column3;
                newBone.offsetMatrix_.Column4 = Vector4.UnitW;

                // Read bone collision data
                newBone.collisionMask_ = source.Read<byte>();
                if ((newBone.collisionMask_ & Bone.BONECOLLISION_SPHERE) != 0)
                    newBone.radius_ = source.Read<float>();
                if ((newBone.collisionMask_ & Bone.BONECOLLISION_BOX) != 0)
                    newBone.boundingBox_ = source.Read<BoundingBox>();

                if (newBone.parentIndex_ == i)
                    rootBoneIndex_ = i;

                bones_[i] = newBone;
            }

            return true;
        }

        public void Define( Skeleton src)
        {
            ClearBones();
            bones_ = (Bone[])src.bones_.Clone();
            Array.Resize(ref bones_, src.bones_.Length);
            for(int i = 0; i < bones_.Length; i++)
            {
                bones_[i] = (Bone)src.bones_[i].Clone();
            }

            rootBoneIndex_ = src.rootBoneIndex_;
        }

        public void SetRootBoneIndex(uint index)
        {
            if (index < bones_.Length)
                rootBoneIndex_ = index;
            else
                Log.Error("Root bone index out of bounds");
        }

        public void ClearBones()
        {
            if(bones_ != null)
            {
                bones_.Clear();
            }

            rootBoneIndex_ = uint.MaxValue;
        }

        public void Reset()
        {
            foreach(var i in bones_)
            {
                if (i.animated_ && i.node_)
                    i.node_.SetTransform(i.initialPosition_, i.initialRotation_, i.initialScale_);
            }
        }

        public void ResetSilent()
        {
            foreach (var i in bones_)
            {
                if (i.animated_ && i.node_)
                    i.node_.SetTransformSilent(i.initialPosition_, i.initialRotation_, i.initialScale_);
            }
        }

        
    }
}
