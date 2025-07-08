using Godot;
using Godot.Collections;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public class AssetManager
{
    public Dictionary<int, Rid> Meshes = new();
    public Dictionary<int, Rid> Shaders = new();
    public Dictionary<int, Rid> Texture2Ds = new();
    public Dictionary<int, Rid> Texture3Ds = new();
    public Dictionary<int, Rid> Cubemaps = new();
    public Dictionary<int, Rid> RenderTextures = new();
    public Dictionary<int, Rid> Materials = new();

    private void HandleRenderCommand(RendererCommand command)
    {
        switch (command)
        {
            case RendererShutdown:
            {
                Main.Instance.GetTree().Quit(); //todo: do we need to do more than this?
                return;
            }
            case MeshUploadData meshUploadData:
            {
                var index = meshUploadData.assetId;
                if (Meshes.TryGetValue(index, out var rid)) RenderingServer.MeshClear(rid);
                else rid = RenderingServer.MeshCreate();
                //TODO convert meshes
                //resonite meshes can have up to 8 UV channels while godot only natively supports 2
                //however, godot also supports up to 4 custom data channels, where each channel can be one of (4 byte colors, 2 or 4 half precision floats, or between 1-4 floats),
                //so we can use 3 of the custom channels for UVs if we need to, it might be good to check what range of channels reso's shaders use
                break;
            }
            case MeshUnload meshUnload:
            {
                var index = meshUnload.assetId;
                if (Meshes.TryGetValue(index, out var rid)) RenderingServer.FreeRid(rid);
                break;
            }
        }
    }
}
