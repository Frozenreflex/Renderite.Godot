using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;
using Array = Godot.Collections.Array;

namespace Renderite.Godot.Source;

public class AssetManager
{
    public class AssetContainer
    {
        public global::Godot.Collections.Dictionary<int, Rid> Dictionary = new();
        private Func<Rid> _createFunc;

        public AssetContainer(Func<Rid> create)
        {
            _createFunc = create;
        }
        public Rid Get(int index)
        {
            if (Dictionary.TryGetValue(index, out var result)) return result;
            result = _createFunc();
            Dictionary[index] = result;
            return result;
        }
        public void Unload(int index)
        {
            if (Dictionary.TryGetValue(index, out var rid)) RenderingServer.FreeRid(rid);
        }
    }

    public MaterialManager MaterialManager = new();
    public AssetContainer Meshes = new(RenderingServer.MeshCreate);
    //public AssetContainer Texture2Ds = new();
    //public AssetContainer Texture3Ds = new();
    //public AssetContainer Cubemaps = new();
    //public AssetContainer RenderTextures = new();

    public void HandleRenderCommand(RendererCommand command)
    {
        switch (command)
        {
            //TODO: does frooxengine properly ensure that referenced objects are unreferenced before sending unload commands, or do we need to do so ourselves?
            case RendererShutdown:
            {
                Main.Instance.GetTree().Quit(); //todo: do we need to do more than this?
                return;
            }
            case MeshUploadData meshUploadData:
            {
                var index = meshUploadData.assetId;
                var rid = Meshes.Get(index);
                RenderingServer.MeshClear(rid);

                var meshData = MeshConverter.Convert(meshUploadData);
                foreach (var mesh in meshData)
                    RenderingServer.MeshAddSurfaceFromArrays(rid, RenderingServer.PrimitiveType.Triangles, mesh.arrays, mesh.blendShapes, null, (RenderingServer.ArrayFormat)mesh.flags);

                //TODO convert meshes
                //resonite meshes can have up to 8 UV channels while godot only natively supports 2
                //however, godot also supports up to 4 custom data channels, where each channel can be one of (4 byte colors, 2 or 4 half precision floats, or between 1-4 floats),
                //so we can use 3 of the custom channels for UVs if we need to, it might be good to check what range of channels reso's shaders use

                break;
            }
            case MeshUnload meshUnload:
            {
                Meshes.Unload(meshUnload.assetId);
                break;
            }
            case ShaderUpload shaderUpload:
            {
                MaterialManager.Handle(shaderUpload);
                break;
            }
            case ShaderUnload shaderUnload:
            {
                MaterialManager.Handle(shaderUnload);
                break;
            }
        }
    }
}
