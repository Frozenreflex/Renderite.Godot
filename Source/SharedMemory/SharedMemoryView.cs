using System;
using Cloudtoid.Interprocess;
using Renderite.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace Renderite.Godot.Source.SharedMemory;

// NOTE: decompiled code
public class SharedMemoryView : IDisposable
{
    private MemoryView view;

    private UnmanagedMemoryManager<byte> memory;

    private long capacity;

    public SharedMemoryAccessor Accessor { get; private set; }

    public int BufferId { get; private set; }

    public Span<byte> RawData => view.Data;

    public Memory<byte> Memory => memory.Memory;

    public unsafe UnmanagedSpan<byte> UnmanagedRawData => new UnmanagedSpan<byte>(view.Pointer, (int)capacity);

    public unsafe SharedMemoryView(SharedMemoryAccessor accessor, int bufferId, long capacity)
    {
        Accessor = accessor;
        BufferId = bufferId;
        this.capacity = capacity;
        string memoryViewName = Renderite.Shared.Helper.ComposeMemoryViewName(accessor.Prefix, bufferId);
        view = new MemoryView(new MemoryViewOptions(memoryViewName, capacity), new NullLoggerFactory());
        memory = new UnmanagedMemoryManager<byte>(view.Pointer, (int)capacity);
    }

    public void Dispose()
    {
        view.Dispose();
        view = null;
    }
}
