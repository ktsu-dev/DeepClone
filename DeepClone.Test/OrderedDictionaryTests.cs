// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

/// <summary>
/// Tests that an <see cref="OrderedDictionary{TKey, TValue}"/> clones without a cast, stays ordered, and keeps the
/// source's comparer, including when cloned through <see cref="IDictionary{TKey, TValue}"/> or
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> (ktsu-dev/DeepClone#108).
/// </summary>
[TestClass]
public class OrderedDictionaryTests
{
	private static readonly string[] InsertionOrder = ["c", "a", "b"];

	/// <summary>
	/// Tests that an ordered dictionary clones to an ordered dictionary with the source's comparer and key order.
	/// </summary>
	[TestMethod]
	public void OrderedDictionary_DeepClone_KeepsTypeComparerAndOrder()
	{
		OrderedDictionary<string, int> original = new(StringComparer.OrdinalIgnoreCase) { ["c"] = 3, ["a"] = 1, ["b"] = 2 };

		OrderedDictionary<string, int> clone = original.DeepClone();

		Assert.AreNotSame(original, clone);
		Assert.AreSame(StringComparer.OrdinalIgnoreCase, clone.Comparer);
		CollectionAssert.AreEqual(InsertionOrder, clone.Keys.ToArray());
		Assert.IsTrue(clone.ContainsKey("C"));
	}

	/// <summary>
	/// Tests that an ordered dictionary's values are deep cloned.
	/// </summary>
	[TestMethod]
	public void OrderedDictionary_DeepClone_ClonesValues()
	{
		Mocks.Shapes.Circle circle = new() { Radius = 2 };
		OrderedDictionary<string, Mocks.Shapes.Circle> original = new() { ["c"] = circle };

		OrderedDictionary<string, Mocks.Shapes.Circle> clone = original.DeepClone();

		Assert.AreNotSame(circle, clone["c"]);
		Assert.AreEqual(2, clone["c"].Radius);
	}

	/// <summary>
	/// Tests that an ordered dictionary cloned through IDictionary stays ordered and keeps its comparer.
	/// </summary>
	[TestMethod]
	public void OrderedDictionary_ThroughIDictionary_KeepsTypeAndComparer()
	{
		OrderedDictionary<string, int> original = new(StringComparer.OrdinalIgnoreCase) { ["c"] = 3, ["a"] = 1, ["b"] = 2 };

		IDictionary<string, int> clone = ((IDictionary<string, int>)original).DeepClone();

		Assert.IsInstanceOfType<OrderedDictionary<string, int>>(clone);
		Assert.IsTrue(clone.ContainsKey("A"));
		CollectionAssert.AreEqual(InsertionOrder, clone.Keys.ToArray());
	}

	/// <summary>
	/// Tests that an ordered dictionary cloned through IReadOnlyDictionary keeps its comparer.
	/// </summary>
	[TestMethod]
	public void OrderedDictionary_ThroughIReadOnlyDictionary_KeepsComparer()
	{
		OrderedDictionary<string, int> original = new(StringComparer.OrdinalIgnoreCase) { ["Key"] = 1 };

		IReadOnlyDictionary<string, int> clone = ((IReadOnlyDictionary<string, int>)original).DeepClone();

		Assert.IsTrue(clone.ContainsKey("KEY"));
	}

	/// <summary>
	/// Tests that an ordered dictionary using reference equality, holding two equal but distinct keys,
	/// clones without throwing and keeps both entries.
	/// </summary>
	[TestMethod]
	public void OrderedDictionary_WithReferenceEquality_ClonesEqualKeys()
	{
		OrderedDictionary<object, int> original = new(ReferenceEqualityComparer.Instance)
		{
			[new string('k', 1)] = 1,
			[new string('k', 1)] = 2,
		};

		IDictionary<object, int> clone = ((IDictionary<object, int>)original).DeepClone();
		OrderedDictionary<object, int> orderedClone = original.DeepClone();

		Assert.HasCount(2, clone);
		Assert.HasCount(2, orderedClone);
	}
}
