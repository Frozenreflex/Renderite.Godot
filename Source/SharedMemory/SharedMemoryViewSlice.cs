using System;
using System.Runtime.InteropServices;
using Renderite.Shared;

namespace Renderite.Godot.Source.SharedMemory;

// NOTE: decompiled code
public class SharedMemoryViewSlice<T> : BackingMemoryBuffer where T : unmanaged
{
    private int _sizeBytes;

    public SharedMemoryView SharedView { get; private set; }

    public int OffsetBytes { get; private set; }

    public override int SizeBytes => _sizeBytes;

    public override Span<byte> RawData => SharedView.RawData.Slice(OffsetBytes, SizeBytes);

    public Span<T> Data => MemoryMarshal.Cast<byte, T>(RawData);

    public override Memory<byte> Memory => SharedView.Memory.Slice(OffsetBytes, SizeBytes);

    public SharedMemoryViewSlice(SharedMemoryView view, int offset, int size)
    {
        SharedView = view;
        OffsetBytes = offset;
        _sizeBytes = size;
    }

    protected override void ActuallyDispose()
    {
        SharedView = null;
    }
}
