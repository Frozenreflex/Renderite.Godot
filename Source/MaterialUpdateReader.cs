using System;
using System.Collections.Generic;
using Godot;
using Renderite.Godot.Source.SharedMemory;
using Renderite.Shared;

#nullable enable
namespace Renderite.Godot.Source;

//decomped code
//TODO: i fucking hate this, we should eventually replace it
public ref struct MaterialUpdateReader(MaterialsUpdateBatch batch, BitSpan instanceChangedBuffer)
{
    private int _instanceChangedIndex;
    private BitSpan _instanceChangedBuffer = instanceChangedBuffer;
    private Span<MaterialPropertyUpdate> _updateBuffer;
    private Span<int> _intBuffer;
    private Span<float> _floatBuffer;
    private Span<Vector4> _vectorBuffer;
    private Span<Projection> _matrixBuffer;
    private int _updateBufferIndex;
    private int _intBufferIndex;
    private int _floatBufferIndex;
    private int _vectorBufferIndex;
    private int _matrixBufferIndex;
    private int _updateIndex;
    private int _intIndex;
    private int _floatIndex;
    private int _vectorIndex;
    private int _matrixIndex;

    public bool HasNextUpdate => _updateIndex != _updateBuffer.Length
        ? _updateBuffer[_updateIndex].updateType != MaterialPropertyUpdateType.UpdateBatchEnd
        : _updateBufferIndex != batch.materialUpdates.Count;

    public void WriteInstanceChanged(bool instanceChanged) => _instanceChangedBuffer[_instanceChangedIndex++] = instanceChanged;

    //public MaterialPropertyUpdate ReadUpdate() => ReadValue(ref _updateBufferIndex, ref _updateIndex, ref _updateBuffer, batch.materialUpdates);
    public MaterialPropertyUpdate ReadUpdate()
    {
        if (_updateIndex == _updateBuffer.Length)
        {
            if (_updateBufferIndex >= batch.materialUpdates.Count)
                throw new Exception();
            var bufferDescriptor = batch.materialUpdates[_updateBufferIndex++];
            _updateIndex = 0;
            _updateBuffer = SharedMemoryAccessor.Instance.AccessData(bufferDescriptor.As<MaterialPropertyUpdate>());
        }
        return _updateBuffer[_updateIndex++];
    }

    //public int ReadInt() => ReadValue(ref _intBufferIndex, ref _intIndex, ref _intBuffer, batch.intBuffers);
    public int ReadInt()
    {
        if (_intIndex == _intBuffer.Length)
        {
            if (_intBufferIndex >= batch.intBuffers.Count)
                throw new Exception();
            var bufferDescriptor = batch.intBuffers[_intBufferIndex++];
            _intIndex = 0;
            _intBuffer = SharedMemoryAccessor.Instance.AccessData(bufferDescriptor.As<int>());
        }
        return _intBuffer[_intIndex++];
    }

    //public float ReadFloat() => ReadValue(ref _floatBufferIndex, ref _floatIndex, ref _floatBuffer, batch.floatBuffers);
    public float ReadFloat()
    {
        if (_floatIndex == _floatBuffer.Length)
        {
            if (_floatBufferIndex >= batch.floatBuffers.Count)
                throw new Exception();
            var bufferDescriptor = batch.floatBuffers[_floatBufferIndex++];
            _floatIndex = 0;
            _floatBuffer = SharedMemoryAccessor.Instance.AccessData(bufferDescriptor.As<float>());
        }
        return _floatBuffer[_floatIndex++];
    }

    //public Vector4 ReadVector() => ReadValue(ref _vectorBufferIndex, ref _vectorIndex, ref _vectorBuffer, batch.float4Buffers);
    public Vector4 ReadVector()
    {
        if (_vectorIndex == _vectorBuffer.Length)
        {
            if (_vectorBufferIndex >= batch.float4Buffers.Count)
                throw new Exception();
            var bufferDescriptor = batch.float4Buffers[_vectorBufferIndex++];
            _vectorIndex = 0;
            _vectorBuffer = SharedMemoryAccessor.Instance.AccessData(bufferDescriptor.As<Vector4>());
        }
        return _vectorBuffer[_vectorIndex++];
    }

    //public Projection ReadMatrix() => ReadValue(ref _matrixBufferIndex, ref _matrixIndex, ref _matrixBuffer, batch.matrixBuffers);
    public Projection ReadMatrix()
    {
        if (_matrixIndex == _matrixBuffer.Length)
        {
            if (_matrixBufferIndex >= batch.matrixBuffers.Count)
                throw new Exception();
            var bufferDescriptor = batch.matrixBuffers[_matrixBufferIndex++];
            _matrixIndex = 0;
            _matrixBuffer = SharedMemoryAccessor.Instance.AccessData(bufferDescriptor.As<Projection>());
        }
        return _matrixBuffer[_matrixIndex++];
    }

    //public unsafe Span<float> AccessFloatArray() => AccessArray(ref _floatBufferIndex, ref _floatIndex, ref _floatBuffer, batch.floatBuffers);
    public Span<float> AccessFloatArray()
    {
        var length = ReadInt();
        if (length + _floatIndex >= _floatBuffer.Length)
        {
            if (_floatBufferIndex >= batch.floatBuffers.Count)
                throw new Exception();
            var bufferDescriptor = batch.floatBuffers[_floatBufferIndex++];
            _floatIndex = 0;
            _floatBuffer = SharedMemoryAccessor.Instance.AccessData(bufferDescriptor.As<float>());
        }
        var span = _floatBuffer.Slice(_floatIndex, length);
        _floatIndex += length;
        return span;
    }

    //public unsafe Span<Vector4> AccessVectorArray() => AccessArray(ref _vectorBufferIndex, ref _vectorIndex, ref _vectorBuffer, batch.float4Buffers);
    public Span<Vector4> AccessVectorArray()
    {
        var length = ReadInt();
        if (length + _vectorIndex >= _vectorBuffer.Length)
        {
            if (_vectorBufferIndex >= batch.float4Buffers.Count)
                throw new Exception();
            var bufferDescriptor = batch.float4Buffers[_vectorBufferIndex++];
            _vectorIndex = 0;
            _vectorBuffer = SharedMemoryAccessor.Instance.AccessData(bufferDescriptor.As<Vector4>());
        }
        var span = _vectorBuffer.Slice(_vectorIndex, length);
        _vectorIndex += length;
        return span;
    }

    private unsafe T ReadValue<T, S>(
        ref int bufferIndex,
        ref int valueIndex,
        ref Span<T> buffer,
        List<SharedMemoryBufferDescriptor<S>> list)
        where T : unmanaged
        where S : unmanaged
    {
        if (valueIndex == buffer.Length)
            buffer = FetchNextBuffer<T, S>(ref bufferIndex, ref valueIndex, list);
        return buffer[valueIndex++];
    }

    private unsafe Span<T> AccessArray<T, S>(
        ref int bufferIndex,
        ref int valueIndex,
        ref Span<T> buffer,
        List<SharedMemoryBufferDescriptor<S>> list)
        where T : unmanaged
        where S : unmanaged
    {
        var length = ReadInt();
        if (length + valueIndex >= buffer.Length)
            buffer = FetchNextBuffer<T, S>(ref bufferIndex, ref valueIndex, list);
        var span = buffer.Slice(valueIndex, length);
        valueIndex += length;
        return span;
    }

    private Span<T> FetchNextBuffer<T, S>(
        ref int bufferIndex,
        ref int valueIndex,
        List<SharedMemoryBufferDescriptor<S>> list)
        where T : unmanaged
        where S : unmanaged
    {
        if (bufferIndex >= list.Count)
            throw new InvalidOperationException($"Next buffer of type {typeof(T)} does not exist!");
        var bufferDescriptor = list[bufferIndex++];
        valueIndex = 0;
        return SharedMemoryAccessor.Instance.AccessData(bufferDescriptor.As<T>());
    }
}
