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
}
