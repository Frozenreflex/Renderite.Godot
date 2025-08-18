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

                PackerMemoryPool.Instance.Return(meshUploadData);
                break;
            }
            case MeshUnload meshUnload:
            {
                var index = meshUnload.assetId;
                if (Meshes.TryGetValue(index, out var mesh)) mesh.Cleanup();
                Meshes.Remove(index);
                PackerMemoryPool.Instance.Return(meshUnload);
                break;
            }
            case ShaderUpload shaderUpload:
            {
                MaterialManager.Handle(shaderUpload);
                PackerMemoryPool.Instance.Return(shaderUpload);
                break;
            }
            case ShaderUnload shaderUnload:
            {
                MaterialManager.Handle(shaderUnload);
                PackerMemoryPool.Instance.Return(shaderUnload);
                break;
            }
            case MaterialPropertyIdRequest materialPropertyIdRequest:
            {
                MaterialManager.Handle(materialPropertyIdRequest);
                PackerMemoryPool.Instance.Return(materialPropertyIdRequest);
                break;
            }
            case MaterialsUpdateBatch materialsUpdateBatch:
            {
                MaterialManager.Handle(materialsUpdateBatch);
                //PackerMemoryPool.Instance.Return(materialsUpdateBatch);
                break;
            }
            case SetTexture2DFormat setTexture2DFormat:
            {
                TextureManager.Handle(setTexture2DFormat);
                PackerMemoryPool.Instance.Return(setTexture2DFormat);
                break;
            }
            case SetTexture2DProperties setTexture2DProperties:
            {
                TextureManager.Handle(setTexture2DProperties);
                PackerMemoryPool.Instance.Return(setTexture2DProperties);
                break;
            }
            case SetTexture2DData setTexture2DData:
            {
                TextureManager.Handle(setTexture2DData);
                PackerMemoryPool.Instance.Return(setTexture2DData);
                break;
            }
            case UnloadTexture2D unloadTexture2D:
            {
                TextureManager.Handle(unloadTexture2D);
                PackerMemoryPool.Instance.Return(unloadTexture2D);
                break;
            }
            case SetRenderTextureFormat setRenderTextureFormat:
            {
                Callable.From(() =>
                {
                    TextureManager.Handle(setRenderTextureFormat);
                    PackerMemoryPool.Instance.Return(setRenderTextureFormat);
                }).CallDeferred();
                break;
            }
            case UnloadRenderTexture unloadRenderTexture:
            {
                Callable.From(() =>
                {
                    TextureManager.Handle(unloadRenderTexture);
                    PackerMemoryPool.Instance.Return(unloadRenderTexture);
                }).CallDeferred();
                break;
            }
        }
    }
}