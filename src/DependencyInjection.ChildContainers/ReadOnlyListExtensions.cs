namespace Dazinator.Extensions.DependencyInjection.ChildContainers
{
    using System.Collections.Generic;

    public static class ReadOnlyCollectionExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
        {
            var i = 0;
            foreach (var element in self)
            {
                if (Equals(element, elementToFind))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }
    }
}
