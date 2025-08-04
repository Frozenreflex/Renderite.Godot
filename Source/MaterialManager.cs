using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;
using FileAccess = System.IO.FileAccess;

namespace Renderite.Godot.Source;

public class MaterialManager
{
    public static readonly List<StringName> PropertyIdMap = 
    [
        "_SrcBlend",
        "_DstBlend",
        "_ZTest",
    ];
    
    public Dictionary<int, ShaderInstance> ShaderMap = new();
    public Dictionary<int, MaterialInstance> Materials = new();
    
    public void Handle(ShaderUpload command)
    {
        var shader = new ShaderInstance();
        

        var shaderName = command.file.Replace(".shader", "");

        var path = $"res://Resources/Shaders/{shaderName}.gdshader";
        if (ResourceLoader.Exists(path))
        {
            GD.Print($"Loading {path}");
            var baseShader = ResourceLoader.Load<Shader>(path);
            shader.BaseShader = baseShader.Code;
        }
        else GD.Print($"Unimplemented: {shaderName}");
        ShaderMap[command.assetId] = shader;
        
        var result = new ShaderUploadResult
        {
            assetId = command.assetId,
            instanceChanged = true,
        };
        RendererManager.Instance.BackgroundMessagingManager.SendCommand(result);
    }
    public void Handle(ShaderUnload command)
    {
        if (ShaderMap.TryGetValue(command.assetId, out var remove)) remove.Cleanup();
        ShaderMap.Remove(command.assetId);
    }

    public void Handle(MaterialPropertyIdRequest request)
    {
        var command = new MaterialPropertyIdResult { requestId = request.requestId, };
        foreach (var name in request.propertyNames)
        {
            var index = PropertyIdMap.IndexOf(name);
            if (index >= 0) command.propertyIDs.Add(index);
            else
            {
                command.propertyIDs.Add(PropertyIdMap.Count);
                //GD.Print($"Index {PropertyIdMap.Count}: {name}");
                PropertyIdMap.Add(name);
            }
        }
        RendererManager.Instance.BackgroundMessagingManager.SendCommand(command);
    }

