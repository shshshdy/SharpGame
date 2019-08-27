using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    [DataContract]
    public class Camera : Component
    {
        const float M_LARGE_EPSILON = 0.00005f;
        const float M_MIN_NEARCLIP = 0.01f;
        const float M_MAX_FOV = 160.0f* (MathUtil.Pi / 180.0f);

        const float DEFAULT_NEARCLIP = 0.1f;
        const float DEFAULT_FARCLIP = 1000.0f;
        const float DEFAULT_CAMERA_FOV = 45.0f * (MathUtil.Pi / 180.0f);
        const float DEFAULT_ORTHOSIZE = 20.0f;

        const uint DEFAULT_VIEWMASK = uint.MaxValue;

        [DataMember(Order = 0)]
        public bool Orthographic { get => orthographic_; set => SetOrthographic(value); }

        [DataMember(Order = 1)]
        public float NearClip { get => nearClip_; set => SetNearClip(value); }

        [DataMember(Order = 2)]
        public float FarClip { get => farClip_; set => SetFarClip(value); }

        [DataMember(Order = 3)]
        public float Fov { get => fov_; set => SetFov(value); }

        [DataMember(Order = 4)]
        public float OrthoSize { get => orthoSize_; set => SetOrthoSize(value); }

        [DataMember(Order = 5)]
        public float AspectRatio { get => aspectRatio_; set => SetAspectRatio(value); }

        [DataMember(Order = 6)]
        public float Zoom { get => zoom_; set => SetZoom(value); }

        [DataMember(Order = 7)]
        public bool AutoAspectRatio { get => autoAspectRatio_; set => SetAutoAspectRatio(value); }


        /// Return projection matrix. It's in D3D convention with depth range 0 - 1.
        [IgnoreDataMember]
        public ref Matrix Projection
        {
            get
            {
                if(projectionDirty_)
                {
                    UpdateProjection();
                }

                return ref projection_;
            }
        }

        /// Return view matrix.
        [IgnoreDataMember]
        public ref Matrix View
        {
            get
            {
                if(viewDirty_)
                {
                    if(node_ != null)
                    {
                        // Note: view matrix is unaffected by node or parent scale
                        Vector3 worldPosition = node_.WorldPosition;
                        view_ = Matrix.Transformation(ref worldPosition, ref node_.WorldRotation);
                        view_.Invert();
                        
                    }
                    else
                    {
                        view_ = Matrix.Identity;
                    }

                    viewDirty_ = false;
                }

                return ref view_;
            }
        }

        [IgnoreDataMember]
        public ref BoundingFrustum Frustum
        {
            get
            {
                if(projectionDirty_)
                {
                    UpdateProjection();
                }

                if(frustumDirty_)
                {
                    frustum_.Matrix = View * projection_;
                    frustumDirty_ = false;
                }

                return ref frustum_;
            }

        }

        [IgnoreDataMember]
        public BoundingFrustum ViewSpaceFrustum
        {
            get
            {
                if (projectionDirty_)
                    UpdateProjection();

                return new BoundingFrustum(projection_);
            }

        }

        /// Cached view matrix.
        Matrix view_;
        /// Cached projection matrix.
        Matrix projection_;
        /// Cached world space frustum.
        BoundingFrustum frustum_;

        /// View matrix dirty flag.
        bool viewDirty_ = true;
        /// Projection matrix dirty flag.
        bool projectionDirty_ = true;
        /// Frustum dirty flag.
        bool frustumDirty_ = true;
        /// Orthographic mode flag.
        bool orthographic_ = false;

        /// Near clip distance.
        float nearClip_ = DEFAULT_NEARCLIP;
        /// Far clip distance.
        float farClip_ = DEFAULT_FARCLIP;
        /// Field of view.
        float fov_ = DEFAULT_CAMERA_FOV;
        /// Orthographic view size.
        float orthoSize_ = DEFAULT_ORTHOSIZE;
        /// Aspect ratio.
        float aspectRatio_ = 1.0f;
        /// Zoom.
        float zoom_ = 1.0f;
        /// View mask.
        uint viewMask_ = uint.MaxValue;
        /// LOD bias.
        float lodBias_ = 1.0f;
        /// View override flags.
        uint viewOverrideFlags_;

        bool autoAspectRatio_ = true;

        public void SetNearClip(float nearClip)
        {
            nearClip_ = Math.Max(nearClip, M_MIN_NEARCLIP);
            frustumDirty_ = true;
            projectionDirty_ = true;
        }

        public void SetFarClip(float farClip)
        {
            farClip_ = Math.Max(farClip, M_MIN_NEARCLIP);
            frustumDirty_ = true;
            projectionDirty_ = true;
        }

        public void SetFov(float fov)
        {
            fov_ = MathUtil.Clamp(fov, 0.0f, M_MAX_FOV);
            frustumDirty_ = true;
            projectionDirty_ = true;
        }

        public void SetOrthoSize(float orthoSize)
        {
            orthoSize_ = orthoSize;
            aspectRatio_ = 1.0f;
            frustumDirty_ = true;
            projectionDirty_ = true;
        }

        public void SetOrthoSize(Vector2 orthoSize)
        {
            autoAspectRatio_ = false;
            orthoSize_ = orthoSize.Y;
            aspectRatio_ = orthoSize.X / orthoSize.Y;
            frustumDirty_ = true;
            projectionDirty_ = true;
        }

        public void SetAspectRatio(float aspectRatio)
        {
            autoAspectRatio_ = false;
            SetAspectRatioInternal(aspectRatio);
        }

        internal void SetAspectRatioInternal(float aspectRatio)
        {
            if(aspectRatio != aspectRatio_)
            {
                aspectRatio_ = aspectRatio;
                frustumDirty_ = true;
                projectionDirty_ = true;
            }
        }

        public void SetZoom(float zoom)
        {
            zoom_ = Math.Max(zoom, MathUtil.Epsilon);
            frustumDirty_ = true;
            projectionDirty_ = true;
        }

        public void SetViewMask(uint mask)
        {
            viewMask_ = mask;
        }

        public void SetOrthographic(bool enable)
        {
            orthographic_ = enable;
            frustumDirty_ = true;
            projectionDirty_ = true;
        }

        public void SetAutoAspectRatio(bool enable)
        {
            autoAspectRatio_ = enable;
        }

        public override void OnNodeSet(Node node)
        {
            if(node != null)
                node.AddListener(this);
        }

        public override void OnMarkedDirty(Node node)
        {
            frustumDirty_ = true;
            viewDirty_ = true;
        }

        void UpdateProjection()
        {
            if(orthographic_)
            {
                float h = (1.0f / (orthoSize_ * 0.5f)) * zoom_;
                float w = h / aspectRatio_;
                Matrix.OrthoLH(w, h, nearClip_, farClip_, false, out projection_);
            }
            else
            {
                Matrix.PerspectiveFovLH(fov_, aspectRatio_, nearClip_, farClip_, out projection_);
                projection_.M22 = -projection_.M22;
            }

            projectionDirty_ = false;
        }

        public float GetHalfViewSize()
        {
            if(!orthographic_)
                return (float)Math.Tan(fov_ * 0.5f) / zoom_;
            else
                return orthoSize_ * 0.5f / zoom_;
        }

        public float GetDistance(Vector3 worldPos)
        {
            if(!orthographic_)
            {
                Vector3 cameraPos = node_ ? node_.WorldPosition : Vector3.Zero;
                return (worldPos - cameraPos).Length();
            }
            else
                return Math.Abs(Vector3.Transform(worldPos, View).Z);
        }

        public float GetDistanceSquared(ref Vector3 worldPos)
        {
            if(!orthographic_)
            {
                Vector3 cameraPos = node_ ? node_.WorldPosition : Vector3.Zero;
                return (worldPos - cameraPos).LengthSquared();
            }
            else
            {
                float distance = Vector3.Transform(worldPos, View).Z;
                return distance * distance;
            }
        }

        public float GetLodDistance(float distance, float scale, float bias)
        {
            float d = Math.Max(lodBias_ * bias * scale * zoom_, MathUtil.Epsilon);
            if(!orthographic_)
                return distance / d;
            else
                return orthoSize_ / d;
        }
    }
}
