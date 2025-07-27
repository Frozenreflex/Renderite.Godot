using System;
using Cloudtoid.Interprocess;
using Renderite.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace Renderite.Godot.Source.SharedMemory;

// NOTE: decompiled code
public class SharedMemoryView : IDisposable
{
    private MemoryView _view;

    private UnmanagedMemoryManager<byte> _memory;

    private long _capacity;

    public SharedMemoryAccessor Accessor { get; private set; }

    public int BufferId { get; private set; }

    public Span<byte> RawData => _view.Data;

    public Memory<byte> Memory => _memory.Memory;

    public unsafe UnmanagedSpan<byte> UnmanagedRawData => new(_view.Pointer, (int)_capacity);

    public unsafe SharedMemoryView(SharedMemoryAccessor accessor, int bufferId, long capacity)
    {
        Accessor = accessor;
        BufferId = bufferId;
        _capacity = capacity;
        var memoryViewName = Helper.ComposeMemoryViewName(accessor.Prefix, bufferId);
        _view = new MemoryView(new MemoryViewOptions(memoryViewName, capacity), new NullLoggerFactory());
        _memory = new UnmanagedMemoryManager<byte>(_view.Pointer, (int)capacity);
    }

    public void Dispose()
    {
        _view.Dispose();
        _view = null;
    }
}
