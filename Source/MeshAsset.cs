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

public class MeshAsset
{
    public Rid AssetID { get; private set; }
    public Transform3D[] Skin = [];

    public event Action MeshChanged = () => { };

    public static MeshAsset Create()
    {
        var asset = new MeshAsset
        {
            AssetID = RenderingServer.MeshCreate(),
        };
        return asset;
    }
    public void Cleanup()
    {
        if (AssetID != new Rid()) RenderingServer.FreeRid(AssetID);
        Skin = null;
    }

    public void Upload(MeshUploadData meshUploadData)
    {
        RenderingServer.MeshClear(AssetID);

        try
        {
            var meshBuffer = new MeshBuffer(meshUploadData);
            if (!meshUploadData.buffer.IsEmpty)
                meshBuffer.Data = SharedMemoryAccessor.Instance.AccessSlice(meshUploadData.buffer);

            if (meshBuffer.SubmeshCount == 0)
            {
                //GD.Print("mesh with no submeshes, skipping to end");
                goto end;
            }


            var vertexMem = new MemoryStream(meshBuffer.GetRawVertexBufferData().ToArray());
            var vertReader = new BinaryReader(vertexMem);
            var vertCount = meshUploadData.vertexCount;
            var boneWeightCount = meshUploadData.boneWeightCount;
            var boneCount = meshUploadData.boneCount;
            
            //ignore the upload hint, it's a dirty liar
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

            #region myEyesHurt

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
                            vertexActions.Add(() => positionList.Add(new Vector3(vertReader.ReadSingle(), vertReader.ReadSingle(), vertReader.ReadSingle())));
                            used = sizeof(float) * 3;
                        }
                        else if (format is VertexAttributeFormat.Half16)
                        {
                            vertexActions.Add(() => positionList.Add(new Vector3((float)vertReader.ReadHalf(), (float)vertReader.ReadHalf(), (float)vertReader.ReadHalf())));
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
                                vertexActions.Add(() => normalList.Add(new Vector3(vertReader.ReadSingle(), vertReader.ReadSingle(), vertReader.ReadSingle())));
                                used = sizeof(float) * 3;
                            }
                            else if (format is VertexAttributeFormat.Half16)
                            {
                                vertexActions.Add(() => normalList.Add(new Vector3((float)vertReader.ReadHalf(), (float)vertReader.ReadHalf(), (float)vertReader.ReadHalf())));
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
                                vertexActions.Add(() => tangentList.Add(new Vector4(vertReader.ReadSingle(), vertReader.ReadSingle(), vertReader.ReadSingle(), vertReader.ReadSingle())));
                                used = sizeof(float) * 4;
                            }
                            else if (format is VertexAttributeFormat.Half16)
                            {
                                vertexActions.Add(() =>
                                    tangentList.Add(new Vector4((float)vertReader.ReadHalf(), (float)vertReader.ReadHalf(), (float)vertReader.ReadHalf(), (float)vertReader.ReadHalf())));
                                used = 2 * 4; //sizeof(Half) * 4
                            }
                            else if (format is VertexAttributeFormat.UNorm8)
                            {
                                float Read() => Mathf.Remap(vertReader.ReadByte(), byte.MinValue, byte.MaxValue, -1f, 1f);
                                vertexActions.Add(() => tangentList.Add(new Vector4(Read(), Read(), Read(), Read())));
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
                                vertexActions.Add(() => colorList.Add(new Color(vertReader.ReadSingle(), vertReader.ReadSingle(), vertReader.ReadSingle(), vertReader.ReadSingle())));
                                used = sizeof(float) * 4;
                            }
                            else if (format is VertexAttributeFormat.Half16)
                            {
                                vertexActions.Add(() =>
                                    colorList.Add(new Color((float)vertReader.ReadHalf(), (float)vertReader.ReadHalf(), (float)vertReader.ReadHalf(), (float)vertReader.ReadHalf())));
                                used = 2 * 4; //sizeof(Half) * 4
                            }
                            else if (format is VertexAttributeFormat.UNorm8)
                            {
                                vertexActions.Add(() => colorList.Add(Color.Color8(vertReader.ReadByte(), vertReader.ReadByte(), vertReader.ReadByte(), vertReader.ReadByte())));
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
                                vertexActions.Add(() => list.Add(new Vector2(vertReader.ReadSingle(), vertReader.ReadSingle())));
                                used = sizeof(float) * 2;
                            }
                            else if (format is VertexAttributeFormat.Half16)
                            {
                                vertexActions.Add(() => list.Add(new Vector2((float)vertReader.ReadHalf(), (float)vertReader.ReadHalf())));
                                used = 2 * 2; //sizeof(Half) * 2
                            }
                            else if (format is VertexAttributeFormat.UNorm8)
                            {
                                float Read() => Mathf.Remap(vertReader.ReadByte(), byte.MinValue, byte.MaxValue, 0f, 1f);
                                vertexActions.Add(() => list.Add(new Vector2(Read(), Read())));
                                used = 2;
                            }
                            else if (format is VertexAttributeFormat.UNorm16)
                            {
                                float Read() => Mathf.Remap(vertReader.ReadUInt16(), ushort.MinValue, ushort.MaxValue, 0f, 1f);
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
                            vertexActions.Add(use8Bones ? () => weightList.AddRange(0, 0, 0, 0, 0, 0, 0, 0) : () => weightList.AddRange(0, 0, 0, 0));
                            break;
                        }
                        Func<float> readMethod = format switch
                        {
                            VertexAttributeFormat.Float32 => () => vertReader.ReadSingle(),
                            VertexAttributeFormat.Half16 => () => (float)vertReader.ReadHalf(),
                            VertexAttributeFormat.UNorm8 => () => Mathf.Remap(vertReader.ReadByte(), byte.MinValue, byte.MaxValue, 0f, 1f),
                            VertexAttributeFormat.UNorm16 => () => Mathf.Remap(vertReader.ReadUInt16(), ushort.MinValue, ushort.MaxValue, 0f, 1f),
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
                            vertexActions.Add(use8Bones ? () => boneIndexList.AddRange(-1, -1, -1, -1, -1, -1, -1, -1) : () => boneIndexList.AddRange(-1, -1, -1, -1));
                            break;
                        }
                        Func<int> readMethod = format switch
                        {
                            VertexAttributeFormat.SInt8 => () => vertReader.ReadSByte(),
                            VertexAttributeFormat.SInt16 => () => vertReader.ReadInt16(),
                            VertexAttributeFormat.SInt32 => () => vertReader.ReadInt32(),
                            VertexAttributeFormat.UInt8 => () => vertReader.ReadByte(),
                            VertexAttributeFormat.UInt16 => () => vertReader.ReadUInt16(),
                            VertexAttributeFormat.UInt32 => () => (int)vertReader.ReadUInt32(), //TODO is this correct?
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
                        1 => () => vertReader.ReadByte(),
                        2 => () => vertReader.ReadInt16(),
                        4 => () => vertReader.ReadInt32(),
                        _ => () =>
                        {
                            for (var i = 0; i < toRead; i++) vertReader.ReadByte();
                        },
                    };
                    vertexActions.Add(method);
                }
            }

            #endregion

            for (var i = 0; i < vertCount; i++)
                foreach (var action in vertexActions)
                    action.Invoke();

            vertReader.Dispose();
            vertexMem.Dispose();

            int[] indexBuffer;
            if (meshBuffer.IndexBufferFormat is IndexBufferFormat.UInt16)
            {
                //uint16
                var buffer = meshBuffer.GetIndexBufferUInt16();
                indexBuffer = new int[buffer.Length];
                for (var index = 0; index < buffer.Length; index++) indexBuffer[index] = buffer[index];
            }
            else
            {
                //uint32
                var buffer = meshBuffer.GetIndexBufferUInt32();
                indexBuffer = new int[buffer.Length];
                for (var index = 0; index < buffer.Length; index++) indexBuffer[index] = (int)buffer[index];
            }

            var indexList = new List<int[]>();
            var typeList = new List<RenderingServer.PrimitiveType>();

            if (meshBuffer.Submeshes is not null)
            {
                foreach (var submesh in meshBuffer.Submeshes)
                {
                    indexList.Add(submesh.indexCount > 0 ? indexBuffer.Skip(submesh.indexStart).Take(submesh.indexCount).ToArray() : [0, 0, 0]);
                    typeList.Add(submesh.topology.ToGodot());
                }
            }

            var baseArray = new Array();
            baseArray.Resize((int)Mesh.ArrayType.Max);

            if (usesPosition) baseArray[(int)Mesh.ArrayType.Vertex] = positionList.ToArray();
            if (usesNormal) baseArray[(int)Mesh.ArrayType.Normal] = normalList.ToArray();
            if (usesTangent) baseArray[(int)Mesh.ArrayType.Tangent] = tangentList.SelectMany(i => new[] { i.X, i.Y, i.Z, i.W }).ToArray();
            if (usesColor) baseArray[(int)Mesh.ArrayType.Color] = colorList.ToArray();
            if (usesUV0) baseArray[(int)Mesh.ArrayType.TexUV] = uv0List.ToArray();
            if (usesUV1) baseArray[(int)Mesh.ArrayType.TexUV2] = uv1List.ToArray();
            //TODO: custom channels for other UVs
            if (usesBoneIndices) baseArray[(int)Mesh.ArrayType.Bones] = boneIndexList.ToArray();
            if (usesBoneWeights) baseArray[(int)Mesh.ArrayType.Weights] = weightList.ToArray();

            var form = RenderingServer.ArrayFormat.FlagFormatCurrentVersion;
            if (usesPosition) form |= RenderingServer.ArrayFormat.FormatVertex;
            if (usesNormal) form |= RenderingServer.ArrayFormat.FormatNormal;
            if (usesTangent) form |= RenderingServer.ArrayFormat.FormatTangent;
            if (usesColor) form |= RenderingServer.ArrayFormat.FormatColor;
            if (usesUV0) form |= RenderingServer.ArrayFormat.FormatTexUV;
            if (usesUV1) form |= RenderingServer.ArrayFormat.FormatTexUV2;
            if (usesBoneIndices && usesBoneWeights) form |= RenderingServer.ArrayFormat.FormatBones;
            if (use8Bones) form |= RenderingServer.ArrayFormat.FlagUse8BoneWeights;

            //TODO blendshapes

            for (var i = 0; i < indexList.Count; i++)
            {
                var arr = baseArray.Duplicate();
                arr[(int)Mesh.ArrayType.Index] = indexList[i];
                var type = typeList[i];
                RenderingServer.MeshAddSurfaceFromArrays(AssetID, type, arr, null, null, form);
            }
            
            end:

            var bindPoses = meshBuffer.GetBindPosesBuffer<RenderMatrix4x4>();
            Skin = new Transform3D[bindPoses.Length];
            for (var i = 0; i < bindPoses.Length; i++) Skin[i] = bindPoses[i].ToGodot().AffineInverse(); //TODO

            MeshChanged.Invoke();
        }
        catch (Exception e)
        {
            GD.Print($"Mesh upload error: {e}");
        }
    }
}
