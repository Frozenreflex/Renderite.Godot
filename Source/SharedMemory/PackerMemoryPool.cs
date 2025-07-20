using System.Collections.Concurrent;
using Renderite.Shared;

namespace Renderite.Godot.Source.SharedMemory;

public class PackerMemoryPool : IMemoryPackerEntityPool
{
    public static readonly PackerMemoryPool Instance = new();

    public T Borrow<T>() where T : class, IMemoryPackable, new() => PackerMemoryPool<T>.Borrow();
    public void Return<T>(T value) where T : class, IMemoryPackable, new() => PackerMemoryPool<T>.Return(value);
}
public static class PackerMemoryPool<T> where T : class, IMemoryPackable, new()
{
    private static readonly ConcurrentStack<T> Instances = new();

    public static T Borrow() => Instances.TryPop(out var value) ? value : new T();
    public static void Return(T instance) => Instances.Push(instance);
}