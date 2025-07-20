using System;
using Renderite.Shared;

namespace Renderite.Godot.Source.SharedMemory;

public class SharedMemoryManager
{
    public static SharedMemoryManager Instance;

    public Span<T> Read<T>(SharedMemoryBufferDescriptor<T> descriptor) where T : unmanaged
    {
        //TODO im too stupid for this
        return default;
    }
}
