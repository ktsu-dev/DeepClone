// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone;

/// <summary>
/// Extension methods for deep cloning collections of objects.
/// </summary>
/// <remarks>
/// These methods provide efficient ways to deep clone various collection types.
/// They automatically handle deep cloning of elements that implement IDeepCloneable,
/// while preserving non-cloneable elements unchanged.
/// 
/// Performance tip: Use DeepCloneFrom for in-place cloning when you already have a destination collection.
/// </remarks>
public static class DeepCloneContainerExtensions
{
	/// <summary>
	/// Deep clones items from a source collection into a destination collection.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collections.</typeparam>
	/// <param name="dest">The destination collection to clone into.</param>
	/// <param name="source">The source collection to clone from.</param>
	/// <remarks>
	/// This is an in-place operation that modifies the destination collection. 
	/// The destination collection is first cleared, then filled with deep clones
	/// of the items from the source collection.
	/// 
	/// Example usage:
	/// <code>
	/// // Create a new list and clone into it
	/// var clonedList = new List&lt;MyType&gt;();
	/// clonedList.DeepCloneFrom(originalList);
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	public static void DeepCloneFrom<T>(this ICollection<T> dest, IEnumerable<T> source)
	{
		ArgumentNullException.ThrowIfNull(dest);
		ArgumentNullException.ThrowIfNull(source);

		dest.Clear();
		foreach (var item in source)
		{
			dest.Add(DeepClone(item));
		}
	}

	/// <summary>
	/// Deep clones items from a source dictionary into a destination dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dest">The destination dictionary to clone into.</param>
	/// <param name="source">The source dictionary to clone from.</param>
	/// <remarks>
	/// This is an in-place operation that modifies the destination dictionary.
	/// The destination dictionary is first cleared, then filled with deep clones
	/// of the key-value pairs from the source dictionary. Both keys and values
	/// are deep cloned if they implement IDeepCloneable.
	/// 
	/// Example usage:
	/// <code>
	/// // Create a new dictionary and clone into it
	/// var clonedDict = new Dictionary&lt;KeyType, ValueType&gt;();
	/// clonedDict.DeepCloneFrom(originalDict);
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	public static void DeepCloneFrom<TKey, TValue>(this IDictionary<TKey, TValue> dest, IDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		ArgumentNullException.ThrowIfNull(dest);
		ArgumentNullException.ThrowIfNull(source);

		dest.Clear();
		foreach (var pair in source)
		{
			dest.Add(DeepClone(pair.Key), DeepClone(pair.Value));
		}
	}

	/// <summary>
	/// Helper method to deep clone an object if it implements IDeepCloneable, otherwise returns the object unchanged.
	/// </summary>
	/// <typeparam name="T">The type of object to clone.</typeparam>
	/// <param name="source">The source object to clone.</param>
	/// <returns>A deep clone of the source object if it implements IDeepCloneable, otherwise the source object itself.</returns>
	/// <remarks>
	/// This internal method is used by all other extension methods to handle deep cloning
	/// of individual elements. It checks if the object implements IDeepCloneable and calls
	/// DeepClone() if it does, otherwise it returns the original object.
	/// </remarks>
	private static T DeepClone<T>(T source) => source == null ? default! : source is IDeepCloneable cloneable ? (T)cloneable.DeepClone() : source;

	/// <summary>
	/// Deep clones a collection of objects.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collection.</typeparam>
	/// <param name="source">The source collection to clone.</param>
	/// <returns>A new collection containing deep clones of the original items if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// This method returns an IEnumerable sequence of cloned items. To get a specific collection type,
	/// you'll need to convert the result (for example using ToList() or ToArray()).
	/// 
	/// Example usage:
	/// <code>
	/// var clonedList = originalList.DeepClone().ToList();
	/// var clonedArray = originalArray.DeepClone().ToArray();
	/// </code>
	/// </remarks>
	public static IEnumerable<T> DeepClone<T>(this IEnumerable<T> source) =>
		source.Select(DeepClone);

	/// <summary>
	/// Deep clones a dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new dictionary containing deep clones of the keys and values if they implement IDeepCloneable,
	/// otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// This method returns a new Dictionary with cloned key-value pairs. Both keys and values
	/// are deep cloned if they implement IDeepCloneable.
	/// 
	/// Example usage:
	/// <code>
	/// var clonedDict = originalDict.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static IDictionary<TKey, TValue> DeepClone<TKey, TValue>(this IDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		ArgumentNullException.ThrowIfNull(source);
		return source.ToDictionary(
			pair => DeepClone(pair.Key),
			pair => DeepClone(pair.Value));
	}

	/// <summary>
	/// Deep clones a read-only dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new read-only dictionary containing deep clones of the keys and values if they implement IDeepCloneable,
	/// otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// This method returns a new read-only dictionary with cloned key-value pairs.
	/// 
	/// Example usage:
	/// <code>
	/// var clonedReadOnlyDict = originalReadOnlyDict.DeepClone();
	/// </code>
	/// </remarks>
	public static IReadOnlyDictionary<TKey, TValue> DeepClone<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
		where TKey : notnull =>
		(IReadOnlyDictionary<TKey, TValue>)source.ToDictionary(DeepClone, DeepClone);

	/// <summary>
	/// Deep clones a hash set.
	/// </summary>
	/// <typeparam name="T">The type of objects in the set.</typeparam>
	/// <param name="source">The source set to clone.</param>
	/// <returns>A new hash set containing deep clones of the original items if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the hash set's comparer when creating the clone.
	/// 
	/// Example usage:
	/// <code>
	/// var clonedHashSet = originalHashSet.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static HashSet<T> DeepClone<T>(this HashSet<T> source)
	{
		ArgumentNullException.ThrowIfNull(source);
		return new(source.Select(DeepClone), source.Comparer);
	}

	/// <summary>
	/// Deep clones a sorted set.
	/// </summary>
	/// <typeparam name="T">The type of objects in the set.</typeparam>
	/// <param name="source">The source set to clone.</param>
	/// <returns>A new sorted set containing deep clones of the original items if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the sorted set's comparer when creating the clone.
	/// 
	/// Example usage:
	/// <code>
	/// var clonedSortedSet = originalSortedSet.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static SortedSet<T> DeepClone<T>(this SortedSet<T> source)
	{
		ArgumentNullException.ThrowIfNull(source);
		return new(source.Select(DeepClone), source.Comparer);
	}

	/// <summary>
	/// Deep clones a stack.
	/// </summary>
	/// <typeparam name="T">The type of objects in the stack.</typeparam>
	/// <param name="source">The source stack to clone.</param>
	/// <returns>A new stack containing deep clones of the original items in the same order if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the order of elements in the stack.
	/// 
	/// Example usage:
	/// <code>
	/// var clonedStack = originalStack.DeepClone();
	/// </code>
	/// </remarks>
	public static Stack<T> DeepClone<T>(this Stack<T> source) =>
		new(source.Reverse().Select(DeepClone));
}
