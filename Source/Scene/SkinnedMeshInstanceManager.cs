using System.Collections.Generic;
using Godot;
using Renderite.Godot.Source.Helpers;

namespace Renderite.Godot.Source.Scene;

public class SkinnedMeshInstanceManager : MeshInstanceManager
{
    public class Bone
    {
        public TransformNode Node;
        public int BoneIndex;
        public SkinnedMeshInstanceManager Manager;

        public Bone(TransformNode node, int bone, SkinnedMeshInstanceManager manager)
        {
            Node = node;
            BoneIndex = bone;
            Manager = manager;
            if (GodotObject.IsInstanceValid(Node)) Node.GlobalTransformChanged += NodeOnGlobalTransformChanged;
        }
        public void UpdateTransform()
        {
            if (Manager?.Mesh is null) return;
            if (Manager.Mesh.AssetID == NullRid) return;
            var skin = Manager.Mesh.Skin;
            if (BoneIndex >= skin.Length) return;
            var skinValue = skin.ElementAtOrValue(BoneIndex, Transform3D.Identity);
            
            if (Node is null) RenderingServer.SkeletonBoneSetTransform(Manager.SkeletonRid, BoneIndex, skinValue);
            else RenderingServer.SkeletonBoneSetTransform(Manager.SkeletonRid, BoneIndex, (Manager.InverseGlobal * Node.GlobalTransform) * skinValue);
        }
        private void NodeOnGlobalTransformChanged(TransformNode obj) => UpdateTransform();
        public void Cleanup()
        {
            if (GodotObject.IsInstanceValid(Node)) Node.GlobalTransformChanged -= NodeOnGlobalTransformChanged;
            BoneIndex = -1;
            Node = null;
            Manager = null;
        }
    }

    public void UpdateAllTransforms()
    {
        if (TrackedBones is null || Mesh is null || SkeletonRid == NullRid) return;
        RenderingServer.SkeletonAllocateData(SkeletonRid, Mesh.Skin.Length);
        if (InstanceRid != NullRid && Mesh.AssetID != NullRid)
            foreach (var bone in TrackedBones)
                bone?.UpdateTransform();
    }

    protected override void OnMeshAssetChanged()
    {
        base.OnMeshAssetChanged();
        UpdateAllTransforms();
        UpdateBlendShapes();
    }
    protected override void OnMeshChanged()
    {
        base.OnMeshChanged();
        if (Mesh.AssetID != NullRid) RenderingServer.InstanceAttachSkeleton(InstanceRid, SkeletonRid); //TODO is this needed
        UpdateAllTransforms();
        UpdateBlendShapes();
    }

    public Bone[] TrackedBones
    {
        get;
        set
        {
            field = value;
            UpdateAllTransforms();
        }
    }

    public Rid SkeletonRid;
    public Transform3D InverseGlobal { get; private set; }

    public Dictionary<int, float> BlendShapeValues = new();

    protected override void OnInitialize()
    {
        base.OnInitialize();
        SkeletonRid = RenderingServer.SkeletonCreate();
        RenderingServer.InstanceAttachSkeleton(InstanceRid, SkeletonRid);
    }
    protected override void BaseOnGlobalTransformChanged(TransformNode obj)
    {
        base.BaseOnGlobalTransformChanged(obj);
        UpdateInverseGlobal();
        UpdateAllTransforms();
    }
    public void UpdateBlendShapes()
    {
        if (!InstanceValid) return;
        if (Mesh is null) return;
        var count = Mesh.BlendShapeCount;
        for (var i = 0; i < count; i++) RenderingServer.InstanceSetBlendShapeWeight(InstanceRid, i, BlendShapeValues.GetValueOrDefault(i));
    }
    private void UpdateInverseGlobal() => InverseGlobal = Base.GlobalTransform.AffineInverse();

    public override void Cleanup()
    {
        base.Cleanup();
        RenderingServer.FreeRid(SkeletonRid);
        SkeletonRid = NullRid;
        foreach (var bone in TrackedBones) bone.Cleanup();
    }
}
