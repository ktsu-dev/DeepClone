// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.Immutable;

/// <summary>
/// Tests that <c>.DeepClone()</c> can be called directly on dictionary types that implement both
/// <see cref="IDictionary{TKey, TValue}"/> and <see cref="IReadOnlyDictionary{TKey, TValue}"/>, and that the
/// result has the source's type and comparer (ktsu-dev/DeepClone#86). Each test calls the extension without
/// a cast, so this file stops compiling if the overloads become ambiguous again.
/// </summary>
[TestClass]
public class DirectDictionaryOverloadTests
{
	private static readonly string[] DescendingKeys = ["c", "b", "a"];

	/// <summary>
	/// Tests that a sorted list clones to a sorted list with the source's comparer and deep-cloned values.
	/// </summary>
	[TestMethod]
	public void SortedList_DeepClone_KeepsTypeComparerAndClonesValues()
	{
		Comparer<string> descending = Comparer<string>.Create((x, y) => string.CompareOrdinal(y, x));
		SortedList<string, SimpleObject> original = new(descending)
		{
			["a"] = new SimpleObject { Id = 1, Name = "A" },
			["c"] = new SimpleObject { Id = 3, Name = "C" },
		};

		SortedList<string, SimpleObject> clone = original.DeepClone();
		clone["b"] = new SimpleObject { Id = 2, Name = "B" };

		Assert.AreSame(descending, clone.Comparer);
		Assert.AreSequenceEqual(DescendingKeys, clone.Keys);
		Assert.AreNotSame(original["a"], clone["a"]);
		Assert.AreEqual(1, clone["a"].Id);
	}

	/// <summary>
	/// Tests that an immutable dictionary clones to an immutable dictionary with the source's key and value
	/// comparers and deep-cloned values.
	/// </summary>
	[TestMethod]
	public void ImmutableDictionary_DeepClone_KeepsTypeComparersAndClonesValues()
	{
		ImmutableDictionary<string, SimpleObject> original = ImmutableDictionary
			.Create<string, SimpleObject>(StringComparer.OrdinalIgnoreCase, ReferenceEqualityComparer.Instance)
			.Add("Key", new SimpleObject { Id = 1, Name = "One" });

		ImmutableDictionary<string, SimpleObject> clone = original.DeepClone();

		Assert.AreSame(StringComparer.OrdinalIgnoreCase, clone.KeyComparer);
		Assert.AreSame(ReferenceEqualityComparer.Instance, clone.ValueComparer);
		Assert.IsTrue(clone.ContainsKey("KEY"));
		Assert.AreNotSame(original["Key"], clone["Key"]);
		Assert.AreEqual(1, clone["Key"].Id);
	}

	/// <summary>
	/// Tests that an immutable sorted dictionary clones to an immutable sorted dictionary with the source's
	/// key and value comparers and deep-cloned values.
	/// </summary>
	[TestMethod]
	public void ImmutableSortedDictionary_DeepClone_KeepsTypeComparersAndClonesValues()
	{
		Comparer<string> descending = Comparer<string>.Create((x, y) => string.CompareOrdinal(y, x));
		ImmutableSortedDictionary<string, SimpleObject> original = ImmutableSortedDictionary
			.Create<string, SimpleObject>(descending, ReferenceEqualityComparer.Instance)
			.Add("a", new SimpleObject { Id = 1, Name = "A" })
			.Add("c", new SimpleObject { Id = 3, Name = "C" });

		ImmutableSortedDictionary<string, SimpleObject> clone = original.DeepClone().Add("b", new SimpleObject { Id = 2, Name = "B" });

		Assert.AreSame(descending, clone.KeyComparer);
		Assert.AreSame(ReferenceEqualityComparer.Instance, clone.ValueComparer);
		Assert.AreSequenceEqual(DescendingKeys, clone.Keys);
		Assert.AreNotSame(original["a"], clone["a"]);
		Assert.AreEqual(1, clone["a"].Id);
	}
}