    public void Handle(MaterialsUpdateBatch command)
    {
        var instanceChangedBuffer = new BitSpan(SharedMemoryAccessor.Instance.AccessData(command.instanceChangedBuffer));
        var reader = new MaterialUpdateReader(command, instanceChangedBuffer);
        /*
        MaterialPropertyBlockAsset target2 = (MaterialPropertyBlockAsset) null;
        */
        MaterialInstance materialTarget = null;
        var isMaterialPropertyBlock = false;
        var updateCount = 0;
        bool? instanceChanged = null;
        
        while (reader.HasNextUpdate)
        {
            //TODO: WriteInstanceChanged scares me, i dont know if stuff will break if i don't update it properly
            var update = reader.ReadUpdate();
            if (update.updateType is MaterialPropertyUpdateType.SelectTarget)
            {
                if (updateCount == command.materialUpdateCount) isMaterialPropertyBlock = true;
                updateCount++;
                if (instanceChanged.HasValue) reader.WriteInstanceChanged(instanceChanged.Value);
                instanceChanged = false;
                if (isMaterialPropertyBlock)
                {
                    instanceChanged = true;
                    //target2 = this.PropertyBlocks.GetAsset(update.propertyID);
                    //nullable1 = new bool?(target2.EnsureInstance());
                }
                else
                {
                    if (!Materials.TryGetValue(update.propertyID, out var mat))
                    {
                        mat = new MaterialInstance();
                        Materials.Add(update.propertyID, mat);
                        instanceChanged = true;
                    }
                    materialTarget = mat;
                }
            }
            else if (isMaterialPropertyBlock)
            {
                //TODO
                instanceChanged = true;
                //bool? nullable2 = nullable1;
                //nullable1 = this.HandlePropertyBlockUpdate(ref reader, ref update, target2) ? new bool?(true) : nullable2;
                switch (update.updateType)
                {
                    case MaterialPropertyUpdateType.SetFloat:
                        reader.ReadFloat();
                        break;
                    case MaterialPropertyUpdateType.SetFloat4:
                        reader.ReadVector();
                        break;
                    case MaterialPropertyUpdateType.SetFloat4x4:
                        reader.ReadMatrix();
                        break;
                    case MaterialPropertyUpdateType.SetFloatArray:
                        reader.AccessFloatArray();
                        break;
                    case MaterialPropertyUpdateType.SetFloat4Array:
                        reader.AccessVectorArray();
                        break;
                    case MaterialPropertyUpdateType.SetTexture:
                        reader.ReadInt();
                        break;
                }
            }
            else
            {
                if (materialTarget is null)
                {
                    //GD.Print("Material Target is empty!");
                    continue;
                }
                if (update.updateType != MaterialPropertyUpdateType.SetShader && !materialTarget.Instantiated) throw new Exception();
                var propertyId = update.propertyID;
                switch (update.updateType)
                {
                    case MaterialPropertyUpdateType.SetShader:
                    {
                        materialTarget.Shader = ShaderMap.GetValueOrDefault(propertyId);
                        if (!materialTarget.Instantiated)
                        {
                            instanceChanged = true;
                            materialTarget.Instantiated = true;
                        }
                        break;
                    }
                    case MaterialPropertyUpdateType.SetRenderQueue:
                    case MaterialPropertyUpdateType.SetInstancing:
                    {
                        //neither are used?
                        break;
                    }
                    case MaterialPropertyUpdateType.SetRenderType:
                    {
                        var renderType = (MaterialRenderType)propertyId;
                        materialTarget.UseBlendMode = true;
                        materialTarget.Type = renderType;
                        break;
                    }
                    case MaterialPropertyUpdateType.SetFloat:
                    {
                        var value = reader.ReadFloat();
                        switch (propertyId)
                        {
                            //_SrcBlend
                            case 0:
                            {
                                materialTarget.UseBlendMode = true;
                                materialTarget.SourceBlendProp = value;
                                break;
                            }
                            //_DstBlend
                            case 1:
                            {
                                materialTarget.UseBlendMode = true;
                                materialTarget.DestinationBlendProp = value;
                                break;
                            }
                            //_ZTest
                            case 2:
                            {
                                /*
                                   Less,
                                   Greater,
                                   LessOrEqual,
                                   GreaterOrEqual,
                                   Equal,
                                   NotEqual,
                                   Always,
                                 */
                                //TODO: godot has 3 ztest modes, either it's default, which does what you expect, inverted, which draws behind other objects, and disabled, which always draws
                                //Less and LessOrEqual are essentially the same, and can map to default
                                //Greater and GreaterOrEqual can map to inverted
                                //NotEqual and Always can convert to Disabled
                                //what the fuck do i map equal to
                                var mode = 0;
                                var asInt = (int)value;
                                mode = asInt switch
                                {
                                    1 => 1,
                                    3 => 1,
                                    5 => 2,
                                    6 => 2,
                                    _ => 0,
                                };
                                switch (mode)
                                {
                                    case 0:
                                        materialTarget.ChangeBaseShader(ShaderVariant.ZTestDefault, ShaderVariant.ZTestMask);
                                        break;
                                    case 1:
                                        materialTarget.ChangeBaseShader(ShaderVariant.ZTestInvert, ShaderVariant.ZTestMask);
                                        break;
                                    case 2:
                                        materialTarget.ChangeBaseShader(ShaderVariant.ZTestDisable, ShaderVariant.ZTestMask);
                                        break;
                                }
                                break;
                            }
                            default:
                            {
                                materialTarget.SetValue(PropertyIdMap[propertyId], value);
                                break;
                            }
                        }
                        break;
                    }
                    case MaterialPropertyUpdateType.SetFloat4:
                    {
                        materialTarget.SetValue(PropertyIdMap[propertyId], reader.ReadVector());
                        break;
                    }
                    case MaterialPropertyUpdateType.SetFloat4x4:
                    {
                        materialTarget.SetValue(PropertyIdMap[propertyId], reader.ReadMatrix());
                        break;
                    }
                    case MaterialPropertyUpdateType.SetFloatArray:
                    {
                        materialTarget.SetValue(PropertyIdMap[propertyId], reader.AccessFloatArray().ToArray());
                        break;
                    }
                    case MaterialPropertyUpdateType.SetFloat4Array:
                    {
                        materialTarget.SetValue(PropertyIdMap[propertyId], reader.AccessVectorArray().ToArray());
                        break;
                    }
                    case MaterialPropertyUpdateType.SetTexture:
                    {
                        //TODO
                        var index = reader.ReadInt();
                        break;
                    }
                }
                /*
                bool? nullable3 = nullable1;
                //nullable1 = this.HandleMaterialUpdate(ref reader, ref update, target1) ? new bool?(true) : nullable3;
                */
            }
        }
        if (instanceChanged.HasValue) reader.WriteInstanceChanged(instanceChanged.Value);
        RendererManager.Instance.BackgroundMessagingManager.SendCommand(new MaterialsUpdateBatchResult { updateBatchId = command.updateBatchId });
    }
}
