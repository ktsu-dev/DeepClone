// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
#if NET
using System.Collections.Immutable;
#endif

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
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		T[] items = [.. source];
		dest.Clear();
		foreach (T? item in items)
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
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		KeyValuePair<TKey, TValue>[] items = [.. source];
		dest.Clear();
		foreach (KeyValuePair<TKey, TValue> pair in items)
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
	///
	/// A <see cref="KeyValuePair{TKey, TValue}"/> is rebuilt from a deep clone of its key and value.
	/// Without that, a dictionary reached only as a sequence of pairs (an <see cref="IReadOnlyDictionary{TKey, TValue}"/>
	/// passed to <see cref="DeepCloneFrom{T}(ICollection{T}, IEnumerable{T})"/>, or LINQ over a dictionary)
	/// came back sharing its keys and values with the source (ktsu-dev/DeepClone#82).
	/// </remarks>
	private static T DeepClone<T>(T source) => source switch
	{
		null => default!,
		IDeepCloneable cloneable => (T)cloneable.DeepClone(),
		_ when PairCloner<T>.Instance is { } pairCloner => pairCloner.Clone(source),
		_ => source,
	};

	/// <summary>
	/// Deep clones an element of a particular type.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	private interface IElementCloner<T>
	{
		/// <summary>
		/// Deep clones <paramref name="source"/>.
		/// </summary>
		/// <param name="source">The element to clone.</param>
		/// <returns>The clone.</returns>
		public T Clone(T source);
	}

	/// <summary>
	/// Deep clones the key and value of a key-value pair.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	private sealed class PairElementCloner<TKey, TValue> : IElementCloner<KeyValuePair<TKey, TValue>>
	{
		/// <inheritdoc />
		public KeyValuePair<TKey, TValue> Clone(KeyValuePair<TKey, TValue> source) =>
			new(DeepClone(source.Key), DeepClone(source.Value));
	}

	/// <summary>
	/// Caches, per element type, the cloner that clones it as a key-value pair.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	private static class PairCloner<T>
	{
		/// <summary>
		/// Gets a <see cref="PairElementCloner{TKey, TValue}"/> for <typeparamref name="T"/> when it is a
		/// <see cref="KeyValuePair{TKey, TValue}"/>, otherwise <see langword="null"/>.
		/// </summary>
		internal static IElementCloner<T>? Instance { get; } =
			typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
				? (IElementCloner<T>)Activator.CreateInstance(typeof(PairElementCloner<,>).MakeGenericType(typeof(T).GetGenericArguments()))!
				: null;
	}

	/// <summary>
	/// Deep clones a collection of objects.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collection.</typeparam>
	/// <param name="source">The source collection to clone.</param>
	/// <returns>A new collection containing deep clones of the original items if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// The items are cloned when this method is called, so the result is a snapshot: it does not change
	/// when the source changes, and enumerating it again yields the same cloned instances. To get a
	/// specific collection type, convert the result (for example using ToList() or ToArray()).
	///
	/// Example usage:
	/// <code>
	/// var clonedList = originalList.DeepClone().ToList();
	/// var clonedArray = originalArray.DeepClone().ToArray();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static IEnumerable<T> DeepClone<T>(this IEnumerable<T> source)
	{
		Ensure.NotNull(source);

		return [.. source.Select(DeepClone)];
	}

	/// <summary>
	/// Deep clones a dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new dictionary containing deep clones of the keys and values if they implement IDeepCloneable,
	/// otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// This method returns a new dictionary with cloned key-value pairs. Both keys and values
	/// are deep cloned if they implement IDeepCloneable. The source's key comparer is kept when the runtime
	/// type exposes one: <see cref="Dictionary{TKey, TValue}"/>, <see cref="SortedDictionary{TKey, TValue}"/>,
	/// <see cref="SortedList{TKey, TValue}"/>, <see cref="ConcurrentDictionary{TKey, TValue}"/> (.NET 6 and later),
	/// and, on .NET, <c>ImmutableDictionary</c> and <c>ImmutableSortedDictionary</c>. A sorted source stays sorted,
	/// a sorted list or concurrent dictionary is cloned as the same type, and an immutable source is cloned as the
	/// mutable dictionary of the same kind. A <see cref="ReadOnlyDictionary{TKey, TValue}"/> is cloned according to the
	/// dictionary it wraps.
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
		Ensure.NotNull(source);

		return CloneDictionary(source, source);
	}

	/// <summary>
	/// Deep clones a dictionary, keeping its key comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new dictionary with the source's comparer, containing deep clones of the keys and values
	/// if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// <see cref="Dictionary{TKey, TValue}"/> implements both <see cref="IDictionary{TKey, TValue}"/> and
	/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so this overload is also what lets
	/// <c>dictionary.DeepClone()</c> compile without a cast.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static Dictionary<TKey, TValue> DeepClone<TKey, TValue>(this Dictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		Dictionary<TKey, TValue> clone = new(source.Count, source.Comparer);
		AddClonedPairs(clone, source);
		return clone;
	}

	/// <summary>
	/// Deep clones a sorted dictionary, keeping its key comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new sorted dictionary with the source's comparer, containing deep clones of the keys and values
	/// if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static SortedDictionary<TKey, TValue> DeepClone<TKey, TValue>(this SortedDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		SortedDictionary<TKey, TValue> clone = new(source.Comparer);
		AddClonedPairs(clone, source);
		return clone;
	}

	/// <summary>
	/// Deep clones a concurrent dictionary, keeping its key comparer where the target framework exposes it.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new concurrent dictionary containing deep clones of the keys and values
	/// if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// <see cref="ConcurrentDictionary{TKey, TValue}"/> implements both <see cref="IDictionary{TKey, TValue}"/> and
	/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so this overload is also what lets
	/// <c>concurrentDictionary.DeepClone()</c> compile without a cast. The comparer is kept on .NET 6 and later;
	/// the .NET Standard builds cannot read it, so the clone uses the default comparer there.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static ConcurrentDictionary<TKey, TValue> DeepClone<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		IEnumerable<KeyValuePair<TKey, TValue>> pairs = source.Select(p => new KeyValuePair<TKey, TValue>(DeepClone(p.Key), DeepClone(p.Value)));
