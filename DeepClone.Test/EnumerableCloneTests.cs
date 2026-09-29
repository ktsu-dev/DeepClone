// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

/// <summary>
/// Tests for deep cloning plain sequences and stacks through their extension methods.
/// </summary>
[TestClass]
public class EnumerableCloneTests
{
#pragma warning disable CA1851 // Enumerating the clone more than once is what these tests check
	/// <summary>
	/// Tests that edits to the items of a cloned sequence survive a second enumeration.
	/// </summary>
	[TestMethod]
	public void Enumerable_DeepClone_ShouldKeepEditsAcrossEnumerations()
	{
		// Arrange
		List<SimpleObject> original = [new() { Id = 1, Name = "Item1" }];

		// Act
		IEnumerable<SimpleObject> clone = original.DeepClone();
		foreach (SimpleObject item in clone)
		{
			item.Name = "Edited";
		}

		// Assert
		Assert.AreEqual("Edited", clone.First().Name);
		Assert.AreSame(clone.First(), clone.First());
		Assert.AreEqual("Item1", original[0].Name);
	}

	/// <summary>
	/// Tests that a cloned sequence does not change when the source changes afterwards.
	/// </summary>
	[TestMethod]
	public void Enumerable_DeepClone_ShouldNotTrackSourceChanges()
	{
		// Arrange
		List<SimpleObject> original =
		[
			new() { Id = 1, Name = "Item1" },
			new() { Id = 2, Name = "Item2" },
		];

		// Act
		IEnumerable<SimpleObject> clone = original.DeepClone();
		original.Clear();

		// Assert
		Assert.AreEqual(2, clone.Count());
	}
#pragma warning restore CA1851

	/// <summary>
	/// Tests that deep cloning a null sequence throws ArgumentNullException.
	/// </summary>
	[TestMethod]
	public void Enumerable_DeepClone_NullSource_ShouldThrow()
	{
		IEnumerable<SimpleObject> source = null!;
		Assert.ThrowsExactly<ArgumentNullException>(() => source.DeepClone());
	}

	/// <summary>
	/// Tests that deep cloning a null stack throws ArgumentNullException.
	/// </summary>
	[TestMethod]
	public void Stack_DeepClone_NullSource_ShouldThrow()
	{
		Stack<SimpleObject> source = null!;
		Assert.ThrowsExactly<ArgumentNullException>(() => source.DeepClone());
	}

	/// <summary>
	/// Tests that cloning from a read-only dictionary into a dictionary clones the values, rather than
	/// copying the pairs and sharing the value instances with the source (ktsu-dev/DeepClone#82).
	/// </summary>
	[TestMethod]
	public void DeepCloneFrom_ReadOnlyDictionarySource_ShouldCloneKeysAndValues()
	{
		// Arrange
		Dictionary<SimpleObject, SimpleObject> original = new()
		{
			[new() { Id = 1, Name = "Key" }] = new() { Id = 2, Name = "Value" },
		};
		Dictionary<SimpleObject, SimpleObject> dest = [];

		// Act
		dest.DeepCloneFrom((IReadOnlyDictionary<SimpleObject, SimpleObject>)original);

		// Assert
		KeyValuePair<SimpleObject, SimpleObject> source = original.Single();
		KeyValuePair<SimpleObject, SimpleObject> clone = dest.Single();
		Assert.AreNotSame(source.Key, clone.Key);
		Assert.AreNotSame(source.Value, clone.Value);
		Assert.AreEqual("Key", clone.Key.Name);
		Assert.AreEqual("Value", clone.Value.Name);
	}

	/// <summary>
	/// Tests that deep cloning a LINQ query over a dictionary clones each pair's key and value
	/// (ktsu-dev/DeepClone#82).
	/// </summary>
	[TestMethod]
	public void Enumerable_DeepClone_LinqOverDictionary_ShouldCloneKeysAndValues()
	{
		// Arrange
		Dictionary<string, SimpleObject> original = new()
		{
			["a"] = new() { Id = 1, Name = "Item1" },
			["b"] = new() { Id = 2, Name = "Item2" },
		};

		// Act
		List<KeyValuePair<string, SimpleObject>> clone = [.. original.Where(_ => true).DeepClone()];

		// Assert
		Assert.HasCount(2, clone);
		foreach (KeyValuePair<string, SimpleObject> pair in clone)
		{
			Assert.AreNotSame(original[pair.Key], pair.Value);
			Assert.AreEqual(original[pair.Key].Name, pair.Value.Name);
		}
	}

	/// <summary>
	/// Tests that a pair whose key and value are not cloneable is copied through unchanged.
	/// </summary>
	[TestMethod]
	public void Enumerable_DeepClone_PairsOfNonCloneables_ShouldKeepTheirContents()
	{
		// Arrange
		KeyValuePair<string, int>[] original = [new("a", 1), new("b", 2)];

		// Act
		KeyValuePair<string, int>[] clone = [.. original.AsEnumerable().DeepClone()];

		// Assert
		CollectionAssert.AreEqual(original, clone);
	}
}
