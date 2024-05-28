using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TOHE;

// Credit: Endless Host Roles by Gurge44
// Reference: https://github.com/Gurge44/EndlessHostRoles/blob/main/Modules/Extensions/CollectionExtensions.cs
public static class CollectionExtensions
{
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
    /// Executes an action for each element in a collection in parallel
    /// </summary>
    /// <param name="collection">The collection to iterate over</param>
    /// <param name="action">The action to execute for each element</param>
    /// <typeparam name="T">The type of the elements in the collection</typeparam>
    public static void Do<T>(this IEnumerable<T> collection, System.Action<T> action)
    {
        Parallel.ForEach(collection, action);
    }

    /// <summary>
    /// Executes an action for each element in a collection in parallel
    /// </summary>
    /// <param name="collection">The collection to iterate over</param>
    /// <param name="action">The action to execute for each element</param>
    /// <typeparam name="T">The type of the elements in the collection</typeparam>
    public static void Do<T>(this ParallelQuery<T> collection, System.Action<T> action)
    {
        collection.ForAll(action);
    }

    /// <summary>
    /// Executes an action for each element in a collection in parallel if the predicate is true
    /// </summary>
    /// <param name="collection">The collection to iterate over</param>
    /// <param name="predicate">The predicate to check for each element</param>
    /// <param name="action">The action to execute for each element that satisfies the predicate</param>
    /// <typeparam name="T">The type of the elements in the collection</typeparam>
    public static void DoIf<T>(this IEnumerable<T> collection, System.Func<T, bool> predicate, System.Action<T> action)
    {
        var partitioner = Partitioner.Create(collection.Where(predicate));
        Parallel.ForEach(partitioner, action);
    }

    /// <summary>
    /// Executes an action for each element in a collection in parallel if the predicate is true
    /// </summary>
    /// <param name="collection">The collection to iterate over</param>
    /// <param name="predicate">The predicate to check for each element</param>
    /// <param name="action">The action to execute for each element that satisfies the predicate</param>
    /// <typeparam name="T">The type of the elements in the collection</typeparam>
    public static void DoIf<T>(this ParallelQuery<T> collection, System.Func<T, bool> predicate, System.Action<T> action)
    {
        collection.Where(predicate).ForAll(action);
    }

    /// <summary>
    /// Removes an element from a collection
    /// </summary>
    /// <param name="collection">The collection to remove the element from</param>
    /// <param name="element">The element to remove</param>
    /// <typeparam name="T">The type of the elements in the collection</typeparam>
    /// <returns>A collection containing all elements of <paramref name="collection"/> except for <paramref name="element"/></returns>
    public static ParallelQuery<T> Remove<T>(this IEnumerable<T> collection, T element)
    {
        return collection.AsParallel().Where(x => !x.Equals(element));
    }


    /// <summary>
    /// Filters a Delegate HashSet of any object reference duplicates
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegates in the collection</typeparam>
    /// <returns>A HashSet containing all delegates without duplicate object references.</returns>
    public static HashSet<TDelegate> FilterDuplicates<TDelegate>(this HashSet<TDelegate> collection) where TDelegate : Delegate
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
    /// Return the first byte of a HashSet(Byte)
    /// </summary>
    public static byte First(this HashSet<byte> source)
        => source.ToArray().First();
}
