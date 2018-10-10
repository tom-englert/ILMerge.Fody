namespace ILMerge.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    internal static class ExtensionMethods
    {
        public static void RemoveAll<T>([NotNull, ItemCanBeNull] this ICollection<T> target, [NotNull] Func<T, bool> condition)
        {
            target.RemoveAll(target.Where(condition).ToArray());
        }

        public static void RemoveAll<T>([NotNull, ItemCanBeNull] this ICollection<T> target, [NotNull, ItemCanBeNull] IEnumerable<T> items)
        {
            foreach (var i in items)
            {
                target.Remove(i);
            }
        }
    }
}
