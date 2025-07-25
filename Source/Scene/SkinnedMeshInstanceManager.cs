using Godot;

namespace Renderite.Godot.Source.Scene;

public class SkinnedMeshInstanceManager : AssetSceneInstanceManager
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
        private void UpdateTransform() => RenderingServer.SkeletonBoneSetTransform(Manager.InstanceRid, BoneIndex, Node.GlobalTransform);
        private void NodeOnGlobalTransformChanged(TransformNode obj) => UpdateTransform();
        public void Cleanup()
        {
            if (GodotObject.IsInstanceValid(Node)) Node.GlobalTransformChanged -= NodeOnGlobalTransformChanged;
            BoneIndex = -1;
            Node = null;
            Manager = null;
        }
    }
    public Bone[] TrackedBones = [];
}
