using Godot;

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
            if (GodotObject.IsInstanceValid(Node))
            {
                Node.GlobalTransformChanged += NodeOnGlobalTransformChanged;
                UpdateTransform();
            }
            else RenderingServer.SkeletonBoneSetTransform(Manager.InstanceRid, BoneIndex, Transform3D.Identity);
        }
        public void UpdateTransform()
        {
            if (Manager.Mesh.AssetID == NullRid) return;
            
            if (Node is null)
                RenderingServer.SkeletonBoneSetTransform(Manager.SkeletonRid, BoneIndex, Transform3D.Identity);
            else
            {
                var skin = Manager.Mesh.Skin;
                var skinValue = BoneIndex >= 0 && BoneIndex < skin.Length ? skin[BoneIndex] : Transform3D.Identity;
                RenderingServer.SkeletonBoneSetTransform(Manager.SkeletonRid, BoneIndex, Node.GlobalTransform * skinValue);
            }
        }
        public void ResetTransform() => RenderingServer.SkeletonBoneSetTransform(Manager.SkeletonRid, BoneIndex, Transform3D.Identity);
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

    private void UpdateAllTransforms()
    {
        if (InstanceRid != NullRid && Mesh.AssetID != NullRid)
            foreach (var bone in TrackedBones)
                bone?.ResetTransform();
        else
            foreach (var bone in TrackedBones)
                bone?.UpdateTransform();
    }

    protected override void OnMeshAssetChanged() => UpdateAllTransforms();
    protected override void OnMeshChanged()
    {
        if (Mesh.AssetID != NullRid) RenderingServer.InstanceAttachSkeleton(InstanceRid, SkeletonRid); //TODO is this needed
        UpdateAllTransforms();
    }

    public Bone[] TrackedBones = [];

    public Rid SkeletonRid;

    protected override void OnInitialize()
    {
        base.OnInitialize();
        SkeletonRid = RenderingServer.SkeletonCreate();
        RenderingServer.InstanceAttachSkeleton(InstanceRid, SkeletonRid);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        RenderingServer.FreeRid(SkeletonRid);
        SkeletonRid = NullRid;
    }
}
