using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Godot.Collections;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;
using Array = Godot.Collections.Array;

namespace Renderite.Godot.Source.Helpers;

public static class MeshConverter
{
    public static List<(Array arrays, Array blendShapes, Mesh.ArrayFormat flags)> Convert(MeshUploadData meshUploadData)
    {
        var buffer = SharedMemoryAccessor.Instance.AccessData(meshUploadData.buffer);
        var mem = new MemoryStream(buffer.ToArray());
        var reader = new BinaryReader(mem);
        var vertCount = meshUploadData.vertexCount;
        var boneWeightCount = meshUploadData.boneWeightCount;
        var boneCount = meshUploadData.boneCount;

        var usesPosition = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.Position);
        var usesNormal = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.Normal);
        var usesTangent = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.Tangent);
        var usesColor = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.Color);
        var usesUV0 = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.UV0);
        var usesUV1 = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.UV1);
        var usesUV2 = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.UV2);
        var usesUV3 = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.UV3);
        var usesUV4 = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.UV4);
        var usesUV5 = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.UV5);
        var usesUV6 = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.UV6);
        var usesUV7 = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.UV7);
        var usesBoneWeights = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.BoneWeights);
        var usesBoneIndices = meshUploadData.vertexAttributes.Any(i => i.attribute is VertexAttributeType.BoneIndicies);

        var positionList = new List<Vector3>(usesPosition ? vertCount : 0);
        var normalList = new List<Vector3>(usesNormal ? vertCount : 0);
        var tangentList = new List<Vector4>(usesTangent ? vertCount : 0);
        var colorList = new List<Color>(usesColor ? vertCount : 0);
        var uv0List = new List<Vector2>(usesUV0 ? vertCount : 0);
        var uv1List = new List<Vector2>(usesUV1 ? vertCount : 0);
        var uv2List = new List<Vector2>(usesUV2 ? vertCount : 0);
        var uv3List = new List<Vector2>(usesUV3 ? vertCount : 0);
        var uv4List = new List<Vector2>(usesUV4 ? vertCount : 0);
        var uv5List = new List<Vector2>(usesUV5 ? vertCount : 0);
        var uv6List = new List<Vector2>(usesUV6 ? vertCount : 0);
        var uv7List = new List<Vector2>(usesUV7 ? vertCount : 0);
        List<Vector2>[] uvLists = [uv0List, uv1List, uv2List, uv3List, uv4List, uv5List, uv6List, uv7List];
        
        var weightList = new List<float>(usesBoneWeights ? vertCount * 8 : 0);
        var boneIndexList = new List<int>(usesBoneIndices ? vertCount * 8 : 0);

        var use8Bones = false;

        var vertexActions = new List<Action>();

        foreach (var attribute in meshUploadData.vertexAttributes)
        {
            var type = attribute.attribute;
            var format = attribute.format;
            var size = attribute.Size;
            var dimension = attribute.dimensions;
            var used = 0;
            switch (type)
            {
                case VertexAttributeType.Position:
                {
                    if (format is VertexAttributeFormat.Float32)
                    {
                        vertexActions.Add(() => positionList.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
                        used = sizeof(float) * 3;
                    }
                    else if (format is VertexAttributeFormat.Half16)
                    {
                        vertexActions.Add(() => positionList.Add(new Vector3((float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf())));
                        used = 2 * 3; //sizeof(Half) * 3
                    }
                    else vertexActions.Add(() => positionList.Add(Vector3.Zero));
                    break;
                }
                case VertexAttributeType.Normal:
                {
                    //frooxengine supposedly supports 2d normals but thats a big fat TODO
                    if (dimension is 3)
                    {
                        if (format is VertexAttributeFormat.Float32)
                        {
                            vertexActions.Add(() => normalList.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
                            used = sizeof(float) * 3;
                        }
                        else if (format is VertexAttributeFormat.Half16)
                        {
                            vertexActions.Add(() => normalList.Add(new Vector3((float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf())));
                            used = 2 * 3; //sizeof(Half) * 3
                        }
                        else vertexActions.Add(() => normalList.Add(Vector3.Zero));
                    }
                    else vertexActions.Add(() => normalList.Add(Vector3.Zero));
                    break;
                }
                case VertexAttributeType.Tangent:
                {
                    if (dimension is 4)
                    {
                        if (format is VertexAttributeFormat.Float32)
                        {
                            vertexActions.Add(() => tangentList.Add(new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
                            used = sizeof(float) * 4;
                        }
                        else if (format is VertexAttributeFormat.Half16)
                        {
                            vertexActions.Add(() => tangentList.Add(new Vector4((float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf())));
                            used = 2 * 4; //sizeof(Half) * 4
                        }
                        else if (format is VertexAttributeFormat.UNorm8)
                        {
                            float Read() => Mathf.Remap(reader.ReadByte(), byte.MinValue, byte.MaxValue, -1f, 1f);
                            vertexActions.Add(() => tangentList.Add(new Vector4(Read(),Read(),Read(),Read())));
                            used = 4;
                        }
                        else vertexActions.Add(() => tangentList.Add(Vector4.Zero));
                    }
                    else vertexActions.Add(() => tangentList.Add(Vector4.Zero));
                    break;
                }
                case VertexAttributeType.Color:
                {
                    if (dimension is 4)
                    {
                        if (format is VertexAttributeFormat.Float32)
                        {
                            vertexActions.Add(() => colorList.Add(new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())));
                            used = sizeof(float) * 4;
                        }
                        else if (format is VertexAttributeFormat.Half16)
                        {
                            vertexActions.Add(() => colorList.Add(new Color((float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf())));
                            used = 2 * 4; //sizeof(Half) * 4
                        }
                        else if (format is VertexAttributeFormat.UNorm8)
                        {
                            vertexActions.Add(() => colorList.Add(Color.Color8(reader.ReadByte(),reader.ReadByte(),reader.ReadByte(),reader.ReadByte())));
                            used = 4;
                        }
                        else vertexActions.Add(() => colorList.Add(Colors.White));
                    }
                    else vertexActions.Add(() => colorList.Add(Colors.White));
                    break;
                }
                case VertexAttributeType.UV0:
                case VertexAttributeType.UV1:
                case VertexAttributeType.UV2:
                case VertexAttributeType.UV3:
                case VertexAttributeType.UV4:
                case VertexAttributeType.UV5:
                case VertexAttributeType.UV6:
                case VertexAttributeType.UV7:
                {
                    var index = (int)type - (int)VertexAttributeType.UV0;
                    var list = uvLists[index];
                    if (dimension >= 2)
                    {
                        if (format is VertexAttributeFormat.Float32)
                        {
                            vertexActions.Add(() => list.Add(new Vector2(reader.ReadSingle(), reader.ReadSingle())));
                            used = sizeof(float) * 2;
                        }
                        else if (format is VertexAttributeFormat.Half16)
                        {
                            vertexActions.Add(() => list.Add(new Vector2((float)reader.ReadHalf(), (float)reader.ReadHalf())));
                            used = 2 * 2; //sizeof(Half) * 2
                        }
                        else if (format is VertexAttributeFormat.UNorm8)
                        {
                            float Read() => Mathf.Remap(reader.ReadByte(), byte.MinValue, byte.MaxValue, 0f, 1f);
                            vertexActions.Add(() => list.Add(new Vector2(Read(), Read())));
                            used = 2;
                        }
                        else if (format is VertexAttributeFormat.UNorm16)
                        {
                            float Read() => Mathf.Remap(reader.ReadUInt16(), ushort.MinValue, ushort.MaxValue, 0f, 1f);
                            vertexActions.Add(() => list.Add(new Vector2(Read(), Read())));
                            used = sizeof(ushort) * 2;
                        }
                        else vertexActions.Add(() => list.Add(Vector2.Zero));
                    }
                    else vertexActions.Add(() => list.Add(Vector2.Zero));
                    break;
                }
                case VertexAttributeType.BoneWeights:
                {
                    if (dimension > 4) use8Bones = true;
                    if (format is not (VertexAttributeFormat.Float32 or VertexAttributeFormat.Half16 or VertexAttributeFormat.UNorm8 or VertexAttributeFormat.UNorm16))
                    {
                        vertexActions.Add(use8Bones ? 
                            () => weightList.AddRange(0, 0, 0, 0, 0, 0, 0, 0) : 
                            () => weightList.AddRange(0, 0, 0, 0));
                        break;
                    }
                    Func<float> readMethod = format switch
                    {
                        VertexAttributeFormat.Float32 => () => reader.ReadSingle(),
                        VertexAttributeFormat.Half16 => () => (float)reader.ReadHalf(),
                        VertexAttributeFormat.UNorm8 => () => Mathf.Remap(reader.ReadByte(), byte.MinValue, byte.MaxValue, 0f, 1f),
                        VertexAttributeFormat.UNorm16 => () => Mathf.Remap(reader.ReadUInt16(), ushort.MinValue, ushort.MaxValue, 0f, 1f),
                        _ => throw new ArgumentOutOfRangeException(), //should never happen
                    };
                    
                    var maxCount = use8Bones ? 8 : 4;
                    var toRead = Mathf.Min(maxCount, dimension);
                    
                    used = toRead * format switch
                    {
                        VertexAttributeFormat.Float32 => 4,
                        VertexAttributeFormat.Half16 => 2,
                        VertexAttributeFormat.UNorm8 => 1,
                        VertexAttributeFormat.UNorm16 => 2,
                    };
                    vertexActions.Add(() => weightList.AddRange(Enumerable.Range(0, toRead).Select(_ => readMethod.Invoke())));
                    if (toRead < maxCount)
                    {
                        var bufferCount = maxCount - toRead;
                        vertexActions.Add(() => weightList.AddRange(Enumerable.Range(0, bufferCount).Select(_ => 0f)));
                    }
                    break;
                }
                case VertexAttributeType.BoneIndicies:
                {
                    if (dimension > 4) use8Bones = true;
                    if (format is not 
                        (VertexAttributeFormat.SInt8 or 
                        VertexAttributeFormat.SInt16 or 
                        VertexAttributeFormat.SInt32 or 
                        VertexAttributeFormat.UInt8 or 
                        VertexAttributeFormat.UInt16 or 
                        VertexAttributeFormat.UInt32))
                    {
                        vertexActions.Add(use8Bones ? 
                            () => boneIndexList.AddRange(-1, -1, -1, -1, -1, -1, -1, -1) : 
                            () => boneIndexList.AddRange(-1, -1, -1, -1));
                        break;
                    }
                    Func<int> readMethod = format switch
                    {
                        VertexAttributeFormat.SInt8 => () => reader.ReadSByte(),
                        VertexAttributeFormat.SInt16 => () => reader.ReadInt16(),
                        VertexAttributeFormat.SInt32 => () => reader.ReadInt32(),
                        VertexAttributeFormat.UInt8 => () => reader.ReadByte(),
                        VertexAttributeFormat.UInt16 => () => reader.ReadUInt16(),
                        VertexAttributeFormat.UInt32 => () => (int)reader.ReadUInt32(), //TODO is this correct?
                        _ => throw new ArgumentOutOfRangeException(), //should never happen
                    };
                    
                    var maxCount = use8Bones ? 8 : 4;
                    var toRead = Mathf.Min(maxCount, dimension);
                    
                    used = toRead * format switch
                    {
                        VertexAttributeFormat.SInt8 => 1,
                        VertexAttributeFormat.SInt16 => 2,
                        VertexAttributeFormat.SInt32 => 4,
                        VertexAttributeFormat.UInt8 => 1,
                        VertexAttributeFormat.UInt16 => 2,
                        VertexAttributeFormat.UInt32 => 4,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    vertexActions.Add(() => boneIndexList.AddRange(Enumerable.Range(0, toRead).Select(_ => readMethod.Invoke())));
                    if (toRead < maxCount)
                    {
                        var bufferCount = maxCount - toRead;
                        vertexActions.Add(() => boneIndexList.AddRange(Enumerable.Range(0, bufferCount).Select(_ => -1)));
                    }
                    break;
                }
            }
            if (used > size) throw new Exception($"Used bytes are larger than actual size. Used: {used} Size: {size}");
            if (used < size)
            {
                //if we don't use up the entirety of the size of the attribute, we need to account for it here
                var toRead = size - used;
                Action method = toRead switch
                {
                    1 => () => reader.ReadByte(),
                    2 => () => reader.ReadInt16(),
                    4 => () => reader.ReadInt32(),
                    _ => () => { for (var i = 0; i < toRead; i++) reader.ReadByte(); },
                };
                vertexActions.Add(method);
            }
        }

        for (var i = 0; i < vertCount; i++)
            foreach (var action in vertexActions) action.Invoke();
        // TODO: do something
        return default;
    }
}
