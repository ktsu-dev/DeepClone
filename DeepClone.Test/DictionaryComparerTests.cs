// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.Concurrent;
using System.Collections.Immutable;

/// <summary>
/// Tests that cloning a dictionary through <see cref="IDictionary{TKey, TValue}"/> or
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> keeps the source's key comparer for every
/// dictionary type that exposes one (ktsu-dev/DeepClone#83).
/// </summary>
[TestClass]
public class DictionaryComparerTests
{
	private static readonly string[] DescendingKeys = ["d", "c", "b", "a"];

	/// <summary>
	/// Tests that a concurrent dictionary cloned through IDictionary keeps a case-insensitive comparer
	/// and stays concurrent.
	/// </summary>
	[TestMethod]
	public void ConcurrentDictionary_ThroughIDictionary_KeepsComparer()
	{
		ConcurrentDictionary<string, int> original = new(StringComparer.OrdinalIgnoreCase);
		original["Key"] = 1;

		IDictionary<string, int> clone = ((IDictionary<string, int>)original).DeepClone();

		Assert.IsInstanceOfType<ConcurrentDictionary<string, int>>(clone);
		Assert.IsTrue(clone.ContainsKey("key"));
	}

	/// <summary>
	/// Tests that a concurrent dictionary using reference equality, holding two equal but distinct keys,
	/// clones without throwing and keeps both entries.
	/// </summary>
	[TestMethod]
	public void ConcurrentDictionary_WithReferenceEquality_ClonesEqualKeys()
	{
		ConcurrentDictionary<object, int> original = new(ReferenceEqualityComparer.Instance);
		original[new string('k', 1)] = 1;
		original[new string('k', 1)] = 2;

		IDictionary<object, int> clone = ((IDictionary<object, int>)original).DeepClone();

		Assert.HasCount(2, clone);
	}

	/// <summary>
	/// Tests that a sorted list cloned through IDictionary stays a sorted list with the source's comparer,
	/// so it keeps its order on later inserts.
	/// </summary>
	[TestMethod]
	public void SortedList_ThroughIDictionary_KeepsComparerAndOrder()
	{
		Comparer<string> descending = Comparer<string>.Create((x, y) => string.CompareOrdinal(y, x));
		SortedList<string, int> original = new(descending) { ["a"] = 1, ["c"] = 3, ["b"] = 2 };

		IDictionary<string, int> clone = ((IDictionary<string, int>)original).DeepClone();
		clone["d"] = 4;

		SortedList<string, int> sortedClone = Assert.IsInstanceOfType<SortedList<string, int>>(clone);
		Assert.AreSame(descending, sortedClone.Comparer);
		CollectionAssert.AreEqual(DescendingKeys, sortedClone.Keys.ToArray());
	}

	/// <summary>
	/// Tests that a sorted list cloned through IReadOnlyDictionary keeps a case-insensitive comparer.
	/// </summary>
	[TestMethod]
	public void SortedList_ThroughIReadOnlyDictionary_KeepsComparer()
	{
		SortedList<string, int> original = new(StringComparer.OrdinalIgnoreCase) { ["Key"] = 1 };

		IReadOnlyDictionary<string, int> clone = ((IReadOnlyDictionary<string, int>)original).DeepClone();

		Assert.IsTrue(clone.ContainsKey("key"));
	}

	/// <summary>
	/// Tests that an immutable dictionary cloned through IDictionary keeps its case-insensitive key comparer.
	/// </summary>
	[TestMethod]
	public void ImmutableDictionary_ThroughIDictionary_KeepsComparer()
	{
		ImmutableDictionary<string, int> original = ImmutableDictionary.Create<string, int>(StringComparer.OrdinalIgnoreCase).Add("Key", 1);

		IDictionary<string, int> clone = ((IDictionary<string, int>)original).DeepClone();

		Assert.IsTrue(clone.ContainsKey("key"));
	}

	/// <summary>
	/// Tests that an immutable dictionary using reference equality, holding two equal but distinct keys,
	/// clones without throwing and keeps both entries.
	/// </summary>
	[TestMethod]
	public void ImmutableDictionary_WithReferenceEquality_ClonesEqualKeys()
	{
		ImmutableDictionary<object, int> original = ImmutableDictionary.Create<object, int>(ReferenceEqualityComparer.Instance)
			.Add(new string('k', 1), 1)
			.Add(new string('k', 1), 2);

		IReadOnlyDictionary<object, int> clone = ((IReadOnlyDictionary<object, int>)original).DeepClone();

		Assert.HasCount(2, clone);
	}

	/// <summary>
	/// Tests that an immutable sorted dictionary cloned through IDictionary stays sorted by the source's comparer.
	/// </summary>
	[TestMethod]
	public void ImmutableSortedDictionary_ThroughIDictionary_KeepsComparerAndOrder()
	{
		Comparer<string> descending = Comparer<string>.Create((x, y) => string.CompareOrdinal(y, x));
		ImmutableSortedDictionary<string, int> original = ImmutableSortedDictionary.Create<string, int>(descending)
			.Add("a", 1).Add("c", 3).Add("b", 2);

		IDictionary<string, int> clone = ((IDictionary<string, int>)original).DeepClone();
		clone["d"] = 4;

		CollectionAssert.AreEqual(DescendingKeys, clone.Keys.ToArray());
	}
}
