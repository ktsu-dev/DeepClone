// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.Concurrent;

/// <summary>
/// Tests that <c>DeepCloneFrom</c> refuses a source whose comparer differs from the destination's, instead of producing
/// a clone that drops elements, reorders them or misses lookups (ktsu-dev/DeepClone#139).
/// </summary>
[TestClass]
public class DeepCloneFromComparerTests
{
	private sealed class Settings : DeepCloneable<Settings>
	{
		public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

		protected override Settings CreateInstance() => new();

		protected override void DeepClone(Settings clone) => clone.Tags.DeepCloneFrom(Tags);
	}

	private static readonly int[] DescendingOrder = [3, 2, 1];

	private static readonly Comparer<int> Descending = Comparer<int>.Create((x, y) => y.CompareTo(x));

	/// <summary>
	/// The issue's repro: a reassigned ordinal source set cloned into the clone's case-insensitive set.
	/// </summary>
	[TestMethod]
	public void ReassignedSourceSet_DeepClone_ThrowsInsteadOfDroppingElements()
	{
		Settings source = new() { Tags = ["a", "A"] };

		ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(source.DeepClone);

		Assert.Contains("DeepClone()", exception.Message);
	}

	/// <summary>
	/// Tests that a case-insensitive source set refuses an ordinal destination, which would keep both "a" and "A" apart.
	/// </summary>
	[TestMethod]
	public void HashSet_DifferentComparer_ThrowsAndLeavesDestinationUnchanged()
	{
		HashSet<string> dest = ["keep"];
		HashSet<string> source = new(StringComparer.OrdinalIgnoreCase) { "a" };

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(source));

		Assert.HasCount(1, dest);
		Assert.Contains("keep", dest);
	}

	/// <summary>
	/// Tests that a descending source set refuses an ascending destination, which would reverse its order.
	/// </summary>
	[TestMethod]
	public void SortedSet_DifferentComparer_Throws()
	{
		SortedSet<int> dest = [];
		SortedSet<int> source = new(Descending) { 1, 2, 3 };

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(source));
		Assert.IsEmpty(dest);
	}

	/// <summary>
	/// Tests that a case-insensitive source dictionary refuses an ordinal destination, whose lookups would miss.
	/// </summary>
	[TestMethod]
	public void Dictionary_DifferentComparer_Throws()
	{
		Dictionary<string, int> dest = [];
		Dictionary<string, int> source = new(StringComparer.OrdinalIgnoreCase) { ["key"] = 1 };

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(source));
		Assert.IsEmpty(dest);
	}

	/// <summary>
	/// Tests that a descending source sorted dictionary refuses an ascending destination.
	/// </summary>
	[TestMethod]
	public void SortedDictionary_DifferentComparer_Throws()
	{
		SortedDictionary<int, string> dest = [];
		SortedDictionary<int, string> source = new(Descending) { [1] = "a", [2] = "b" };

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(source));
		Assert.IsEmpty(dest);
	}

	/// <summary>
	/// Tests that a descending source sorted list refuses an ascending destination.
	/// </summary>
	[TestMethod]
	public void SortedList_DifferentComparer_Throws()
	{
		SortedList<int, string> dest = [];
		SortedList<int, string> source = new(Descending) { [1] = "a", [2] = "b" };

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(source));
		Assert.IsEmpty(dest);
	}

	/// <summary>
	/// Tests that a case-insensitive source ordered dictionary refuses an ordinal destination, whose lookups would miss
	/// (ktsu-dev/DeepClone#143).
	/// </summary>
	[TestMethod]
	public void OrderedDictionary_DifferentComparer_ThrowsAndLeavesDestinationUnchanged()
	{
		OrderedDictionary<string, int> dest = new() { ["keep"] = 0 };
		OrderedDictionary<string, int> source = new(StringComparer.OrdinalIgnoreCase) { ["KEY"] = 1 };

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(source));

		Assert.HasCount(1, dest);
		Assert.AreEqual(0, dest["keep"]);
	}

	/// <summary>
	/// Tests that a case-insensitive source concurrent dictionary refuses an ordinal destination, whose lookups would
	/// miss (ktsu-dev/DeepClone#143).
	/// </summary>
	[TestMethod]
	public void ConcurrentDictionary_DifferentComparer_ThrowsAndLeavesDestinationUnchanged()
	{
		ConcurrentDictionary<string, int> dest = new() { ["keep"] = 0 };
		ConcurrentDictionary<string, int> source = new(StringComparer.OrdinalIgnoreCase) { ["KEY"] = 1 };

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(source));

		Assert.HasCount(1, dest);
		Assert.AreEqual(0, dest["keep"]);
	}

	/// <summary>
	/// Tests that ordered and concurrent dictionaries with matching comparers still clone, keeping case-insensitive
	/// lookups.
	/// </summary>
	[TestMethod]
	public void OrderedAndConcurrentDictionaries_SameComparer_Clone()
	{
		OrderedDictionary<string, int> orderedDest = new(StringComparer.OrdinalIgnoreCase);
		OrderedDictionary<string, int> orderedSource = new(StringComparer.OrdinalIgnoreCase) { ["KEY"] = 1 };
		ConcurrentDictionary<string, int> concurrentDest = new(StringComparer.OrdinalIgnoreCase);
		ConcurrentDictionary<string, int> concurrentSource = new(StringComparer.OrdinalIgnoreCase) { ["KEY"] = 2 };

		orderedDest.DeepCloneFrom(orderedSource);
		concurrentDest.DeepCloneFrom(concurrentSource);

		Assert.AreEqual(1, orderedDest["key"]);
		Assert.AreEqual(2, concurrentDest["key"]);
	}

	/// <summary>
	/// Tests that matching comparers still clone, keeping the destination's (and so the source's) behaviour.
	/// </summary>
	[TestMethod]
	public void SameComparer_Clones()
	{
		HashSet<string> dest = new(StringComparer.OrdinalIgnoreCase);
		HashSet<string> source = new(StringComparer.OrdinalIgnoreCase) { "a" };
		SortedSet<int> sortedDest = new(Descending);
		SortedSet<int> sortedSource = new(Descending) { 1, 2, 3 };

		dest.DeepCloneFrom(source);
		sortedDest.DeepCloneFrom(sortedSource);

		Assert.Contains("A", dest);
		CollectionAssert.AreEqual(DescendingOrder, sortedDest.ToArray());
	}

	/// <summary>
	/// Tests that an explicit <see cref="StringComparer.Ordinal"/> and the default string comparer, which compare the
	/// same way, are not reported as a mismatch.
	/// </summary>
	[TestMethod]
	public void OrdinalAndDefaultStringComparers_Clone()
	{
		HashSet<string> dest = [];
		HashSet<string> source = new(StringComparer.Ordinal) { "a", "A" };
		Dictionary<string, int> mapDest = new(StringComparer.Ordinal);
		Dictionary<string, int> mapSource = new() { ["a"] = 1 };

		dest.DeepCloneFrom(source);
		mapDest.DeepCloneFrom(mapSource);

		Assert.HasCount(2, dest);
		Assert.AreEqual(1, mapDest["a"]);
	}

	/// <summary>
	/// Tests that a destination of a different kind from the source is filled as before, with no comparer check.
	/// </summary>
	[TestMethod]
	public void DifferentKinds_Clone()
	{
		HashSet<string> dest = new(StringComparer.OrdinalIgnoreCase);
		List<string> source = ["a"];

		dest.DeepCloneFrom(source);

		Assert.Contains("A", dest);
	}
}
