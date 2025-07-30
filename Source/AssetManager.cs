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
        public Dictionary<int, Rid> Dictionary = new();
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
            Dictionary.Remove(index);
        }
    }

    public MaterialManager MaterialManager = new();
    public Dictionary<int, MeshAsset> Meshes = new();
    public TextureManager TextureManager = new();
    //public AssetContainer Texture3Ds = new();
    //public AssetContainer Cubemaps = new();
    //public AssetContainer RenderTextures = new();

    public MeshAsset GetMesh(int index)
    {
        if (index < 0) return null;
        if (!Meshes.TryGetValue(index, out var mesh))
        {
            mesh = MeshAsset.Create();
            Meshes[index] = mesh;
        }
        return mesh;
    }

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
                if (!Meshes.TryGetValue(index, out var mesh))
                {
                    mesh = MeshAsset.Create();
                    Meshes[index] = mesh;
                }
                mesh.Upload(meshUploadData);

                RendererManager.Instance.BackgroundMessagingManager.SendCommand(new MeshUploadResult
                {
                    assetId = index,
                    instanceChanged = true,
                });
                
                break;
            }
            case MeshUnload meshUnload:
            {
                var index = meshUnload.assetId;
                if (Meshes.TryGetValue(index, out var mesh)) mesh.Cleanup();
                Meshes.Remove(index);
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
            case MaterialPropertyIdRequest materialPropertyIdRequest:
            {
                MaterialManager.Handle(materialPropertyIdRequest);
                break;
            }
            case MaterialsUpdateBatch materialsUpdateBatch:
            {
                MaterialManager.Handle(materialsUpdateBatch);
                break;
            }
            case SetTexture2DFormat setTexture2DFormat:
            {
                TextureManager.Handle(setTexture2DFormat);
                break;
            }
            case SetTexture2DProperties setTexture2DProperties:
            {
                TextureManager.Handle(setTexture2DProperties);
                break;
            }
            case SetTexture2DData setTexture2DData:
            {
                TextureManager.Handle(setTexture2DData);
                break;
            }
            case UnloadTexture2D unloadTexture2D:
            {
                TextureManager.Handle(unloadTexture2D);
                break;
            }
        }
    }
}
