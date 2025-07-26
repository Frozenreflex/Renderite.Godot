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
        var buffer = SharedMemoryManager.Instance.Read(meshUploadData.buffer);
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
        var weightList = new List<float>(usesBoneWeights ? vertCount * 8 : 0);
        var indexList = new List<int>(usesBoneIndices ? vertCount * 8 : 0);

        for (var i = 0; i < vertCount; i++)
        {
            foreach (var layout in meshUploadData.vertexAttributes)
            {
                switch (layout.attribute)
                {
                    case VertexAttributeType.Position:
                        ReadVector3(positionList);
                        break;
                    case VertexAttributeType.Normal:
                        ReadVector3(normalList);
                        break;
                    case VertexAttributeType.Tangent:
                        ReadVector4(tangentList);
                        break;
                    case VertexAttributeType.Color:
                        ReadColor(colorList);
                        break;
                    case VertexAttributeType.UV0:
                        ReadUV(uv0List);
                        break;
                    case VertexAttributeType.UV1:
                        ReadUV(uv1List);
                        break;
                    case VertexAttributeType.UV2:
                        ReadUV(uv2List);
                        break;
                    case VertexAttributeType.UV3:
                        ReadUV(uv3List);
                        break;
                    case VertexAttributeType.UV4:
                        ReadUV(uv4List);
                        break;
                    case VertexAttributeType.UV5:
                        ReadUV(uv5List);
                        break;
                    case VertexAttributeType.UV6:
                        ReadUV(uv6List);
                        break;
                    case VertexAttributeType.UV7:
                        ReadUV(uv7List);
                        break;
                    case VertexAttributeType.BoneWeights:
                        
                        break;
                    case VertexAttributeType.BoneIndicies:
                        break;


                    
                }

                continue;

                void ReadBoneInfo<T>(List<T> list, Action<T> read)
                {
                    
                }
                void ReadVector3(List<Vector3> list)
                {
                    if (layout.dimensions < 3) goto bad;
                    if (layout.format is VertexAttributeFormat.Float32)
                    {
                        list.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                        Cleanup(sizeof(float) * 3);
                        return;
                    }
                    if (layout.format is VertexAttributeFormat.Half16)
                    {
                        list.Add(new Vector3((float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf()));
                        Cleanup(2 * 3); //sizeof(Half) * 3
                        //why the fuck is sizeof(Half) unsafe
                        return;
                    }
                    
                    bad:
                    //format is wrong, pad with placeholder value and continue
                    list.Add(Vector3.Zero);
                    reader.ReadBytes(layout.Size);
                }
                void ReadVector4(List<Vector4> list)
                {
                    if (layout.dimensions < 4) goto bad;
                    if (layout.format is VertexAttributeFormat.Float32)
                    {
                        list.Add(new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                        Cleanup(sizeof(float) * 4);
                        return;
                    }
                    if (layout.format is VertexAttributeFormat.Half16)
                    {
                        list.Add(new Vector4((float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf()));
                        Cleanup(2 * 4);
                        return;
                    }

                    bad:
                    //format is wrong, pad with placeholder value and continue
                    list.Add(Vector4.Zero);
                    reader.ReadBytes(layout.Size);
                }
                void ReadColor(List<Color> list)
                {
                    if (layout.dimensions < 4) goto bad;
                    if (layout.format is VertexAttributeFormat.Float32)
                    {
                        list.Add(new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                        Cleanup(sizeof(float) * 4);
                        return;
                    }
                    if (layout.format is VertexAttributeFormat.Half16)
                    {
                        list.Add(new Color((float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf(), (float)reader.ReadHalf()));
                        Cleanup(2 * 4);
                        return;
                    }
                    if (layout.format is VertexAttributeFormat.UNorm8)
                    {
                        list.Add(new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()));
                        Cleanup(sizeof(byte) * 4);
                        return;
                    }

                    bad:
                    //format is wrong, pad with placeholder value and continue
                    list.Add(Colors.White);
                    reader.ReadBytes(layout.Size);
                }

                void ReadUV(List<Vector2> list)
                {
                    //yeah, it *can* send a 3d or 4d uv, but as far as i can tell they aren't actually used by any shaders
                    if (layout.dimensions is not (2 or 3 or 4)) goto bad;
                    //the only format reso uses for UVs is Float32
                    if (layout.format is not VertexAttributeFormat.Float32) goto bad;
                    list.Add(new Vector2(reader.ReadSingle(), reader.ReadSingle()));
                    switch (layout.dimensions)
                    {
                        case 3:
                            reader.ReadSingle();
                            break;
                        case 4:
                            reader.ReadSingle();
                            reader.ReadSingle();
                            break;
                    }
                    return;
                    
                    bad:
                    //format is wrong, pad with placeholder value and continue
                    list.Add(Vector2.Zero);
                    reader.ReadBytes(layout.Size);
                }

                void Cleanup(int used)
                {
                    var remaining = layout.Size - used;
                    if (remaining <= 0) return;
                    for (var j = 0; j < remaining; j++)
                        reader.ReadByte();
                }
            }
        }
    }
}
