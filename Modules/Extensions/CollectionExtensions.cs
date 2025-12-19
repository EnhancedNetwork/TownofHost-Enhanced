using System;

namespace TOHE;

// Credit: Endless Host Roles by Gurge44
// Reference: https://github.com/Gurge44/EndlessHostRoles/blob/main/Modules/Extensions/CollectionExtensions.cs
public static class CollectionExtensions
{
    /// <summary>
    /// Returns the key of a dictionary by its value
    /// </summary>
    /// <param name="dictionary">The <see cref="Dictionary{TKey,TValue}"/> to search</param>
    /// <param name="value">The <typeparamref name="TValue"/> used to search for the corresponding key</param>
    /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/></typeparam>
    /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/></typeparam>
    /// <returns>The key of the <paramref name="dictionary"/> that corresponds to the given <paramref name="value"/>, or the default value of <typeparamref name="TKey"/> if the <paramref name="value"/> is not found in the <paramref name="dictionary"/></returns>
    public static TKey GetKeyByValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value)
    {
        foreach (KeyValuePair<TKey, TValue> pair in dictionary)
        {
            if (pair.Value.Equals(value))
            {
                return pair.Key;
            }
        }

        return default;
    }
    /// <summary>
    /// Returns a random element from a collection
    /// </summary>
    /// <param name="collection">The collection</param>
    /// <typeparam name="T">The type of the collection</typeparam>
    /// <returns>A random element from the collection, or the default value of <typeparamref name="T"/> if the collection is empty</returns>
    public static T RandomElement<T>(this IList<T> collection)
    {
        if (collection.Count == 0) return default;
        return collection[IRandom.Instance.Next(collection.Count)];
    }
    public static T RandomElement<T>(this IEnumerable<T> collection)
    {
        if (collection is IList<T> list) return list.RandomElement();

        return collection.ToList().RandomElement();
    }
    /// <summary>
    /// Combines multiple collections into a single collection
    /// </summary>
    /// <param name="firstCollection">The collection to start with</param>
    /// <param name="collections">The other collections to add to <paramref name="firstCollection"/></param>
    /// <typeparam name="T">The type of the elements in the collections to combine</typeparam>
    /// <returns>A collection containing all elements of <paramref name="firstCollection"/> and all <paramref name="collections"/></returns>
    public static IEnumerable<T> CombineWith<T>(this IEnumerable<T> firstCollection, params IEnumerable<T>[] collections)
    {
        return firstCollection.Concat(collections.SelectMany(x => x));
    }

    /// <summary>
    /// Shuffles all elements in a collection randomly
    /// </summary>
    /// <typeparam name="T">The type of the collection</typeparam>
    /// <param name="collection">The collection to be shuffled</param>
    /// <param name="random">An instance of a randomizer algorithm</param>
    /// <returns>The shuffled collection</returns>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection, IRandom random)
    {
        var list = collection.ToList();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
        return list;
    }
    /// <summary>
    /// Shuffles all elements in a collection randomly
    /// </summary>
    /// <typeparam name="T">The type of the collection</typeparam>
    /// <param name="collection">The collection to be shuffled</param>
    /// <returns>The shuffled collection</returns>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection)
    {
        var list = collection.ToList();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = IRandom.Instance.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
        return list;
    }

    /// <summary>
    /// Filters a IEnumerable(<typeparamref name="TDelegate"/>) of any duplicates
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegates in the collection</typeparam>
    /// <returns>A HashSet(<typeparamref name="TDelegate"/>) without duplicate object references nor static duplicates.</returns>
    public static HashSet<TDelegate> FilterDuplicates<TDelegate>(this IEnumerable<TDelegate> collection) where TDelegate : Delegate
    {
        // Filter out delegates which do not have a object reference (static methods)
        var filteredCollection = collection.Where(d => d.Target != null);

        // Group by the target object (the instance the method belongs to) and select distinct
        var distinctDelegates = filteredCollection
            .GroupBy(d => d.Target.GetType())
            .Select(g => g.First())
            .Concat(collection.Where(x => x.Target == null)); // adds back static methods

        return distinctDelegates.ToHashSet();
    }

    /// <summary>
    /// Determines whether a collection contains any elements that satisfy a predicate and returns the first element that satisfies the predicate
    /// </summary>
    /// <param name="collection">The collection to search</param>
    /// <param name="predicate">The predicate to check for each element</param>
    /// <param name="element">The first element that satisfies the predicate, or the default value of <typeparamref name="T"/> if no elements satisfy the predicate</param>
    /// <typeparam name="T">The type of the elements in the collection</typeparam>
    /// <returns><c>true</c> if the collection contains any elements that satisfy the predicate, <c>false</c> otherwise</returns>
    public static bool Find<T>(this IEnumerable<T> collection, Func<T, bool> predicate, out T element)
    {
        if (collection is List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                if (predicate(item))
                {
                    element = item;
                    return true;
                }
            }

            element = default;
            return false;
        }

        foreach (T item in collection)
        {
            if (predicate(item))
            {
                element = item;
                return true;
            }
        }

        element = default;
        return false;
    }

    /// <summary>
    ///     Determines whether a collection contains any elements that satisfy a predicate and returns the first element that
    ///     satisfies the predicate
    /// </summary>
    /// <param name="collection">The collection to search</param>
    /// <param name="predicate">The predicate to check for each element</param>
    /// <param name="element">
    ///     The first element that satisfies the predicate, or the default value of <typeparamref name="T" />
    ///     if no elements satisfy the predicate
    /// </param>
    /// <typeparam name="T">The type of the elements in the collection</typeparam>
    /// <returns><c>true</c> if the collection contains any elements that satisfy the predicate, <c>false</c> otherwise</returns>
    public static bool FindFirst<T>(this IEnumerable<T> collection, Func<T, bool> predicate, out T element)
    {
        if (collection is List<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                T item = list[i];

                if (predicate(item))
                {
                    element = item;
                    return true;
                }
            }

            element = default(T);
            return false;
        }

        foreach (T item in collection)
        {
            if (predicate(item))
            {
                element = item;
                return true;
            }
        }

        element = default(T);
        return false;
    }

    /// <summary>
    /// Return the first byte of a HashSet(Byte)
    /// </summary>
    public static byte First(this HashSet<byte> source)
        => source.ToArray().First();
}
