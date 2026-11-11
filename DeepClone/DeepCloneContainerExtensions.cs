// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
#if NET
using System.Collections.Immutable;
#endif
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
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
	/// <para>
	/// The destination keeps its own comparer. A <see cref="HashSet{T}"/> or <see cref="SortedSet{T}"/> source whose
	/// comparer differs from the destination's would clone into a collection that drops elements or orders them
	/// differently, so that case throws instead. Assign <c>dest = source.DeepClone()</c> for a settable property,
	/// which keeps the source's comparer, and keep <c>DeepCloneFrom</c> for get-only ones.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown if <paramref name="dest"/> is read-only, such as an array held as an <see cref="ICollection{T}"/>.
	/// An array declared as one binds to <see cref="DeepCloneFrom{T}(T[], IEnumerable{T})"/> instead.
	/// Also thrown, before <paramref name="dest"/> is changed, if both are <see cref="HashSet{T}"/> or both are
	/// <see cref="SortedSet{T}"/> and their comparers differ.
	/// </exception>
	public static void DeepCloneFrom<T>(this ICollection<T> dest, IEnumerable<T> source)
	{
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		if (dest.IsReadOnly)
		{
			throw new ArgumentException("The destination collection is read-only, so it can't be cloned into.", nameof(dest));
		}

		EnsureSameComparer(dest, source);

		T[] clones = CloneAll(source);

		ReplaceContents(dest, clones);
	}

	/// <summary>
	/// Deep clones items from a source sequence into a destination array of the same length, element by element.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collections.</typeparam>
	/// <param name="dest">The destination array to clone into.</param>
	/// <param name="source">The source to clone from, which must hold exactly <c>dest.Length</c> items.</param>
	/// <remarks>
	/// An array is fixed-size, so the <see cref="ICollection{T}"/> overload, which clears the destination, can't fill
	/// it. This overload overwrites each element in place instead, which lets a get-only array property be cloned.
	/// Every item is cloned and the length is checked before <paramref name="dest"/> is touched, so a throwing clone or a
	/// length mismatch leaves it unchanged, and <c>array.DeepCloneFrom(array)</c> keeps its values.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	/// <exception cref="ArgumentException">Thrown if <paramref name="source"/> doesn't hold exactly <c>dest.Length</c> items.</exception>
	public static void DeepCloneFrom<T>(this T[] dest, IEnumerable<T> source)
	{
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		T[] clones = CloneAll(source);
		if (clones.Length != dest.Length)
		{
			throw new ArgumentException($"The source holds {clones.Length} items but the destination array has length {dest.Length}.", nameof(source));
		}

		clones.CopyTo(dest, 0);
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
	/// <para>
	/// The destination keeps its own key comparer. A <see cref="Dictionary{TKey, TValue}"/>,
	/// <see cref="SortedDictionary{TKey, TValue}"/> or <see cref="SortedList{TKey, TValue}"/> source whose comparer
	/// differs from the destination's would clone into a dictionary whose lookups miss or whose order changes, so that
	/// case throws instead. Assign <c>dest = source.DeepClone()</c> for a settable property, which keeps the source's
	/// comparer, and keep <c>DeepCloneFrom</c> for get-only ones.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown, before <paramref name="dest"/> is changed, if both are the same one of those dictionary kinds and their
	/// comparers differ.
	/// </exception>
	public static void DeepCloneFrom<TKey, TValue>(this IDictionary<TKey, TValue> dest, IDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		EnsureSameComparer(dest, source);

		KeyValuePair<TKey, TValue>[] clones = CloneAllPairs(source);

		ReplaceContents(dest, clones);
	}

	/// <summary>
	/// Throws when <paramref name="dest"/> and <paramref name="source"/> are the same comparer-holding collection kind
	/// with different comparers, since filling one from the other would silently change the clone (ktsu-dev/DeepClone#139).
	/// </summary>
	/// <typeparam name="T">The type of objects in the collections.</typeparam>
	/// <param name="dest">The destination collection.</param>
	/// <param name="source">The source collection.</param>
	private static void EnsureSameComparer<T>(ICollection<T> dest, IEnumerable<T> source)
	{
		object? destComparer;
		object? sourceComparer;
		string kind;
		switch (dest)
		{
			case HashSet<T> destSet when source is HashSet<T> sourceSet:
				(destComparer, sourceComparer, kind) = (destSet.Comparer, sourceSet.Comparer, "HashSet");
				break;
			case SortedSet<T> destSet when source is SortedSet<T> sourceSet:
				(destComparer, sourceComparer, kind) = (destSet.Comparer, sourceSet.Comparer, "SortedSet");
				break;
			default:
				return;
		}

		if (!ComparersMatch<T>(destComparer, sourceComparer))
		{
			throw new ArgumentException(ComparerMismatchMessage(kind, destComparer, sourceComparer), nameof(source));
		}
	}

	/// <summary>
	/// Throws when <paramref name="dest"/> and <paramref name="source"/> are the same comparer-holding dictionary kind
	/// with different key comparers, since filling one from the other would silently change the clone (ktsu-dev/DeepClone#139).
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionaries.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionaries.</typeparam>
	/// <param name="dest">The destination dictionary.</param>
	/// <param name="source">The source dictionary.</param>
	private static void EnsureSameComparer<TKey, TValue>(IDictionary<TKey, TValue> dest, IDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		object? destComparer;
		object? sourceComparer;
		string kind;
		switch (dest)
		{
			case Dictionary<TKey, TValue> destMap when source is Dictionary<TKey, TValue> sourceMap:
				(destComparer, sourceComparer, kind) = (destMap.Comparer, sourceMap.Comparer, "Dictionary");
				break;
			case SortedDictionary<TKey, TValue> destMap when source is SortedDictionary<TKey, TValue> sourceMap:
				(destComparer, sourceComparer, kind) = (destMap.Comparer, sourceMap.Comparer, "SortedDictionary");
				break;
			case SortedList<TKey, TValue> destMap when source is SortedList<TKey, TValue> sourceMap:
				(destComparer, sourceComparer, kind) = (destMap.Comparer, sourceMap.Comparer, "SortedList");
				break;
			default:
				return;
		}

		if (!ComparersMatch<TKey>(destComparer, sourceComparer))
		{
			throw new ArgumentException(ComparerMismatchMessage(kind, destComparer, sourceComparer), nameof(source));
		}
	}

	/// <summary>
	/// Reports whether two comparers compare the same way.
	/// </summary>
	/// <typeparam name="T">The type the comparers compare.</typeparam>
	/// <param name="destComparer">The destination's comparer.</param>
	/// <param name="sourceComparer">The source's comparer.</param>
	/// <returns><see langword="true"/> if they are equal once normalized.</returns>
	private static bool ComparersMatch<T>(object? destComparer, object? sourceComparer) =>
		Equals(Normalize<T>(destComparer), Normalize<T>(sourceComparer));

	/// <summary>
	/// Builds the message for a comparer mismatch, naming both comparers and the fix.
	/// </summary>
	/// <param name="kind">The collection kind.</param>
	/// <param name="destComparer">The destination's comparer.</param>
	/// <param name="sourceComparer">The source's comparer.</param>
	/// <returns>The message.</returns>
	private static string ComparerMismatchMessage(string kind, object? destComparer, object? sourceComparer) =>
		$"The source {kind} uses comparer {sourceComparer?.GetType().Name ?? "null"} but the destination uses " +
		$"{destComparer?.GetType().Name ?? "null"}, so DeepCloneFrom would produce a clone that behaves differently " +
		"from its source. Assign dest = source.DeepClone() instead, which keeps the source's comparer.";

	/// <summary>
	/// Maps <see cref="StringComparer.Ordinal"/> to the default string equality comparer, which compares the same way,
	/// so a set built with one and a set built with the other are not reported as a mismatch.
	/// </summary>
	/// <typeparam name="T">The type the comparer compares.</typeparam>
	/// <param name="comparer">The comparer to normalize.</param>
	/// <returns>The comparer, or the default equality comparer when the two are equivalent.</returns>
	private static object? Normalize<T>(object? comparer) =>
		typeof(T) == typeof(string) && ReferenceEquals(comparer, StringComparer.Ordinal)
			? EqualityComparer<string>.Default
			: comparer;

	/// <summary>
	/// Replaces the contents of <paramref name="dest"/> with <paramref name="items"/>, restoring the original
	/// contents if an <see cref="ICollection{T}.Add"/> throws part-way through.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collection.</typeparam>
	/// <param name="dest">The collection to fill.</param>
	/// <param name="items">The already-cloned items to put in it.</param>
	/// <remarks>
	/// Every clone is made before the destination is touched, so a throwing <see cref="IDeepCloneable.DeepClone"/>
	/// leaves it unchanged. An add can still be refused, for example a key that the destination's comparer treats
	/// as a duplicate, and the original contents are put back before the exception propagates (ktsu-dev/DeepClone#90).
	/// </remarks>
	private static void ReplaceContents<T>(ICollection<T> dest, T[] items)
	{
		T[] original = [.. dest];
		dest.Clear();
		try
		{
			foreach (T item in items)
			{
				dest.Add(item);
			}
		}
		catch
		{
			dest.Clear();
			foreach (T item in original)
			{
				dest.Add(item);
			}

			throw;
		}
	}

	/// <summary>
	/// Deep clones items from a source sequence into a destination stack, so the destination pops in the same order as the source.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collections.</typeparam>
	/// <param name="dest">The destination stack to clone into.</param>
	/// <param name="source">The source to clone from, enumerated top first as a <see cref="Stack{T}"/> enumerates.</param>
	/// <remarks>
	/// <see cref="Stack{T}"/> does not implement <see cref="ICollection{T}"/>, so a get-only stack property can't use
	/// <see cref="DeepCloneFrom{T}(ICollection{T}, IEnumerable{T})"/>. The source is snapshotted first, so
	/// <c>stack.DeepCloneFrom(stack)</c> leaves the stack unchanged, then pushed in reverse so the top stays on top.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	public static void DeepCloneFrom<T>(this Stack<T> dest, IEnumerable<T> source)
	{
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		T[] clones = CloneAll(source);
		dest.Clear();
		for (int i = clones.Length - 1; i >= 0; i--)
		{
			dest.Push(clones[i]);
		}
	}

	/// <summary>
	/// Deep clones items from a source sequence into a destination queue, preserving dequeue order.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collections.</typeparam>
	/// <param name="dest">The destination queue to clone into.</param>
	/// <param name="source">The source to clone from, enumerated front first.</param>
	/// <remarks>
	/// <see cref="Queue{T}"/> does not implement <see cref="ICollection{T}"/>, so a get-only queue property can't use
	/// <see cref="DeepCloneFrom{T}(ICollection{T}, IEnumerable{T})"/>. The source is snapshotted first, so
	/// <c>queue.DeepCloneFrom(queue)</c> leaves the queue unchanged.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	public static void DeepCloneFrom<T>(this Queue<T> dest, IEnumerable<T> source)
	{
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		T[] clones = CloneAll(source);
		dest.Clear();
		foreach (T clone in clones)
		{
			dest.Enqueue(clone);
		}
	}

	/// <summary>
	/// Deep clones items from a source sequence into a destination concurrent stack, so the destination pops in the same order as the source.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collections.</typeparam>
	/// <param name="dest">The destination stack to clone into.</param>
	/// <param name="source">The source to clone from, enumerated top first as a <see cref="ConcurrentStack{T}"/> enumerates.</param>
	/// <remarks>
	/// The source is snapshotted first, so <c>stack.DeepCloneFrom(stack)</c> leaves the stack unchanged. The clear and refill
	/// are not one atomic operation: another thread writing to <paramref name="dest"/> meanwhile may see it part-filled.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	public static void DeepCloneFrom<T>(this ConcurrentStack<T> dest, IEnumerable<T> source)
	{
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		T[] items = [.. source];
		T[] clones = new T[items.Length];
		using (CloneContext.Enter())
		{
			for (int i = 0; i < items.Length; i++)
			{
				// PushRange pushes its first element first, so the last source item goes in first and the top stays on top.
				clones[items.Length - 1 - i] = DeepClone(items[i]);
			}
		}

		dest.Clear();
		if (clones.Length > 0)
		{
			dest.PushRange(clones);
		}
	}

	/// <summary>
	/// Deep clones items from a source sequence into a destination concurrent queue, preserving dequeue order.
	/// </summary>
	/// <typeparam name="T">The type of objects in the collections.</typeparam>
	/// <param name="dest">The destination queue to clone into.</param>
	/// <param name="source">The source to clone from, enumerated front first.</param>
	/// <remarks>
	/// The source is snapshotted first, so <c>queue.DeepCloneFrom(queue)</c> leaves the queue unchanged. The clear and refill
	/// are not one atomic operation: another thread writing to <paramref name="dest"/> meanwhile may see it part-filled.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if dest or source is null.</exception>
	public static void DeepCloneFrom<T>(this ConcurrentQueue<T> dest, IEnumerable<T> source)
	{
		Ensure.NotNull(dest);

		Ensure.NotNull(source);

		T[] clones = CloneAll(source);
#if NETSTANDARD2_1 || NET
		dest.Clear();
#else
		while (dest.TryDequeue(out _))
		{
			// Intentionally empty: netstandard2.0 has no ConcurrentQueue.Clear, so drain it instead.
		}
#endif
		foreach (T clone in clones)
		{
			dest.Enqueue(clone);
		}
	}

	/// <summary>
	/// Snapshots <paramref name="source"/> and deep clones every item, in order, before any destination is touched,
	/// so an item whose clone throws leaves the destination unchanged (ktsu-dev/DeepClone#90, #124).
	/// </summary>
	private static T[] CloneAll<T>(IEnumerable<T> source)
	{
		T[] items = [.. source];
		T[] clones = new T[items.Length];
		using CloneContext.Scope scope = CloneContext.Enter();
		for (int i = 0; i < items.Length; i++)
		{
			clones[i] = DeepClone(items[i]);
		}

		return clones;
	}

	/// <summary>
	/// Deep clones the key and value of each pair, within one clone context, so a key or value
	/// reached more than once is cloned once.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys.</typeparam>
	/// <typeparam name="TValue">The type of the values.</typeparam>
	/// <param name="pairs">The key-value pairs to clone.</param>
	/// <returns>The cloned pairs.</returns>
	private static KeyValuePair<TKey, TValue>[] CloneAllPairs<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
	{
		KeyValuePair<TKey, TValue>[] items = [.. pairs];
		KeyValuePair<TKey, TValue>[] clones = new KeyValuePair<TKey, TValue>[items.Length];
		using CloneContext.Scope scope = CloneContext.Enter();
		for (int i = 0; i < items.Length; i++)
		{
			clones[i] = new(DeepClone(items[i].Key), DeepClone(items[i].Value));
		}

		return clones;
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
	/// A pair held as <c>KeyValuePair&lt;TKey, TValue&gt;?</c>, or boxed in an element type such as
	/// <see cref="object"/>, is rebuilt the same way, chosen from its runtime type (ktsu-dev/DeepClone#135).
	/// </remarks>
	private static T DeepClone<T>(T source) => source switch
	{
		null => default!,
		IDeepCloneable cloneable => (T)cloneable.DeepClone(),
		_ when PairCloner<T>.Instance is { } pairCloner => pairCloner.Clone(source),
		_ when !typeof(T).IsValueType && BoxedPairCloner.For(source.GetType()) is { } boxedCloner => (T)boxedCloner.CloneBoxed(source),
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
	private sealed class PairElementCloner<TKey, TValue> : IElementCloner<KeyValuePair<TKey, TValue>>, IBoxedCloner
	{
		/// <inheritdoc />
		public KeyValuePair<TKey, TValue> Clone(KeyValuePair<TKey, TValue> source) =>
			new(DeepClone(source.Key), DeepClone(source.Value));

		/// <inheritdoc />
		public object CloneBoxed(object source) => Clone((KeyValuePair<TKey, TValue>)source);
	}

	/// <summary>
	/// Deep clones the key and value of a key-value pair held as a <see cref="Nullable{T}"/>.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	private sealed class NullablePairElementCloner<TKey, TValue> : IElementCloner<KeyValuePair<TKey, TValue>?>
	{
		/// <inheritdoc />
		public KeyValuePair<TKey, TValue>? Clone(KeyValuePair<TKey, TValue>? source) =>
			source is { } pair ? new KeyValuePair<TKey, TValue>(DeepClone(pair.Key), DeepClone(pair.Value)) : null;
	}

	/// <summary>
	/// Deep clones an element that is known only by its runtime type.
	/// </summary>
	private interface IBoxedCloner
	{
		/// <summary>
		/// Deep clones <paramref name="source"/>.
		/// </summary>
		/// <param name="source">The boxed element to clone.</param>
		/// <returns>The boxed clone.</returns>
		public object CloneBoxed(object source);
	}

	/// <summary>
	/// Caches, per runtime type, the cloner that clones a boxed key-value pair.
	/// </summary>
	private static class BoxedPairCloner
	{
		private static readonly ConcurrentDictionary<Type, IBoxedCloner?> Cloners = new();

		/// <summary>
		/// Gets the cloner for a boxed value of <paramref name="runtimeType"/> when it is a
		/// <see cref="KeyValuePair{TKey, TValue}"/>, otherwise <see langword="null"/>.
		/// </summary>
		/// <param name="runtimeType">The runtime type of the element.</param>
		/// <returns>The cloner, or <see langword="null"/>.</returns>
		internal static IBoxedCloner? For(Type runtimeType) =>
			IsKeyValuePair(runtimeType)
				? Cloners.GetOrAdd(runtimeType, static type => (IBoxedCloner)CreatePairCloner(type.GetGenericArguments()))
				: null;
	}

	/// <summary>
	/// Determines whether <paramref name="type"/> is a closed <see cref="KeyValuePair{TKey, TValue}"/>.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns><see langword="true"/> when it is a key-value pair type.</returns>
	private static bool IsKeyValuePair(Type type) =>
		type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

	/// <summary>
	/// Creates the <see cref="PairElementCloner{TKey, TValue}"/> for a pair's key and value types. This is the one
	/// place a pair cloner is made from reflected type arguments.
	/// </summary>
	/// <param name="pairArguments">The key and value types.</param>
	/// <returns>The cloner.</returns>
	private static object CreatePairCloner(Type[] pairArguments) =>
		Activator.CreateInstance(typeof(PairElementCloner<,>).MakeGenericType(pairArguments))!;

	/// <summary>
	/// Caches, per element type, the cloner that clones it as a key-value pair.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	private static class PairCloner<T>
	{
		/// <summary>
		/// Gets a <see cref="PairElementCloner{TKey, TValue}"/> for <typeparamref name="T"/> when it is a
		/// <see cref="KeyValuePair{TKey, TValue}"/>, a <see cref="NullablePairElementCloner{TKey, TValue}"/> when it is a
		/// nullable one, otherwise <see langword="null"/>.
		/// </summary>
		internal static IElementCloner<T>? Instance { get; } = Create();

		private static IElementCloner<T>? Create()
		{
			if (IsKeyValuePair(typeof(T)))
			{
				return (IElementCloner<T>)CreatePairCloner(typeof(T).GetGenericArguments());
			}

			Type? underlying = Nullable.GetUnderlyingType(typeof(T));
			return underlying is not null && IsKeyValuePair(underlying)
				? (IElementCloner<T>)Activator.CreateInstance(typeof(NullablePairElementCloner<,>).MakeGenericType(underlying.GetGenericArguments()))!
				: null;
		}
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
	/// specific collection type, convert the result (for example using ToList() or ToHashSet()). Arrays,
	/// <see cref="List{T}"/>, <see cref="Queue{T}"/> and <see cref="LinkedList{T}"/> have overloads that
	/// already return their own type.
	///
	/// Example usage:
	/// <code>
	/// var clonedSet = originalSequence.DeepClone().ToHashSet();
	/// </code>
	///
	/// A source that implements <see cref="IDeepCloneable"/> itself, such as a custom collection whose
	/// <see cref="IDeepCloneable.DeepClone"/> is implemented explicitly, is cloned by that implementation,
	/// so the clone keeps the source's type and its other fields rather than becoming a list of cloned items.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static IEnumerable<T> DeepClone<T>(this IEnumerable<T> source)
	{
		Ensure.NotNull(source);

		if (source is IDeepCloneable cloneable)
		{
			return (IEnumerable<T>)cloneable.DeepClone();
		}

		return [.. CloneAll(source)];
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
	/// <see cref="SortedList{TKey, TValue}"/>, <see cref="ConcurrentDictionary{TKey, TValue}"/> (where the runtime exposes its comparer, as .NET 6 and later do),
	/// on .NET, <c>ImmutableDictionary</c> and <c>ImmutableSortedDictionary</c>, and, on .NET 8 and later,
	/// <c>FrozenDictionary</c>. A sorted source stays sorted, a sorted list or concurrent dictionary is cloned as the
	/// same type, and an immutable or frozen source is cloned as the mutable dictionary of the same kind. A <see cref="ReadOnlyDictionary{TKey, TValue}"/> is cloned according to the
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
	/// Deep clones a sorted list, keeping its key comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the list.</typeparam>
	/// <typeparam name="TValue">The type of values in the list.</typeparam>
	/// <param name="source">The source list to clone.</param>
	/// <returns>A new sorted list with the source's comparer, containing deep clones of the keys and values
	/// if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// <see cref="SortedList{TKey, TValue}"/> implements both <see cref="IDictionary{TKey, TValue}"/> and
	/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so this overload is also what lets
	/// <c>sortedList.DeepClone()</c> compile without a cast.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static SortedList<TKey, TValue> DeepClone<TKey, TValue>(this SortedList<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		SortedList<TKey, TValue> clone = new(source.Count, source.Comparer);
		AddClonedPairs(clone, source);
		return clone;
	}

#if NET9_0_OR_GREATER
	/// <summary>
	/// Deep clones an ordered dictionary, keeping its key comparer and key order.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new ordered dictionary with the source's comparer and key order, containing deep clones of the keys
	/// and values if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// <see cref="OrderedDictionary{TKey, TValue}"/> implements both <see cref="IDictionary{TKey, TValue}"/> and
	/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so this overload is also what lets
	/// <c>orderedDictionary.DeepClone()</c> compile without a cast (.NET 9 and later).
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static OrderedDictionary<TKey, TValue> DeepClone<TKey, TValue>(this OrderedDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		OrderedDictionary<TKey, TValue> clone = new(source.Count, source.Comparer);
		AddClonedPairs(clone, source);
		return clone;
	}
#endif

#if NET
	/// <summary>
	/// Deep clones an immutable dictionary, keeping its key and value comparers.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new immutable dictionary with the source's comparers, containing deep clones of the keys and values
	/// if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// <see cref="ImmutableDictionary{TKey, TValue}"/> implements both <see cref="IDictionary{TKey, TValue}"/> and
	/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so this overload is also what lets
	/// <c>immutableDictionary.DeepClone()</c> compile without a cast.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static ImmutableDictionary<TKey, TValue> DeepClone<TKey, TValue>(this ImmutableDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		return ImmutableDictionary.CreateRange(source.KeyComparer, source.ValueComparer, CloneAllPairs(source));
	}

	/// <summary>
	/// Deep clones an immutable sorted dictionary, keeping its key and value comparers.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new immutable sorted dictionary with the source's comparers, containing deep clones of the keys and
	/// values if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// <see cref="ImmutableSortedDictionary{TKey, TValue}"/> implements both <see cref="IDictionary{TKey, TValue}"/>
	/// and <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so this overload is also what lets
	/// <c>immutableSortedDictionary.DeepClone()</c> compile without a cast.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static ImmutableSortedDictionary<TKey, TValue> DeepClone<TKey, TValue>(this ImmutableSortedDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		return ImmutableSortedDictionary.CreateRange(source.KeyComparer, source.ValueComparer, CloneAllPairs(source));
	}

#if NET8_0_OR_GREATER
	/// <summary>
	/// Deep clones a frozen dictionary, keeping its key comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="source">The source dictionary to clone.</param>
	/// <returns>A new frozen dictionary with the source's comparer, containing deep clones of the keys and values
	/// if they implement IDeepCloneable, otherwise containing the original keys and values.</returns>
	/// <remarks>
	/// <see cref="FrozenDictionary{TKey, TValue}"/> implements both <see cref="IDictionary{TKey, TValue}"/> and
	/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>, so this overload is also what lets
	/// <c>frozenDictionary.DeepClone()</c> compile without a cast (.NET 8 and later).
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static FrozenDictionary<TKey, TValue> DeepClone<TKey, TValue>(this FrozenDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		return CloneAllPairs(source).ToFrozenDictionary(source.Comparer);
	}

	/// <summary>
	/// Deep clones a frozen set, keeping its comparer.
	/// </summary>
	/// <typeparam name="T">The type of objects in the set.</typeparam>
	/// <param name="source">The source set to clone.</param>
	/// <returns>A new frozen set with the source's comparer, containing deep clones of the original items if they
	/// implement IDeepCloneable, otherwise containing the original items.</returns>
	/// <remarks>
	/// Without this overload a frozen set binds to the <see cref="IEnumerable{T}"/> overload, which returns a list
	/// and loses the comparer (.NET 8 and later).
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static FrozenSet<T> DeepClone<T>(this FrozenSet<T> source)
	{
		Ensure.NotNull(source);

		return CloneAll(source).ToFrozenSet(source.Comparer);
	}
#endif
#endif

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
	/// <c>concurrentDictionary.DeepClone()</c> compile without a cast. The comparer is kept wherever the runtime exposes
	/// it, which .NET 6 and later do, including through the .NET Standard builds; on older runtimes the clone uses the
	/// default comparer.
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static ConcurrentDictionary<TKey, TValue> DeepClone<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source)
		where TKey : notnull
	{
		Ensure.NotNull(source);

		KeyValuePair<TKey, TValue>[] pairs = CloneAllPairs(source);
		return new(pairs, ConcurrentComparer<TKey, TValue>.Of(source));
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
	/// <see cref="ConcurrentDictionary{TKey, TValue}"/> (where the runtime exposes its comparer, as .NET 6 and later do), on .NET, <c>ImmutableDictionary</c>
	/// and <c>ImmutableSortedDictionary</c>, and, on .NET 8 and later, <c>FrozenDictionary</c>.
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
	/// <returns>A <see cref="SortedList{TKey, TValue}"/>, <see cref="ConcurrentDictionary{TKey, TValue}"/> or (.NET 9 and
	/// later) <c>OrderedDictionary</c> for a source of that type, a <see cref="SortedDictionary{TKey, TValue}"/> for any other sorted source, otherwise a
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
#if NET9_0_OR_GREATER
			OrderedDictionary<TKey, TValue> ordered => new OrderedDictionary<TKey, TValue>(ordered.Count, ordered.Comparer),
#endif
			ConcurrentDictionary<TKey, TValue> concurrent => new ConcurrentDictionary<TKey, TValue>(ConcurrentComparer<TKey, TValue>.Of(concurrent)),
#if NET
			// The immutable types cannot be filled in place, so the clone is the mutable type with the same lookup.
			ImmutableSortedDictionary<TKey, TValue> immutableSorted => new SortedDictionary<TKey, TValue>(immutableSorted.KeyComparer),
			ImmutableDictionary<TKey, TValue> immutable => new Dictionary<TKey, TValue>(immutable.Count, immutable.KeyComparer),
#endif
#if NET8_0_OR_GREATER
			// A frozen dictionary cannot be filled in place either, so the clone is a dictionary with the same comparer.
			FrozenDictionary<TKey, TValue> frozen => new Dictionary<TKey, TValue>(frozen.Count, frozen.Comparer),
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Major Code Smell",
			"S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
			Justification = "ReadOnlyDictionary exposes the dictionary it wraps only through a protected property, and the getter is only read, never used to modify it. Without it the clone cannot keep the wrapped dictionary's comparer (ktsu-dev/DeepClone#87).")]
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
	/// Reads the key comparer of a <see cref="ConcurrentDictionary{TKey, TValue}"/>. Its <c>Comparer</c> property
	/// exists only from .NET 6, so the .NET Standard builds look it up at run time: a .NET 6 or later app that restores
	/// one of them still keeps the comparer, and only a runtime without the property falls back to the default.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	private static class ConcurrentComparer<TKey, TValue>
		where TKey : notnull
	{
#if NET
		/// <summary>
		/// Gets the key comparer of <paramref name="source"/>.
		/// </summary>
		/// <param name="source">The concurrent dictionary.</param>
		/// <returns>The dictionary's key comparer.</returns>
		internal static IEqualityComparer<TKey> Of(ConcurrentDictionary<TKey, TValue> source) => source.Comparer;
#else
		private static readonly Func<ConcurrentDictionary<TKey, TValue>, IEqualityComparer<TKey>>? Getter =
			typeof(ConcurrentDictionary<TKey, TValue>)
				.GetProperty("Comparer", BindingFlags.Instance | BindingFlags.Public, null, typeof(IEqualityComparer<TKey>), Type.EmptyTypes, null)?
				.GetGetMethod()?
				.CreateDelegate<Func<ConcurrentDictionary<TKey, TValue>, IEqualityComparer<TKey>>>();

		/// <summary>
		/// Gets the key comparer of <paramref name="source"/>, or the default comparer when the runtime does not expose it.
		/// </summary>
		/// <param name="source">The concurrent dictionary.</param>
		/// <returns>The dictionary's key comparer, or <see cref="EqualityComparer{T}.Default"/>.</returns>
		internal static IEqualityComparer<TKey> Of(ConcurrentDictionary<TKey, TValue> source) =>
			Getter?.Invoke(source) ?? EqualityComparer<TKey>.Default;
#endif
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
		using CloneContext.Scope scope = CloneContext.Enter();
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

		return new(CloneAll(source), source.Comparer);
	}

	/// <summary>
	/// Deep clones a list.
	/// </summary>
	/// <typeparam name="T">The type of objects in the list.</typeparam>
	/// <param name="source">The source list to clone.</param>
	/// <returns>A new list containing deep clones of the original items in the same order if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the order of elements in the list. Without this overload the call bound to the <see cref="IEnumerable{T}"/> overload, which returns a read-only sequence that cannot be cast back to a <see cref="List{T}"/> (ktsu-dev/DeepClone#109).
	///
	/// Example usage:
	/// <code>
	/// var clonedList = originalList.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Design",
		"CA1002:Do not expose generic lists",
		Justification = "The overload exists to return the same concrete type it is called on, so a cloned List<T> property can be assigned without a cast (ktsu-dev/DeepClone#109).")]
	public static List<T> DeepClone<T>(this List<T> source)
	{
		Ensure.NotNull(source);

		return [.. CloneAll(source)];
	}

	/// <summary>
	/// Deep clones an array.
	/// </summary>
	/// <typeparam name="T">The type of objects in the array.</typeparam>
	/// <param name="source">The source array to clone.</param>
	/// <returns>A new array of the same length containing deep clones of the original items in the same order if they
	/// implement IDeepCloneable, otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the order of elements in the array. Without this overload the call bound to the <see cref="IEnumerable{T}"/> overload, which returns a read-only sequence that cannot be cast back to an array, so callers paid for a second copy with ToArray() (ktsu-dev/DeepClone#130).
	///
	/// Example usage:
	/// <code>
	/// var clonedArray = originalArray.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static T[] DeepClone<T>(this T[] source)
	{
		Ensure.NotNull(source);

		return CloneAll(source);
	}

	/// <summary>
	/// Deep clones a queue.
	/// </summary>
	/// <typeparam name="T">The type of objects in the queue.</typeparam>
	/// <param name="source">The source queue to clone.</param>
	/// <returns>A new queue containing deep clones of the original items in the same order if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the order of elements in the queue, so the clone dequeues in the same order. Without this overload the call bound to the <see cref="IEnumerable{T}"/> overload, which returns a read-only sequence that cannot be cast back to a <see cref="Queue{T}"/> (ktsu-dev/DeepClone#109).
	///
	/// Example usage:
	/// <code>
	/// var clonedQueue = originalQueue.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static Queue<T> DeepClone<T>(this Queue<T> source)
	{
		Ensure.NotNull(source);

		return new(CloneAll(source));
	}

	/// <summary>
	/// Deep clones a linked list.
	/// </summary>
	/// <typeparam name="T">The type of objects in the list.</typeparam>
	/// <param name="source">The source linked list to clone.</param>
	/// <returns>A new linked list containing deep clones of the original items in the same order if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the order of nodes in the linked list. Without this overload the call bound to the <see cref="IEnumerable{T}"/> overload, which returns a read-only sequence that cannot be cast back to a <see cref="LinkedList{T}"/> (ktsu-dev/DeepClone#109).
	///
	/// Example usage:
	/// <code>
	/// var clonedLinkedList = originalLinkedList.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static LinkedList<T> DeepClone<T>(this LinkedList<T> source)
	{
		Ensure.NotNull(source);

		return new(CloneAll(source));
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

		return new(CloneAll(source), source.Comparer);
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

		return new(CloneAll(source.Reverse()));
	}

	/// <summary>
	/// Deep clones a concurrent stack.
	/// </summary>
	/// <typeparam name="T">The type of objects in the stack.</typeparam>
	/// <param name="source">The source stack to clone.</param>
	/// <returns>A new concurrent stack containing deep clones of the original items in the same pop order if they
	/// implement IDeepCloneable, otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the pop order of the stack. It clones from a single <see cref="ConcurrentStack{T}.ToArray"/>
	/// snapshot, so the clone is a consistent copy even while other threads push or pop. Without this overload the call
	/// bound to the <see cref="IEnumerable{T}"/> overload, and rebuilding a stack from that top-first sequence reversed
	/// it (ktsu-dev/DeepClone#102).
	///
	/// Example usage:
	/// <code>
	/// var clonedStack = originalStack.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Style",
		"IDE0305:Simplify collection initialization",
		Justification = "ConcurrentStack.ToArray takes one atomic snapshot; a spread enumerates while other threads push or pop (ktsu-dev/DeepClone#100).")]
	public static ConcurrentStack<T> DeepClone<T>(this ConcurrentStack<T> source)
	{
		Ensure.NotNull(source);

		T[] snapshot = source.ToArray();
		Array.Reverse(snapshot);
		return new(CloneAll(snapshot));
	}

	/// <summary>
	/// Deep clones a concurrent queue.
	/// </summary>
	/// <typeparam name="T">The type of objects in the queue.</typeparam>
	/// <param name="source">The source queue to clone.</param>
	/// <returns>A new concurrent queue containing deep clones of the original items in the same dequeue order if they
	/// implement IDeepCloneable, otherwise containing the original items.</returns>
	/// <remarks>
	/// This method preserves the dequeue order of the queue. It clones from a single <see cref="ConcurrentQueue{T}.ToArray"/>
	/// snapshot, so the clone is a consistent copy even while other threads enqueue or dequeue. Without this overload the
	/// call bound to the <see cref="IEnumerable{T}"/> overload and returned a read-only list (ktsu-dev/DeepClone#128).
	///
	/// Example usage:
	/// <code>
	/// var clonedQueue = originalQueue.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Style",
		"IDE0305:Simplify collection initialization",
		Justification = "ConcurrentQueue.ToArray takes one atomic snapshot; a spread enumerates while other threads enqueue or dequeue (ktsu-dev/DeepClone#128).")]
	public static ConcurrentQueue<T> DeepClone<T>(this ConcurrentQueue<T> source)
	{
		Ensure.NotNull(source);

		return new(CloneAll(source.ToArray()));
	}

	/// <summary>
	/// Deep clones a concurrent bag.
	/// </summary>
	/// <typeparam name="T">The type of objects in the bag.</typeparam>
	/// <param name="source">The source bag to clone.</param>
	/// <returns>A new concurrent bag containing deep clones of the original items if they implement IDeepCloneable,
	/// otherwise containing the original items.</returns>
	/// <remarks>
	/// A bag has no defined order, so only its contents are preserved. It clones from a single
	/// <see cref="ConcurrentBag{T}.ToArray"/> snapshot, so the clone is a consistent copy even while other threads add or
	/// take. Without this overload the call bound to the <see cref="IEnumerable{T}"/> overload and returned a read-only
	/// list (ktsu-dev/DeepClone#128).
	///
	/// Example usage:
	/// <code>
	/// var clonedBag = originalBag.DeepClone();
	/// </code>
	/// </remarks>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static ConcurrentBag<T> DeepClone<T>(this ConcurrentBag<T> source)
	{
		Ensure.NotNull(source);

		return [.. CloneAll(source.ToArray())];
	}
}
