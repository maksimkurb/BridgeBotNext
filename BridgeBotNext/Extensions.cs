using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBotNext
{
    public static class Extensions
    {
        /**
         * @author Matt Greer
         * @url https://stackoverflow.com/a/5047370
         */
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }

        public static IEnumerable<(T Item, int Level)> PostOrderFlatten<T>(this IEnumerable<T> source,
            Func<T, IEnumerable<T>> selector, int startLevel = 0)
        {
            List<(T Item, int Level)> list = new List<(T Item, int Level)>();

            foreach (var it in source)
            {
                var descendants = selector(it);
                if (!descendants.IsNullOrEmpty())
                {
                    list.AddRange(descendants.PostOrderFlatten(selector, startLevel + 1));
                }

                list.Add((it, startLevel));
            }

            return list;
        }

        /**
         * @author Ivan Stoev
         * @url https://stackoverflow.com/a/31881243
         */
        public static IEnumerable<T> Expand<T>(
            this IEnumerable<T> source, Func<T, IEnumerable<T>> elementSelector) =>
            source.ExpandWithLevel(elementSelector).Select(e => e.Item);

        /**
         * @author Ivan Stoev
         * @url https://stackoverflow.com/a/31881243
         */
        public static IEnumerable<(T Item, int Level)> ExpandWithLevel<T>(
            this IEnumerable<T> source, Func<T, IEnumerable<T>> elementSelector)
        {
            var stack = new Stack<IEnumerator<T>>();
            var e = source.GetEnumerator();
            try
            {
                while (true)
                {
                    while (e.MoveNext())
                    {
                        var item = e.Current;
                        yield return (item, stack.Count);
                        var elements = elementSelector(item);
                        if (elements == null) continue;
                        stack.Push(e);
                        e = elements.GetEnumerator();
                    }

                    if (stack.Count == 0) break;
                    e.Dispose();
                    e = stack.Pop();
                }
            }
            finally
            {
                e.Dispose();
                while (stack.Count != 0) stack.Pop().Dispose();
            }
        }
    }
}