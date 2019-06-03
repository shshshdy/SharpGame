using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    public unsafe class AnimatedModel : StaticModel
    {
        int MAX_ANIMATION_STATES = 256;
        const float ANIMATION_LOD_BASESCALE = 2500.0f;

        /// Skeleton.
        [IgnoreDataMember]
        public Skeleton Skeleton => skeleton_;
        Skeleton skeleton_ = new Skeleton();

        /// Morph vertex buffers.
        DeviceBuffer[] morphVertexBuffers_;

        /// Vertex morphs.
        ModelMorph[] morphs_;

        /// Animation states.
        [DataMember]
        public List<AnimationState> AnimationStates
        {
            get => animationStates_;
            set
            {
                RemoveAllAnimationStates();
                animationStates_ = value;

                if(animationStates_.Count > 0)
                {
                    MarkAnimationDirty();
                    MarkAnimationOrderDirty();
                }
            }
        }
        List<AnimationState> animationStates_ = new List<AnimationState>();

        /// Skinning matrices.
        Span<Matrix> skinMatrices_;
        /// Mapping of subgeometry bone indices, used if more bones than skinning shader can manage.
        int[][] geometryBoneMappings_;
        /// Subgeometry skinning matrices, used if more bones than skinning shader can manage.
        Span<Matrix>[] geometrySkinMatrices_ = Array.Empty<Span<Matrix>>();// new Span<Matrix>[0];
        /// Subgeometry skinning matrix pointers, if more bones than skinning shader can manage.
        IntPtr[][] geometrySkinMatrixPtrs_;
        /// Bounding box calculated from bones.
        BoundingBox boneBoundingBox_;
        /// Attribute buffer.
        //mutable VectorBuffer attrBuffer_;
        /// The frame number animation LOD distance was last calculated on.
        int animationLodFrameNumber_ = 0;
        /// Morph vertex element mask.
        uint morphElementMask_ = 0;
        /// Animation LOD bias.
        float animationLodBias_ = 1.0f;
        /// Animation LOD timer.
        float animationLodTimer_ = -1.0f;
        /// Animation LOD distance, the minimum of all LOD view distances last frame.
        float animationLodDistance_ = 0.0f;
        /// Update animation when invisible flag.
        bool updateInvisible_ = false;
        /// Animation dirty flag.
        bool animationDirty_ = false;
        /// Animation order dirty flag.
        bool animationOrderDirty_ = false;
        /// Vertex morphs dirty flag.
        bool morphsDirty_ = false;
        /// Skinning dirty flag.
        bool skinningDirty_ = true;
        /// Bone bounding box dirty flag.
        bool boneBoundingBoxDirty_ = true;
        /// Master model flag.
        bool isMaster_ = true;
        /// Loading flag. During loading bone nodes are not created, as they will be serialized as child nodes.
        bool loading_ = false;
        /// Bone nodes assignment pending flag.
        bool assignBonesPending_ = false;
        /// Force animation update after becoming visible flag.
        bool forceAnimationUpdate_ = false;

        protected override void Destroy()
        {
            base.Destroy();

            if(!skinMatrices_.IsEmpty)
            {
                UnmanagedPool<Matrix>.Shared.Release(skinMatrices_);
            }

            foreach(Span<Matrix> m in geometrySkinMatrices_)
            {
                UnmanagedPool<Matrix>.Shared.Release(m);
            }

            geometrySkinMatrices_.Clear();
        }

        public override void Update(ref FrameInfo frame)
        {
            // If node was invisible last frame, need to decide animation LOD distance here
            // If headless, retain the current animation distance (should be 0)
            if(frame.camera && Math.Abs((int)frame.frameNumber - (int)viewFrameNumber_) > 1)
            {
                // First check for no update at all when invisible. In that case reset LOD timer to ensure update
                // next time the model is in view
                if(!updateInvisible_)
                {
                    if(animationDirty_)
                    {
                        animationLodTimer_ = -1.0f;
                        forceAnimationUpdate_ = true;
                    }
                    return;
                }
                float distance = frame.camera.GetDistance(node_.WorldPosition);
                // If distance is greater than draw distance, no need to update at all
                if(drawDistance_ > 0.0f && distance > drawDistance_)
                    return;
                float scale = Vector3.Dot(WorldBoundingBox.Size, MathUtil.DotScale);
                animationLodDistance_ = frame.camera.GetLodDistance(distance, scale, lodBias_);
            }

            if(animationDirty_ || animationOrderDirty_)
                UpdateAnimation(ref frame);
            else if(boneBoundingBoxDirty_)
                UpdateBoneBoundingBox();
        }

        public override void UpdateBatches(ref FrameInfo frame)
        {
            ref Matrix worldTransform = ref node_.WorldTransform;
            ref BoundingBox worldBoundingBox = ref WorldBoundingBox;
            distance_ = frame.camera.GetDistance(worldBoundingBox.Center);

            // Note: per-geometry distances do not take skinning into account. Especially in case of a ragdoll they may be
            // much off base if the node's own transform is not updated
            if(batches.Length == 1)
                batches[0].distance = distance_;
            else
            {
                for(int i = 0; i < batches.Length; ++i)
                {
                    Vector3.Transform(ref geometryData_[i].center_, ref worldTransform, out Vector3 worldCenter);
                    batches[i].distance = frame.camera.GetDistance(worldCenter);
                }
            }

            // Use a transformed version of the model's bounding box instead of world bounding box for LOD scale
            // determination so that animation does not change the scale
            BoundingBox transformedBoundingBox = boundingBox_.Transformed(ref worldTransform);
            float scale = Vector3.Dot(transformedBoundingBox.Size, MathUtil.DotScale);
            float newLodDistance = frame.camera.GetLodDistance(distance_, scale, lodBias_);

            // If model is rendered from several views, use the minimum LOD distance for animation LOD
            if(frame.frameNumber != animationLodFrameNumber_)
            {
                animationLodDistance_ = newLodDistance;
                animationLodFrameNumber_ = frame.frameNumber;
            }
            else
                animationLodDistance_ = Math.Min(animationLodDistance_, newLodDistance);

            if(newLodDistance != lodDistance_)
            {
                lodDistance_ = newLodDistance;
                CalculateLodLevels();
            }
        }

        public override void UpdateGeometry(ref FrameInfo frame)
        {
            base.UpdateGeometry(ref frame);

            // Late update in case the model came into view and animation was dirtied in the meanwhile
            if(forceAnimationUpdate_)
            {
                UpdateAnimation(ref frame);
                forceAnimationUpdate_ = false;
            }
            
            if(skinningDirty_)
                UpdateSkinning();
        }
        /*
        public override void DrawDebugGeometry(DebugRenderer debug, bool depthTest)
        {
            if(debug && IsEnabledEffective())
            {
                debug.AddBoundingBox(ref WorldBoundingBox, Color.Green, depthTest);
                debug.AddSkeleton(skeleton_, new Color(0.75f, 0.75f, 0.75f), depthTest);
            }
        }*/

        public void SetModel(Model model, bool createBones = true)
        {
            if(model == model_)
                return;

            if(!node_)
            {
                Log.Error("Can not set model while model component is not attached to a scene node");
                return;
            }

            // Unsubscribe from the reload event of previous model (if any), then subscribe to the new
            //if(model_)
            //    UnsubscribeFromEvent(model_, E_RELOADFINISHED);

            model_ = model;

            if(model)
            {
                //   SubscribeToEvent(model, E_RELOADFINISHED, URHO3D_HANDLER(AnimatedModel, HandleModelReloadFinished));

                // Copy the subgeometry & LOD level structure
                SetNumGeometries(model.NumGeometries);

                Geometry[][] geometries = model.Geometries;
                List<Vector3> geometryCenters = model.GeometryCenters;

                for(int i = 0; i < geometries_.Length; ++i)
                {
                    geometries_[i] = (Geometry[])geometries[i].Clone();
                    geometryData_[i].center_ = geometryCenters[i];
                }

                // Copy geometry bone mappings
                List<int[]> geometryBoneMappings = model.GeometryBoneMappings;
                Array.Resize(ref geometryBoneMappings_, geometryBoneMappings.Count);
                for(int i = 0; i < geometryBoneMappings.Count; ++i)
                    geometryBoneMappings_[i] = (int[])geometryBoneMappings[i].Clone();

                // Copy morphs. Note: morph vertex buffers will be created later on-demand
                morphVertexBuffers_.Clear();
                //morphs_.Clear();

                ModelMorph[] morphs = new ModelMorph[0];// model.Morphs;
                morphElementMask_ = 0;
                Array.Resize(ref morphs_, morphs.Length);
                for(int i = 0; i < morphs.Length; ++i)
                {
                    ref ModelMorph newMorph = ref morphs_[i];
                    newMorph.name_ = morphs[i].name_;
                    newMorph.weight_ = 0.0f;
                    newMorph.buffers_ = morphs[i].buffers_;
                    foreach(var j in morphs[i].buffers_)
                        morphElementMask_ |= j.Value.elementMask_;
                    morphs_[i] = newMorph;
                }

                // Copy bounding box & skeleton
                SetBoundingBox(model.BoundingBox);
                // Initial bone bounding box is just the one stored in the model
                boneBoundingBox_ = boundingBox_;
                boneBoundingBoxDirty_ = true;
                SetSkeleton(model.Skeleton, createBones);
                ResetLodLevels();

                // Reserve space for skinning matrices
                if(!skinMatrices_.IsEmpty)
                {
                    UnmanagedPool<Matrix>.Shared.Release(skinMatrices_);
                }

                int numSkinMatrices = skeleton_.NumBones;
                skinMatrices_ = UnmanagedPool<Matrix>.Shared.Acquire(numSkinMatrices);
                SetGeometryBoneMappings();

                // Enable skinning in batches
                for(int i = 0; i < batches.Length; ++i)
                {
                    if(numSkinMatrices > 0)
                    {
                        batches[i].geometryType = GeometryType.Skinned;
                        // Check if model has per-geometry bone mappings
                        if(geometrySkinMatrices_.Length > 0 && geometrySkinMatrices_[i].Length > 0)
                        {
                            batches[i].worldTransform = (IntPtr)Unsafe.AsPointer(ref geometrySkinMatrices_[i][0]);
                            batches[i].numWorldTransforms = geometrySkinMatrices_[i].Length;
                        }
                        // If not, use the global skin matrices
                        else
                        {
                            batches[i].worldTransform = (IntPtr)Unsafe.AsPointer(ref skinMatrices_[0]);
                            batches[i].numWorldTransforms = numSkinMatrices;
                        }
                    }
                    else
                    {
                        batches[i].geometryType = GeometryType.Static;
                        batches[i].worldTransform = node_.worldTransform_;
                        batches[i].numWorldTransforms = 1;
                    }
                }
            }
            else
            {
                RemoveRootBone(); // Remove existing root bone if any
                SetNumGeometries(0);
                geometryBoneMappings_.Clear();
                morphVertexBuffers_.Clear();
                morphs_.Clear();
                morphElementMask_ = 0;
                SetBoundingBox(BoundingBox.Empty);
                SetSkeleton(default, false);
            }

        }

        public AnimationState AddAnimationState(Animation animation)
        {
            if(!isMaster_)
            {
                Log.Error("Can not add animation state to non-master model");
                return null;
            }

            if(!animation || 0 == skeleton_.NumBones)
                return null;

            // Check for not adding twice
            AnimationState existing = GetAnimationState(animation);
            if(existing != null)
                return existing;

            var newState = new AnimationState(this, animation);
            animationStates_.Add(newState);
            MarkAnimationOrderDirty();
            return newState;
        }

        public void RemoveAnimationState(Animation animation)
        {
            if(animation)
                RemoveAnimationState(animation.AnimationName);
            else
            {
                for(int i = 0; i < animationStates_.Count; i++)
                {
                    AnimationState state = animationStates_[i];
                    if(state.Animation == null)
                    {
                        animationStates_.RemoveAt(i);
                        MarkAnimationDirty();
                        return;
                    }
                }
            }
        }

        public void RemoveAnimationState(StringID animationNameHash)
        {
            for(int i = 0; i < animationStates_.Count; i++)
            {
                AnimationState state = animationStates_[i];
                Animation animation = state.Animation;
                if(animation)
                {
                    // Check both the animation and the resource name
                    if(animation.FileName == animationNameHash || animation.AnimationName == animationNameHash)
                    {
                        animationStates_.RemoveAt(i);
                        MarkAnimationDirty();
                        return;
                    }
                }
            }
        }

        public void RemoveAnimationState(AnimationState state)
        {
            for(int i = 0; i < animationStates_.Count; i++)
            {
                if(animationStates_[i] == state)
                {
                    animationStates_.RemoveAt(i);
                    MarkAnimationDirty();
                    return;
                }
            }
        }

        public void RemoveAnimationState(int index)
        {
            if(index < animationStates_.Count)
            {
                animationStates_.RemoveAt(index);
                MarkAnimationDirty();
            }
        }

        public void RemoveAllAnimationStates()
        {
            if(animationStates_.Count > 0)
            {
                animationStates_.Clear();
                MarkAnimationDirty();
            }
        }

        public void SetAnimationLodBias(float bias)
        {
            animationLodBias_ = Math.Max(bias, 0.0f);
        }

        public void SetUpdateInvisible(bool enable)
        {
            updateInvisible_ = enable;
        }
        
        void ResetMorphWeights()
        {
            for(int i = 0; i < morphs_.Length; i++)
                morphs_[i].weight_ = 0.0f;

            // For a master model, reset weights on non-master models
            if(isMaster_)
            {
                List<AnimatedModel> models = new List<AnimatedModel>();
                GetComponents(models);

                for(int i = 1; i < models.Count; ++i)
                {
                    if(!models[i].isMaster_)
                        models[i].ResetMorphWeights();
                }
            }

        }

        public float GetMorphWeight(int index)
        {
            return index < morphs_.Length ? morphs_[index].weight_ : 0.0f;
        }

        public float GetMorphWeight(StringID nameHash)
        {
            foreach(var i in morphs_)
            {
                if(i.name_ == nameHash)
                    return i.weight_;
            }

            return 0.0f;
        }

        public AnimationState GetAnimationState(Animation animation)
        {
            foreach(var i in animationStates_)
            {
                if(i.Animation == animation)
                    return i;
            }

            return null;
        }

        public AnimationState GetAnimationState(StringID animationNameHash)
        {
            foreach(var i in animationStates_)
            {
                Animation animation = i.Animation;
                if(animation)
                {
                    // Check both the animation and the resource name
                    if(animation.FileName == animationNameHash || animation.AnimationName == animationNameHash)
                        return i;
                }
            }

            return null;
        }

        public AnimationState GetAnimationState(int index)
        {
            return index < animationStates_.Count ? animationStates_[index] : null;
        }

        void SetSkeleton(Skeleton skeleton, bool createBones)
        {
            if(!node_ && createBones)
            {
                Log.Error("AnimatedModel not attached to a scene node, can not create bone nodes");
                return;
            }

            if(isMaster_)
            {
                // Check if bone structure has stayed compatible (reloading the model.) In that case retain the old bones and animations
                if(skeleton_.NumBones == skeleton.NumBones)
                {
                    Bone[] destBones = skeleton_.Bones;
                    Bone[] srcBones = skeleton.Bones;
                    bool compatible = true;

                    for(int i = 0; i < destBones.Length; ++i)
                    {
                        if(destBones[i].node_ && destBones[i].name_ == srcBones[i].name_ && destBones[i].parentIndex_ ==
                                                                                             srcBones[i].parentIndex_)
                        {
                            // If compatible, just copy the values and retain the old node and animated status
                            Node boneNode = destBones[i].node_;
                            bool animated = destBones[i].animated_;
                            destBones[i] = srcBones[i];
                            destBones[i].node_ = boneNode;
                            destBones[i].animated_ = animated;
                        }
                        else
                        {
                            compatible = false;
                            break;
                        }
                    }
                    if(compatible)
                        return;
                }

                RemoveAllAnimationStates();

                // Detach the rootbone of the previous model if any
                if(createBones)
                    RemoveRootBone();

                skeleton_.Define(skeleton);

                // Merge bounding boxes from non-master models
                FinalizeBoneBoundingBoxes();

                Bone[] bones = skeleton_.Bones;
                // Create scene nodes for the bones
                if(createBones)
                {
                    for(int i = 0; i < bones.Length; i++)// (var i in bones)
                    {
                        // Create bones as local, as they are never to be directly synchronized over the network
                        ref Bone b = ref bones[i];
                        Node boneNode = node_.CreateChild(b.name_);
                        boneNode.AddListener(this);
                        boneNode.SetTransform(b.initialPosition_, b.initialRotation_, b.initialScale_);
                        // Copy the model component's temporary status
                        //boneNode.SetTemporary(IsTemporary);
                        b.node_ = boneNode;
                    }

                    for(int i = 0; i < bones.Length; ++i)
                    {
                        int parentIndex = bones[i].parentIndex_;
                        if(parentIndex != i && parentIndex < bones.Length)
                            bones[parentIndex].node_.AddChild(bones[i].node_);
                    }
                }

                BoneHierarchyCreated eventData = new BoneHierarchyCreated
                {
                    Node = node_
                };

                node_.SendEvent(ref eventData);

            }
            else
            {
                // For non-master models: use the bone nodes of the master model
                skeleton_.Define(skeleton);

                // Instruct the master model to refresh (merge) its bone bounding boxes
                AnimatedModel master = node_.GetComponent<AnimatedModel>();
                if(master && master != this)
                    master.FinalizeBoneBoundingBoxes();

                if(createBones)
                {
                    Bone[] bones = skeleton_.Bones;
                    for(int i = 0; i < bones.Length; ++i)
                    {
                        Node boneNode = node_.GetChild(bones[i].name_, true);
                        if(boneNode)
                            boneNode.AddListener(this);
                        bones[i].node_ = boneNode;
                    }
                }
            }

            assignBonesPending_ = !createBones;
        }


        /*
        void SetModelAttr(const ResourceRef& value)
{
    ResourceCache* cache = GetSubsystem<ResourceCache>();
    // When loading a scene, set model without creating the bone nodes (will be assigned later during post-load)
    SetModel(cache->GetResource<Model>(value.name_), !loading_);
    }

    void SetBonesEnabledAttr(const VariantVector& value)
    {
        Vector < Bone > &bones = skeleton_.GetModifiableBones();
        for (unsigned i = 0; i < bones.Length && i < value.Length; ++i)
            bones[i].animated_ = value[i].GetBool();
    }

    void SetAnimationStatesAttr(const VariantVector& value)
    {
        ResourceCache* cache = GetSubsystem<ResourceCache>();
        RemoveAllAnimationStates();
        unsigned index = 0;
        unsigned numStates = index < value.Length ? value[index++].GetUInt() : 0;
        // Prevent negative or overly large value being assigned from the editor
        if (numStates > M_MAX_INT)
            numStates = 0;
        if (numStates > MAX_ANIMATION_STATES)
            numStates = MAX_ANIMATION_STATES;

        animationStates_.Reserve(numStates);
        while (numStates--)
        {
            if (index + 5 < value.Length)
            {
                // Note: null animation is allowed here for editing
                const ResourceRef&animRef = value[index++].GetResourceRef();
                SharedPtr<AnimationState> newState(new AnimationState(this, cache->GetResource<Animation>(animRef.name_)));
                animationStates_.Push(newState);

                newState->SetStartBone(skeleton_.GetBone(value[index++].GetString()));
                newState->SetLooped(value[index++].GetBool());
                newState->SetWeight(value[index++].GetFloat());
                newState->SetTime(value[index++].GetFloat());
                newState->SetLayer((unsigned char)value[index++].GetInt());
            }
            else
            {
                // If not enough data, just add an empty animation state
                SharedPtr<AnimationState> newState(new AnimationState(this, 0));
                animationStates_.Push(newState);
            }
        }

        if (animationStates_.Length)
        {
            MarkAnimationDirty();
            MarkAnimationOrderDirty();
        }
    }*/
        /*
        void SetMorphsAttr(const PODVector<byte>& value)
        {
            for (int index = 0; index < value.Length; ++index)
                SetMorphWeight(index, (float)value[index] / 255.0f);
        }

        ResourceRef GetModelAttr() const
    {
        return GetResourceRef(model_, Model::GetTypeStatic());
    }

    VariantVector GetBonesEnabledAttr() const
    {
        VariantVector ret;
    const Vector<Bone>& bones = skeleton_.GetBones();
        ret.Reserve(bones.Length);
        for (Vector<Bone>::ConstIterator i = bones.Begin(); i != bones.End(); ++i)
            ret.Push(i->animated_);
        return ret;
    }

    VariantVector GetAnimationStatesAttr() const
    {
        VariantVector ret;
    ret.Reserve(animationStates_.Length * 6 + 1);
        ret.Push(animationStates_.Length);
        for (Vector<SharedPtr<AnimationState>>::ConstIterator i = animationStates_.Begin(); i != animationStates_.End(); ++i)
        {
            AnimationState* state = *i;
    Animation* animation = state->GetAnimation();
    Bone* startBone = state->GetStartBone();
    ret.Push(GetResourceRef(animation, Animation::GetTypeStatic()));
            ret.Push(startBone? startBone->name_ : String::EMPTY);
            ret.Push(state->IsLooped());
            ret.Push(state->GetWeight());
            ret.Push(state->GetTime());
            ret.Push((int) state->GetLayer());
        }
        return ret;
    }

    const PODVector<unsigned char>& GetMorphsAttr() const
    {
        attrBuffer_.Clear();
        for (Vector<ModelMorph>::ConstIterator i = morphs_.Begin(); i != morphs_.End(); ++i)
            attrBuffer_.WriteUByte((unsigned char)(i->weight_* 255.0f));

        return attrBuffer_.GetBuffer();
    }*/

        void UpdateBoneBoundingBox()
        {
            if(skeleton_.NumBones > 0)
            {
                // The bone bounding box is in local space, so need the node's inverse transform
                boneBoundingBox_.Clear();
                Matrix inverseNodeTransform;
                Matrix.Invert(ref node_.WorldTransform, out inverseNodeTransform);

                Bone[] bones = skeleton_.Bones;
                foreach(Bone i in bones)
                {
                    Node boneNode = i.node_;
                    if(!boneNode)
                        continue;

                    // Use hitbox if available. If not, use only half of the sphere radius
                    /// \todo The sphere radius should be multiplied with bone scale
                    if((i.collisionMask_ & Bone.BONECOLLISION_BOX) != 0)
                    {
                        var m = boneNode.WorldTransform * inverseNodeTransform;
                        boneBoundingBox_.Merge(i.boundingBox_.Transformed(ref m));
                    }
                    else if((i.collisionMask_ & Bone.BONECOLLISION_SPHERE) != 0)
                    {
                        var bs = new BoundingSphere(Vector3.Transform(boneNode.WorldPosition, inverseNodeTransform), i.radius_ * 0.5f);
                        boneBoundingBox_.Merge(ref bs);
                    }
                }
            }

            boneBoundingBoxDirty_ = false;
            worldBoundingBoxDirty_ = true;
        }

        public override void OnNodeSet(Node node)
        {
            base.OnNodeSet(node);

            if(node)
            {
                // If this AnimatedModel is the first in the node, it is the master which controls animation & morphs
                isMaster_ = (GetComponent<AnimatedModel>() == this);
            }
        }

        public override void OnMarkedDirty(Node node)
        {
            base.OnMarkedDirty(node);

            // If the scene node or any of the bone nodes move, mark skinning dirty
            if(skeleton_.NumBones > 0)
            {
                skinningDirty_ = true;
                // Bone bounding box doesn't need to be marked dirty when only the base scene node moves
                if(node != node_)
                    boneBoundingBoxDirty_ = true;
            }
        }

        protected override void OnWorldBoundingBoxUpdate()
        {
            if(isMaster_)
            {
                // Note: do not update bone bounding box here, instead do it in either of the threaded updates
                worldBoundingBox_ = boneBoundingBox_.Transformed(ref node_.WorldTransform);
                //worldBoundingBox_ = boundingBox_.Transformed(ref node_.WorldTransform);
            }
            else
            {
                // Non-master animated models get the bounding box from the master
                /// \todo If it's a skinned attachment that does not cover the whole body, it will have unnecessarily large bounds
                AnimatedModel master = node_.GetComponent<AnimatedModel>();
                // Check if we've become the new master model in case the original was deleted
                if(master == this)
                    isMaster_ = true;
                if(master)
                    worldBoundingBox_ = master.WorldBoundingBox;
            }
        }

        void AssignBoneNodes()
        {
            assignBonesPending_ = false;

            if(!node_)
                return;

            // Find the bone nodes from the node hierarchy and add listeners
            Bone[] bones = skeleton_.Bones;
            bool boneFound = false;
            for(int i = 0; i < bones.Length; i++)
            {
                Node boneNode = node_.GetChild(bones[i].name_, true);
                if(boneNode)
                {
                    boneFound = true;
                    boneNode.AddListener(this);
                }
                bones[i].node_ = boneNode;
            }

            // If no bones found, this may be a prefab where the bone information was left out.
            // In that case reassign the skeleton now if possible
            if(!boneFound && model_)
                SetSkeleton(model_.Skeleton, true);

            // Re-assign the same start bone to animations to get the proper bone node this time
            foreach(var state in animationStates_)
            {
                state.SetStartBone(state.StartBone);
            }
        }

        void FinalizeBoneBoundingBoxes()
        {
            Bone[] bones = skeleton_.Bones;
            List<AnimatedModel> models = new List<AnimatedModel>();
            GetComponents(models);

            if(models.Count > 1)
            {
                // Reset first to the model resource's original bone bounding information if available (should be)
                if(model_)
                {
                    Bone[] modelBones = model_.Skeleton.Bones;
                    for(int i = 0; i < bones.Length && i < modelBones.Length; ++i)
                    {
                        bones[i].collisionMask_ = modelBones[i].collisionMask_;
                        bones[i].radius_ = modelBones[i].radius_;
                        bones[i].boundingBox_ = modelBones[i].boundingBox_;
                    }
                }

                // Get matching bones from all non-master models and merge their bone bounding information
                // to prevent culling errors (master model may not have geometry in all bones, or the bounds are smaller)
                foreach(var i in models)
                {
                    if(i == this)
                        continue;

                    Skeleton otherSkeleton = i.Skeleton;
                    for(int j = 0; j < bones.Length; ++j)
                    {
                        ref Bone bone = ref bones[j];
                        Bone otherBone = otherSkeleton.GetBone(bone.name_);
                        if(otherBone != null)
                        {
                            if((otherBone.collisionMask_ & Bone.BONECOLLISION_SPHERE) != 0)
                            {
                                bone.collisionMask_ |= Bone.BONECOLLISION_SPHERE;
                                bone.radius_ = Math.Max(bone.radius_, otherBone.radius_);
                            }

                            if((otherBone.collisionMask_ & Bone.BONECOLLISION_BOX) != 0)
                            {
                                bone.collisionMask_ |= Bone.BONECOLLISION_BOX;
                                if(bone.boundingBox_.Defined())
                                    bone.boundingBox_.Merge(otherBone.boundingBox_);
                                else
                                    bone.boundingBox_.Define(otherBone.boundingBox_);
                            }
                        }
                    }
                }
            }

            unchecked
            {
                // Remove collision information from dummy bones that do not affect skinning, to prevent them from being merged
                // to the bounding box and making it artificially large
                //foreach(var i in bones)
                for(int i = 0; i < bones.Length; i++)
                {
                    ref Bone bone = ref bones[i];
                    if((bone.collisionMask_ & Bone.BONECOLLISION_BOX) != 0 && bone.boundingBox_.Size.Length() < MathUtil.Epsilon)
                        bone.collisionMask_ &= (byte)~Bone.BONECOLLISION_BOX;
                    if((bone.collisionMask_ & Bone.BONECOLLISION_SPHERE) != 0 && bone.radius_ < MathUtil.Epsilon)
                        bone.collisionMask_ &= (byte)~Bone.BONECOLLISION_SPHERE;
                }
            }
        }

        void RemoveRootBone()
        {
            Bone rootBone = skeleton_.RootBone;
            if(rootBone != null && rootBone.node_)
                rootBone.node_.Remove();
        }

        public void MarkAnimationDirty()
        {
            if(isMaster_)
            {
                animationDirty_ = true;
                MarkForUpdate();
            }
        }

        public void MarkAnimationOrderDirty()
        {
            if(isMaster_)
            {
                animationOrderDirty_ = true;
                MarkForUpdate();
            }
        }

        void SetGeometryBoneMappings()
        {
            foreach(Span<Matrix> m in geometrySkinMatrices_)
            {
                UnmanagedPool<Matrix>.Shared.Release(m);
            }

            geometrySkinMatrices_.Clear();
            geometrySkinMatrixPtrs_.Clear();

            if(0 == geometryBoneMappings_.Length)
                return;

            // Check if all mappings are empty, then we do not need to use mapped skinning
            bool allEmpty = true;
            for(int i = 0; i < geometryBoneMappings_.Length; ++i)
                if(geometryBoneMappings_[i].Length > 0)
                    allEmpty = false;

            if(allEmpty)
                return;

            // Reserve space for per-geometry skinning matrices
            Array.Resize(ref geometrySkinMatrices_, geometryBoneMappings_.Length);
            for(int i = 0; i < geometryBoneMappings_.Length; ++i)
            {
                geometrySkinMatrices_[i] = UnmanagedPool<Matrix>.Shared.Acquire(geometryBoneMappings_[i].Length);
            //    Array.Resize(ref geometrySkinMatrices_[i], geometryBoneMappings_[i].Length);
            }

            // Build original-to-skinindex matrix pointer mapping for fast copying
            // Note: at this point layout of geometrySkinMatrices_ cannot be modified or pointers become invalid
            Array.Resize(ref geometrySkinMatrixPtrs_, skeleton_.NumBones);
            for(int i = 0; i < geometryBoneMappings_.Length; ++i)
            {
                Array.Resize(ref geometrySkinMatrixPtrs_[i], geometryBoneMappings_[i].Length);
                for (int j = 0; j < geometryBoneMappings_[i].Length; ++j)
                    geometrySkinMatrixPtrs_[geometryBoneMappings_[i][j]][j]= (IntPtr)Unsafe.AsPointer(ref geometrySkinMatrices_[i][j]);
            }
        }

        void UpdateAnimation(ref FrameInfo frame)
        {
            // If using animation LOD, accumulate time and see if it is time to update
            if(animationLodBias_ > 0.0f && animationLodDistance_ > 0.0f)
            {
                // Perform the first update always regardless of LOD timer
                if(animationLodTimer_ >= 0.0f)
                {
                    animationLodTimer_ += animationLodBias_ * frame.timeStep * ANIMATION_LOD_BASESCALE;
                    if(animationLodTimer_ >= animationLodDistance_)
                        animationLodTimer_ = (animationLodTimer_ % animationLodDistance_);
                    else
                        return;
                }
                else
                    animationLodTimer_ = 0.0f;
            }

            ApplyAnimation();
        }

        static int CompareAnimationOrder(AnimationState lhs, AnimationState rhs)
        {
            return lhs.layer - rhs.layer;
        }

        void ApplyAnimation()
        {
            // Make sure animations are in ascending priority order
            if(animationOrderDirty_)
            {
                animationStates_.Sort(CompareAnimationOrder);
                animationOrderDirty_ = false;
            }

            // Reset skeleton, apply all animations, calculate bones' bounding box. Make sure this is only done for the master model
            // (first AnimatedModel in a node)
            if(isMaster_)
            {
                skeleton_.ResetSilent();

                foreach(var i in animationStates_)
                    i.Apply();

                // Skeleton reset and animations apply the node transforms "silently" to avoid repeated marking dirty. Mark dirty now
                node_.MarkDirty();

                // Calculate new bone bounding box
                UpdateBoneBoundingBox();
            }

            animationDirty_ = false;
        }

        void UpdateSkinning()
        {
            // Note: the model's world transform will be baked in the skin matrices
            Bone[] bones = skeleton_.Bones;
            // Use model's world transform in case a bone is missing
            ref Matrix worldTransform = ref node_.WorldTransform;

            // Skinning with global matrices only
            if(geometrySkinMatrices_.Length == 0)
            {
                for(int i = 0; i < bones.Length; ++i)
                {
                    Bone bone = bones[i];
                    if(bone.node_)
                        skinMatrices_[i] = bone.offsetMatrix_ * bone.node_.WorldTransform;
                    else
                        skinMatrices_[i] = worldTransform;
                }
            }
            // Skinning with per-geometry matrices
            else
            {
                for(int i = 0; i < bones.Length; ++i)
                {
                    Bone bone = bones[i];
                    if(bone.node_)
                        skinMatrices_[i] = bone.offsetMatrix_ * bone.node_.WorldTransform;
                    else
                        skinMatrices_[i] = worldTransform;

                    // Copy the skin matrix to per-geometry matrices as needed
                    for(int j = 0; j < geometrySkinMatrixPtrs_[i].Length; ++j)
                    {
                        *(Matrix*)geometrySkinMatrixPtrs_[i][j] = skinMatrices_[i];
                    }
                }
            }

            skinningDirty_ = false;
        }
        


    }
}
