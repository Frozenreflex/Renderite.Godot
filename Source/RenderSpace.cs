using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Godot.Source.Scene;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public partial class RenderSpace : Node3D
{
    public int Id;
    public bool IsActive;
    public bool IsOverlay;
    public Vector3 RootPosition;
    public Quaternion RootRotation;
    public Vector3 RootScale;
    public readonly List<TransformNode> Nodes = new();
    public readonly List<AssetSceneInstanceManager> Meshes = new();
    public readonly List<SkinnedMeshInstanceManager> SkinnedMeshes = new();
    private bool _lastPrivate;
    private bool _lastActive;
    private Transform3D RootTransform => TransformHelpers.TransformFromTRS(RootPosition, RootRotation, RootScale);

    public void UpdateOverlayPositioning(Node3D referenceNode)
    {
        if (IsOverlay)
        {
            //Transform = referenceNode.GlobalTransform * TransformHelpers.TransformFromTR(-RootPosition, RootRotation);
            Transform = TransformHelpers.TransformFromTRS(referenceNode.Position - RootPosition, referenceNode.Quaternion * RootRotation, referenceNode.Scale);
            //TODO: is this correct?
            /*
            transform.position = referenceTransform.position - this.RootPosition;
            transform.rotation = referenceTransform.rotation * this.RootRotation;
            transform.localScale = referenceTransform.localScale;
            */
        }
    }
    
    public void HandleUpdate(RenderSpaceUpdate data)
    {
        IsActive = data.isActive;
        IsOverlay = data.isOverlay;

        if (IsActive != _lastActive)
        {
            _lastActive = IsActive;
            
        }
        if (IsActive)
        {
            
        }

        RootPosition = data.rootTransform.position.ToGodot();
        RootRotation = data.rootTransform.rotation.ToGodot();
        RootScale = data.rootTransform.scale.ToGodotLiteral(); //we don't want to convert (1,1,1) to (-1,1,1)

        if (data.transformsUpdate is not null) HandleTransformUpdate(data.transformsUpdate);
        if (data.meshRenderersUpdate is not null) HandleMeshRenderablesUpdate(data.meshRenderersUpdate);
    }
    public void HandleTransformUpdate(TransformsUpdate update)
    {
        var removals = SharedMemoryManager.Instance.Read(update.removals);
        foreach (var remove in removals)
        {
            if (remove < 0) break;
            if (remove >= Nodes.Count) continue;
            var toRemove = Nodes[remove];
            foreach (var c in toRemove.GetChildren()) toRemove.RemoveChild(c);
            toRemove.QueueFree();
            Nodes[remove] = Nodes.Last();
            Nodes.RemoveAt(Nodes.Count - 1);
        }
        while (Nodes.Count < update.targetTransformCount)
        {
            var node = new TransformNode();
            AddChild(node);
            Nodes.Add(node);
        }
        if (!update.parentUpdates.IsEmpty)
        {
            var parentUpdates = SharedMemoryManager.Instance.Read(update.parentUpdates);
            // i just want to mention i compressed around 25-30 loc down to this
            foreach (var parentUpdate in parentUpdates)
            {
                if (parentUpdate.transformId < 0) break;
                var node = Nodes[parentUpdate.transformId];
                node.Reparent(Nodes[parentUpdate.newParentId], false);
                node.InvokeParentChanged();
            }
            //
        }
        if (!update.poseUpdates.IsEmpty)
        {
            var poseUpdates = SharedMemoryManager.Instance.Read(update.poseUpdates);
            foreach (var poseUpdate in poseUpdates)
            {
                if (poseUpdate.transformId < 0) break;
                var node = Nodes[poseUpdate.transformId];
                node.Transform = poseUpdate.pose.ToGodot();
            }
        }
    }
    public void HandleSkinnedMeshRenderablesUpdate(SkinnedMeshRenderablesUpdate update)
    {
        //skinnedmeshes don't listen to transform updates because bones need to be done in global space, rather than relative to the root
        HandleSceneInstanceAdditionRemoval(SkinnedMeshes, update, false); 
        HandleMeshRenderablesUpdateBase(update, SkinnedMeshes);
        //TODO: bounds updates
        if (!update.boneAssignments.IsEmpty)
        {
            var boneAssignments = SharedMemoryManager.Instance.Read(update.boneAssignments);
            var boneIndices = SharedMemoryManager.Instance.Read(update.boneTransformIndexes);
            var boneIndex = 0;
            foreach (var boneAssignment in boneAssignments)
            {
                if (boneAssignment.renderableIndex < 0) break;
                var mesh = SkinnedMeshes[boneAssignment.renderableIndex];
                
                var bones = new int[boneAssignment.boneCount];
                for (var i = 0; i < bones.Length; i++) bones[i] = boneIndices[boneIndex++];

                foreach (var bone in mesh.TrackedBones) bone.Cleanup();
                mesh.TrackedBones = bones.Select((bone, index) => new SkinnedMeshInstanceManager.Bone(Nodes.ElementAtOrDefault(bone), index, mesh)).ToArray();
            }
        }
        if (!update.blendshapeUpdateBatches.IsEmpty)
        {
            var blendshapeUpdateBatches = SharedMemoryManager.Instance.Read(update.blendshapeUpdateBatches);
            var blendshapeUpdates = SharedMemoryManager.Instance.Read(update.blendshapeUpdates);
            var updateIndex = 0;
            foreach (var blendshapeUpdateBatch in blendshapeUpdateBatches)
            {
                if (blendshapeUpdateBatch.renderableIndex < 0) break;
                var mesh = SkinnedMeshes[blendshapeUpdateBatch.renderableIndex];
                for (var i = 0; i < blendshapeUpdateBatch.blendshapeUpdateCount; i++)
                {
                    var blend = blendshapeUpdates[updateIndex++];
                    RenderingServer.InstanceSetBlendShapeWeight(mesh.InstanceRid, blend.blendshapeIndex, blend.weight);
                }
            }
        }
    }
    public void HandleMeshRenderablesUpdate(MeshRenderablesUpdate update)
    {
        HandleSceneInstanceAdditionRemoval(Meshes, update);
        HandleMeshRenderablesUpdateBase(update, Meshes);
    }
    private void HandleMeshRenderablesUpdateBase<T>(MeshRenderablesUpdate update, List<T> list) where T : AssetSceneInstanceManager
    {
        if (!update.meshStates.IsEmpty)
        {
            var meshStates = SharedMemoryManager.Instance.Read(update.meshStates);
            var materials = SharedMemoryManager.Instance.Read(update.meshMaterialsAndPropertyBlocks);
            var materialsIndex = 0;
            foreach (var meshState in meshStates)
            {
                if (meshState.renderableIndex < 0) break;
                var mesh = list[meshState.renderableIndex];
                var assetId = meshState.meshAssetId;
                if (mesh.AssetIndex != assetId)
                {
                    RenderingServer.InstanceSetBase(mesh.InstanceRid, assetId < 0 ? new Rid() : RendererManager.Instance.AssetManager.Meshes.Get(assetId));
                    mesh.AssetIndex = assetId;
                }
                var shadowMode = meshState.shadowCastMode.ToGodot();
                if (mesh.ShadowCastingMode != shadowMode)
                {
                    RenderingServer.InstanceGeometrySetCastShadowsSetting(mesh.InstanceRid, shadowMode);
                    mesh.ShadowCastingMode = shadowMode;
                }
                //MotionVectorGenerationMode is ignored, seems unity specific
                //TODO: sorting order
                //godot has a sorting offset, but this is a float that changes the depth of the fragment
                if (meshState.materialCount >= 0)
                {
                    for (var i = 0; i < meshState.materialCount; i++)
                    {
                        var matId = materials[materialsIndex++];
                        //TODO: get and set materials
                    }
                    if (meshState.materialPropertyBlockCount >= 0)
                    {
                        for (var i = 0; i < meshState.materialPropertyBlockCount; i++)
                        {
                            var matId = materials[materialsIndex++];
                            //TODO: same thing for property blocks
                        }
                    }
                }
            }
        }
    }
    private void HandleSceneInstanceAdditionRemoval<T>(List<T> list, RenderablesUpdate update, bool listen = true) where T : SceneInstanceManager, new()
    {
        if (!update.removals.IsEmpty)
        {
            var removals = SharedMemoryManager.Instance.Read(update.removals);
            foreach (var remove in removals)
            {
                if (remove < 0) break;
                if (remove >= list.Count) continue;
                list[remove].Cleanup();
                list[remove] = list.Last();
                list.RemoveAt(list.Count - 1);
            }
        }
        if (!update.additions.IsEmpty)
        {
            var additions = SharedMemoryManager.Instance.Read(update.additions);

            foreach (var addition in additions)
            {
                if (addition < 0) break;
                if (addition >= Nodes.Count) continue;
                var node = Nodes[addition];
                if (!IsInstanceValid(node)) throw new Exception();
                var instance = new T();
                instance.Initialize(node, listen);
                list.Add(instance);
            }
        }
    }
}
