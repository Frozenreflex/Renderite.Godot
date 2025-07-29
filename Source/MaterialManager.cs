using Godot.Collections;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public class MaterialManager
{
    //we need to handle materials differently to how frooxengine/unity would due to edge cases, such as stencil shaders (needs new shaders for stencil modes and channels) and material property blocks (needs new materials to emulate material property blocks)
    public Dictionary<int, string> ShaderMap = new();
    public void Handle(ShaderUpload command)
    {
        //TODO: i have no idea how process-safe their strings are
        ShaderMap[command.assetId] = new string(command.file);
    }
    public void Handle(ShaderUnload command) => ShaderMap.Remove(command.assetId);

    public void Handle(MaterialsUpdateBatch command)
    {
        var instanceChangedBuffer = new BitSpan(SharedMemoryAccessor.Instance.AccessData(command.instanceChangedBuffer));
        var reader = new MaterialUpdateReader(command, instanceChangedBuffer);
        /*
        MaterialAsset target1 = (MaterialAsset) null;
        MaterialPropertyBlockAsset target2 = (MaterialPropertyBlockAsset) null;
        */
        
        bool? nullable1 = null;
        var flag = false;
        var num = 0;
        
        while (reader.HasNextUpdate)
        {
            var update = reader.ReadUpdate();
            if (update.updateType == MaterialPropertyUpdateType.SelectTarget)
            {
                if (num == command.materialUpdateCount) flag = true;
                num++;
                if (nullable1.HasValue)
                    reader.WriteInstanceChanged(nullable1.Value);
                nullable1 = new bool?(false);
                if (flag)
                {
                    //target2 = this.PropertyBlocks.GetAsset(update.propertyID);
                    //nullable1 = new bool?(target2.EnsureInstance());
                }
                else
                {
                    //target1 = this.Materials.GetAsset(update.propertyID);
                }
            }
            else if (flag)
            {
                bool? nullable2 = nullable1;
                //nullable1 = this.HandlePropertyBlockUpdate(ref reader, ref update, target2) ? new bool?(true) : nullable2;
            }
            else
            {
                bool? nullable3 = nullable1;
                //nullable1 = this.HandleMaterialUpdate(ref reader, ref update, target1) ? new bool?(true) : nullable3;
            }
        }
        
        if (nullable1.HasValue)
            reader.WriteInstanceChanged(nullable1.Value);
        RendererManager.Instance.BackgroundMessagingManager.SendCommand(new MaterialsUpdateBatchResult { updateBatchId = command.updateBatchId });
    }
}
