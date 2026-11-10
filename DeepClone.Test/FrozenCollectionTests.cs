// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.Frozen;

/// <summary>
/// Tests that frozen dictionaries and sets clone without a cast, stay frozen, and keep the source's comparer,
/// including when cloned through <see cref="IDictionary{TKey, TValue}"/> or
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> (ktsu-dev/DeepClone#101).
/// </summary>
[TestClass]
public class FrozenCollectionTests
{
	private static readonly string[] Keys = ["Key"];

	/// <summary>
	/// Tests that a frozen dictionary clones to a frozen dictionary with the source's comparer.
	/// </summary>
	[TestMethod]
	public void FrozenDictionary_DeepClone_KeepsTypeAndComparer()
	{
		FrozenDictionary<string, int> original = new Dictionary<string, int> { ["Key"] = 1 }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

		FrozenDictionary<string, int> clone = original.DeepClone();

		Assert.AreNotSame(original, clone);
		Assert.AreSame(StringComparer.OrdinalIgnoreCase, clone.Comparer);
		Assert.IsTrue(clone.ContainsKey("KEY"));
		Assert.AreEqual(1, clone["key"]);
	}

	/// <summary>
	/// Tests that a frozen dictionary's values are deep cloned.
	/// </summary>
	[TestMethod]
	public void FrozenDictionary_DeepClone_ClonesValues()
	{
		Mocks.Shapes.Circle circle = new() { Radius = 2 };
		FrozenDictionary<string, Mocks.Shapes.Circle> original = new Dictionary<string, Mocks.Shapes.Circle> { ["c"] = circle }.ToFrozenDictionary();

		FrozenDictionary<string, Mocks.Shapes.Circle> clone = original.DeepClone();

		Assert.AreNotSame(circle, clone["c"]);
		Assert.AreEqual(2, clone["c"].Radius);
	}

	/// <summary>
	/// Tests that a frozen dictionary cloned through IDictionary keeps its comparer.
	/// </summary>
	[TestMethod]
	public void FrozenDictionary_ThroughIDictionary_KeepsComparer()
	{
		FrozenDictionary<string, int> original = new Dictionary<string, int> { ["Key"] = 1 }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

		IDictionary<string, int> clone = ((IDictionary<string, int>)original).DeepClone();

		Assert.IsTrue(clone.ContainsKey("KEY"));
	}

	/// <summary>
	/// Tests that a frozen dictionary cloned through IReadOnlyDictionary keeps its comparer.
	/// </summary>
	[TestMethod]
	public void FrozenDictionary_ThroughIReadOnlyDictionary_KeepsComparer()
	{
		FrozenDictionary<string, int> original = new Dictionary<string, int> { ["Key"] = 1 }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

		IReadOnlyDictionary<string, int> clone = ((IReadOnlyDictionary<string, int>)original).DeepClone();

		Assert.IsTrue(clone.ContainsKey("KEY"));
	}

	/// <summary>
	/// Tests that a frozen dictionary using reference equality, holding two equal but distinct keys,
	/// clones through IDictionary without throwing and keeps both entries.
	/// </summary>
	[TestMethod]
	public void FrozenDictionary_WithReferenceEquality_ClonesEqualKeys()
	{
		Dictionary<object, int> source = new(ReferenceEqualityComparer.Instance)
		{
			[new string('k', 1)] = 1,
			[new string('k', 1)] = 2,
		};
		FrozenDictionary<object, int> original = source.ToFrozenDictionary(ReferenceEqualityComparer.Instance);

		IDictionary<object, int> clone = ((IDictionary<object, int>)original).DeepClone();
		FrozenDictionary<object, int> frozenClone = original.DeepClone();

		Assert.HasCount(2, clone);
		Assert.HasCount(2, frozenClone);
	}

	/// <summary>
	/// Tests that a frozen set clones to a frozen set with the source's comparer.
	/// </summary>
	[TestMethod]
	public void FrozenSet_DeepClone_KeepsTypeAndComparer()
	{
		FrozenSet<string> original = Keys.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		FrozenSet<string> clone = original.DeepClone();

		Assert.AreNotSame(original, clone);
		Assert.AreSame(StringComparer.OrdinalIgnoreCase, clone.Comparer);
		Assert.IsTrue(clone.Contains("KEY"));
	}

	/// <summary>
	/// Tests that a frozen set's items are deep cloned.
	/// </summary>
	[TestMethod]
	public void FrozenSet_DeepClone_ClonesItems()
	{
		Mocks.Shapes.Circle circle = new() { Radius = 3 };
		FrozenSet<Mocks.Shapes.Circle> original = new[] { circle }.ToFrozenSet();

		FrozenSet<Mocks.Shapes.Circle> clone = original.DeepClone();

		Assert.HasCount(1, clone);
		Assert.AreNotSame(circle, clone.Single());
		Assert.AreEqual(3, clone.Single().Radius);
	}
}
