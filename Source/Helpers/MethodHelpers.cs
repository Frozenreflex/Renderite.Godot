using System.Collections.Generic;
using System.Linq;

namespace Renderite.Godot.Source.Helpers;

public static class MethodHelpers
{
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source) => source?.Select((item, index) => (item, index));
    public static T ElementAtOrValue<T>(this IList<T> list, int index, T d) => list.Count > index ? list[index] : d;
}
