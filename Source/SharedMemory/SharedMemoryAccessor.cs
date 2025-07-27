using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Renderite.Shared;

namespace Renderite.Godot.Source.SharedMemory;

// NOTE: decompiled code
public class SharedMemoryAccessor
{
    public static SharedMemoryAccessor Instance;

    private Dictionary<int, SharedMemoryView> _views = new Dictionary<int, SharedMemoryView>();

    public string Prefix { get; private set; }

    public SharedMemoryAccessor(string prefix)
    {
        Prefix = prefix;
        Instance = this;
    }

    public Span<T> AccessData<T>(SharedMemoryBufferDescriptor<T> descriptor) where T : unmanaged
    {
        return MemoryMarshal.Cast<byte, T>(GetMemoryView(descriptor).RawData.Slice(descriptor.offset, descriptor.length));
    }

    public UnmanagedSpan<T> AccessDataUnmanaged<T>(SharedMemoryBufferDescriptor<T> descriptor) where T : unmanaged
    {
        return GetMemoryView(descriptor).UnmanagedRawData.Slice(descriptor.offset, descriptor.length).As<T>();
    }

    public SharedMemoryViewSlice<T> AccessSlice<T>(SharedMemoryBufferDescriptor<T> descriptor) where T : unmanaged
    {
        return new SharedMemoryViewSlice<T>(GetMemoryView(descriptor), descriptor.offset, descriptor.length);
    }

    private SharedMemoryView GetMemoryView<T>(SharedMemoryBufferDescriptor<T> descriptor) where T : unmanaged
    {
        if (!_views.TryGetValue(descriptor.bufferId, out var value))
        {
            value = new SharedMemoryView(this, descriptor.bufferId, descriptor.bufferCapacity);
            _views.Add(descriptor.bufferId, value);
        }
        return value;
    }

    public void ReleaseView(int bufferId)
    {
        if (_views.TryGetValue(bufferId, out var value))
        {
            value.Dispose();
            _views.Remove(bufferId);
        }
    }
}
