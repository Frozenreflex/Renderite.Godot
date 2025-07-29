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

    protected override bool TrackMeshAssetChanges => true;

    public void UpdateAllTransforms()
    {
        if (TrackedBones is null || Mesh is null || SkeletonRid == NullRid) return;
        RenderingServer.SkeletonAllocateData(SkeletonRid, Mesh.Skin.Length);
        if (InstanceRid != NullRid && Mesh.AssetID != NullRid)
            foreach (var bone in TrackedBones)
                bone?.UpdateTransform();
    }

    protected override void OnMeshAssetChanged() => UpdateAllTransforms();
    protected override void OnMeshChanged()
    {
        if (Mesh.AssetID != NullRid) RenderingServer.InstanceAttachSkeleton(InstanceRid, SkeletonRid); //TODO is this needed
        UpdateAllTransforms();
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
    private void UpdateInverseGlobal() => InverseGlobal = Base.GlobalTransform.AffineInverse();

    public override void Cleanup()
    {
        base.Cleanup();
        RenderingServer.FreeRid(SkeletonRid);
        SkeletonRid = NullRid;
    }
}