#if NET6_0_OR_GREATER
		return new(pairs, source.Comparer);
#else
		return new(pairs);
#endif
	}

	/// <summary>
	/// Deep clones a read-only dictionary wrapper.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new read-only dictionary wrapping a new dictionary that contains deep clones of the keys and values
	/// if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// <see cref="ReadOnlyDictionary{TKey, TValue}"/> implements both <see cref="IDictionary{TKey, TValue}"/> and
	/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so this overload is also what lets
	/// <c>readOnlyDictionary.DeepClone()</c> compile without a cast. The new wrapper wraps a dictionary of the same
	/// kind, and with the same key comparer, as the dictionary the source wraps, as described for the
	/// <see cref="IDictionary{TKey, TValue}"/> overload.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static ReadOnlyDictionary<TKey, TValue> DeepClone<TKey, TValue>(this ReadOnlyDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		return new(CloneDictionary(source, source));
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
	/// This method returns a new read-only dictionary with cloned key-value pairs. As with the
	/// <see cref="IDictionary{TKey, TValue}"/> overload, a sorted source stays sorted and the source's key comparer
	/// is kept when the runtime type exposes one: <see cref="Dictionary{TKey, TValue}"/>,
	/// <see cref="SortedDictionary{TKey, TValue}"/>, <see cref="SortedList{TKey, TValue}"/>,
	/// <see cref="ConcurrentDictionary{TKey, TValue}"/> (.NET 6 and later), and, on .NET, <c>ImmutableDictionary</c>
	/// and <c>ImmutableSortedDictionary</c>.
	///
	/// Example usage:
	/// <code>
	/// var clonedReadOnlyDict = originalReadOnlyDict.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static IReadOnlyDictionary<TKey, TValue> DeepClone<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		return (IReadOnlyDictionary<TKey, TValue>)CloneDictionary(source, source);
	}

	/// <summary>
	/// Creates an empty dictionary of the same kind, and with the same key comparer, as <paramref name="source"/>
	/// where its runtime type exposes one, and fills it with deep clones of <paramref name="pairs"/>.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The dictionary being cloned, inspected for its runtime type and comparer. A
	/// <see cref="ReadOnlyDictionary{TKey, TValue}"/> is looked through to the dictionary it wraps.</param>
	/// <param name="pairs">The key-value pairs of the dictionary being cloned.</param>
	/// <returns>A <see cref="SortedList{TKey, TValue}"/> or <see cref="ConcurrentDictionary{TKey, TValue}"/> for a source of
	/// that type, a <see cref="SortedDictionary{TKey, TValue}"/> for any other sorted source, otherwise a
	/// <see cref="Dictionary{TKey, TValue}"/>. All of them implement <see cref="IReadOnlyDictionary{TKey, TValue}"/>.</returns>
	private static IDictionary<TKey, TValue> CloneDictionary<TKey, TValue>(object source, IEnumerable<KeyValuePair<TKey, TValue>> pairs)
		where TKey : notnull
	{
		while (source is ReadOnlyDictionary<TKey, TValue> readOnly)
		{
			source = WrappedDictionary<TKey, TValue>.Of(readOnly);
		}

		IDictionary<TKey, TValue> clone = source switch
		{
			SortedDictionary<TKey, TValue> sorted => new SortedDictionary<TKey, TValue>(sorted.Comparer),
			Dictionary<TKey, TValue> dictionary => new Dictionary<TKey, TValue>(dictionary.Count, dictionary.Comparer),
			SortedList<TKey, TValue> sortedList => new SortedList<TKey, TValue>(sortedList.Count, sortedList.Comparer),
#if NET6_0_OR_GREATER
			ConcurrentDictionary<TKey, TValue> concurrent => new ConcurrentDictionary<TKey, TValue>(concurrent.Comparer),
#endif
#if NET
			// The immutable types cannot be filled in place, so the clone is the mutable type with the same lookup.
			ImmutableSortedDictionary<TKey, TValue> immutableSorted => new SortedDictionary<TKey, TValue>(immutableSorted.KeyComparer),
			ImmutableDictionary<TKey, TValue> immutable => new Dictionary<TKey, TValue>(immutable.Count, immutable.KeyComparer),
#endif
			_ => new Dictionary<TKey, TValue>(),
		};
		AddClonedPairs(clone, pairs);
		return clone;
	}

	/// <summary>
	/// Reads the dictionary a <see cref="ReadOnlyDictionary{TKey, TValue}"/> wraps, which it exposes only through
	/// its protected <c>Dictionary</c> property, so a clone can keep that dictionary's kind and comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	private static class WrappedDictionary<TKey, TValue>
		where TKey : notnull
	{
		private static readonly Func<ReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue>> Getter =
			typeof(ReadOnlyDictionary<TKey, TValue>)
				.GetProperty("Dictionary", BindingFlags.Instance | BindingFlags.NonPublic)!
				.GetGetMethod(nonPublic: true)!
				.CreateDelegate<Func<ReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue>>>();

		/// <summary>
		/// Gets the dictionary that <paramref name="readOnly"/> wraps.
		/// </summary>
		/// <param name="readOnly">The read-only wrapper.</param>
		/// <returns>The wrapped dictionary.</returns>
		internal static IDictionary<TKey, TValue> Of(ReadOnlyDictionary<TKey, TValue> readOnly) => Getter(readOnly);
	}

	/// <summary>
	/// Adds a deep clone of each key-value pair to a destination dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dest">The dictionary to add to.</param>
	/// <param name="pairs">The key-value pairs to clone.</param>
	private static void AddClonedPairs<TKey, TValue>(IDictionary<TKey, TValue> dest, IEnumerable<KeyValuePair<TKey, TValue>> pairs)
		where TKey : notnull
	{
		foreach (KeyValuePair<TKey, TValue> pair in pairs)
		{
			dest.Add(DeepClone(pair.Key), DeepClone(pair.Value));
		}
	}

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
		Ensure.NotNull(source);

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
		Ensure.NotNull(source);

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
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static Stack<T> DeepClone<T>(this Stack<T> source)
	{
		Ensure.NotNull(source);

		return new(source.Reverse().Select(DeepClone));
	}
}
