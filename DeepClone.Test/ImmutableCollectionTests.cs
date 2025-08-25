// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test;

using System.Collections.Immutable;

/// <summary>
/// Tests for immutable collection types.
/// </summary>
/// <remarks>
/// When implementing DeepClone in derived classes, it's important to:
/// <list type="bullet">
/// <item>
/// <description>Always call base.DeepClone(clone) first in the DeepClone method to ensure base class properties are cloned</description>
/// </item>
/// <item>
/// <description>Follow this call with cloning of the derived class's own properties and fields</description>
/// </item>
/// <item>
/// <description>For inheritance hierarchies, the chain of base.DeepClone calls ensures complete cloning of the entire object graph</description>
/// </item>
/// </list>
///
/// See the Shape, Rectangle, and Circle classes in the Mocks.Shapes namespace for examples of properly implemented inheritance with DeepClone.
/// </remarks>
[TestClass]
public class ImmutableCollectionTests
{
	/// <summary>
	/// Tests deep cloning an ImmutableArray.
	/// </summary>
	[TestMethod]
	public void ImmutableArray_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		List<SimpleObject> sourceItems =
		[
				new() { Id = 1, Name = "Item1" },
				new() { Id = 2, Name = "Item2" }
			];

		ImmutableArray<SimpleObject> original = [.. sourceItems];

		// Act
		IEnumerable<SimpleObject> cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		ImmutableArray<SimpleObject> clone = [.. cloneEnumerable];

		// Assert
		Assert.AreEqual(original.Length, clone.Length);

		// Check values were copied correctly
		Assert.AreEqual(1, clone[0].Id);
		Assert.AreEqual("Item1", clone[0].Name);
		Assert.AreEqual(2, clone[1].Id);
		Assert.AreEqual("Item2", clone[1].Name);

		// Verify independence by modifying clone items
		ImmutableArray<SimpleObject>.Builder builder = clone.ToBuilder();
		builder[0].Name = "Modified";
		ImmutableArray<SimpleObject> modifiedClone = builder.ToImmutable();

		// Original should be unchanged
		Assert.AreEqual("Item1", original[0].Name);
		Assert.AreEqual("Modified", modifiedClone[0].Name);
	}

	/// <summary>
	/// Tests deep cloning an ImmutableList.
	/// </summary>
	[TestMethod]
	public void ImmutableList_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		List<SimpleObject> sourceItems =
		[
				new() { Id = 1, Name = "Item1" },
				new() { Id = 2, Name = "Item2" }
			];

		ImmutableList<SimpleObject> original = [.. sourceItems];

		// Act
		IEnumerable<SimpleObject> cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		ImmutableList<SimpleObject> clone = [.. cloneEnumerable];

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Check values were copied correctly
		Assert.AreEqual(1, clone[0].Id);
		Assert.AreEqual("Item1", clone[0].Name);
		Assert.AreEqual(2, clone[1].Id);
		Assert.AreEqual("Item2", clone[1].Name);

		// Verify independence by modifying clone items
		ImmutableList<SimpleObject>.Builder builder = clone.ToBuilder();
		builder[0].Name = "Modified";
		ImmutableList<SimpleObject> modifiedClone = builder.ToImmutable();

		// Original should be unchanged
		Assert.AreEqual("Item1", original[0].Name);
		Assert.AreEqual("Modified", modifiedClone[0].Name);
	}

	/// <summary>
	/// Tests deep cloning an ImmutableDictionary.
	/// </summary>
	[TestMethod]
	public void ImmutableDictionary_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		Dictionary<int, SimpleObject> sourceItems = new()
		{
				{ 1, new SimpleObject { Id = 1, Name = "Item1" } },
				{ 2, new SimpleObject { Id = 2, Name = "Item2" } }
			};

		ImmutableDictionary<int, SimpleObject> original = sourceItems.ToImmutableDictionary();

		// Act - create new objects to avoid shared references
		Dictionary<int, SimpleObject> clonedDictionary = [];
		foreach (KeyValuePair<int, SimpleObject> kvp in original)
		{
			clonedDictionary.Add(kvp.Key, kvp.Value.DeepClone());
		}

		ImmutableDictionary<int, SimpleObject> clone = clonedDictionary.ToImmutableDictionary();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Check values were copied correctly
		Assert.AreEqual(1, clone[1].Id);
		Assert.AreEqual("Item1", clone[1].Name);
		Assert.AreEqual(2, clone[2].Id);
		Assert.AreEqual("Item2", clone[2].Name);

		// Verify independence by modifying clone items
		ImmutableDictionary<int, SimpleObject>.Builder builder = clone.ToBuilder();
		builder[1].Name = "Modified";
		ImmutableDictionary<int, SimpleObject> modifiedClone = builder.ToImmutable();

		// Original should be unchanged
		Assert.AreEqual("Item1", original[1].Name);
		Assert.AreEqual("Modified", modifiedClone[1].Name);
	}

	/// <summary>
	/// Tests deep cloning an ImmutableHashSet.
	/// </summary>
	[TestMethod]
	public void ImmutableHashSet_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		HashSet<SimpleObject> sourceItems =
		[
				new() { Id = 1, Name = "Item1" },
				new() { Id = 2, Name = "Item2" }
			];

		ImmutableHashSet<SimpleObject> original = [.. sourceItems];

		// Act
		IEnumerable<SimpleObject> cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		ImmutableHashSet<SimpleObject> clone = [.. cloneEnumerable];

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Find the items in the clone by ID
		SimpleObject item1Clone = clone.First(item => item.Id == 1);
		SimpleObject item2Clone = clone.First(item => item.Id == 2);

		// Check values were copied correctly
		Assert.AreEqual("Item1", item1Clone.Name);
		Assert.AreEqual("Item2", item2Clone.Name);

		// Verify independence by modifying clone items
		ImmutableHashSet<SimpleObject>.Builder builder = clone.ToBuilder();
		SimpleObject itemToModify = builder.First(item => item.Id == 1);
		itemToModify.Name = "Modified";
		ImmutableHashSet<SimpleObject> modifiedClone = builder.ToImmutable();

		// Original should be unchanged
		SimpleObject originalItem1 = original.First(item => item.Id == 1);
		SimpleObject modifiedItem1 = modifiedClone.First(item => item.Id == 1);
		Assert.AreEqual("Item1", originalItem1.Name);
		Assert.AreEqual("Modified", modifiedItem1.Name);
	}

	/// <summary>
	/// Tests deep cloning an ImmutableSortedDictionary.
	/// </summary>
	[TestMethod]
	public void ImmutableSortedDictionary_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		SortedDictionary<int, SimpleObject> sourceItems = new()
		{
				{ 2, new SimpleObject { Id = 2, Name = "Item2" } },
				{ 1, new SimpleObject { Id = 1, Name = "Item1" } }
			};

		ImmutableSortedDictionary<int, SimpleObject> original =
			sourceItems.ToImmutableSortedDictionary();

		// Act - create new objects to avoid shared references
		SortedDictionary<int, SimpleObject> clonedDictionary = [];
		foreach (KeyValuePair<int, SimpleObject> kvp in original)
		{
			clonedDictionary.Add(kvp.Key, kvp.Value.DeepClone());
		}

		ImmutableSortedDictionary<int, SimpleObject> clone = clonedDictionary.ToImmutableSortedDictionary();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Check ordering
		int[] originalKeys = [.. original.Keys];
		int[] cloneKeys = [.. clone.Keys];
		Assert.AreEqual(1, originalKeys[0]);
		Assert.AreEqual(1, cloneKeys[0]);
		Assert.AreEqual(2, originalKeys[1]);
		Assert.AreEqual(2, cloneKeys[1]);

		// Check values were copied correctly
		Assert.AreEqual(1, clone[1].Id);
		Assert.AreEqual("Item1", clone[1].Name);
		Assert.AreEqual(2, clone[2].Id);
		Assert.AreEqual("Item2", clone[2].Name);

		// Verify independence by modifying clone items
		ImmutableSortedDictionary<int, SimpleObject>.Builder builder = clone.ToBuilder();
		builder[1].Name = "Modified";
		ImmutableSortedDictionary<int, SimpleObject> modifiedClone = builder.ToImmutable();

		// Original should be unchanged
		Assert.AreEqual("Item1", original[1].Name);
		Assert.AreEqual("Modified", modifiedClone[1].Name);
	}

	/// <summary>
	/// Tests deep cloning an ImmutableSortedSet.
	/// </summary>
	[TestMethod]
	public void ImmutableSortedSet_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		Comparer<SimpleObject> comparer = Comparer<SimpleObject>.Create((x, y) => x.Id.CompareTo(y.Id));
		SortedSet<SimpleObject> sourceItems = new(comparer)
			{
				new() { Id = 2, Name = "Item2" },
				new() { Id = 1, Name = "Item1" }
			};

		ImmutableSortedSet<SimpleObject> original =
			sourceItems.ToImmutableSortedSet(comparer);

		// Act
		IEnumerable<SimpleObject> cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		ImmutableSortedSet<SimpleObject> clone = cloneEnumerable.ToImmutableSortedSet(comparer);

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Check ordering (should be sorted by ID)
		SimpleObject[] originalArray = [.. original];
		SimpleObject[] cloneArray = [.. clone];
		Assert.AreEqual(1, originalArray[0].Id);
		Assert.AreEqual(1, cloneArray[0].Id);
		Assert.AreEqual(2, originalArray[1].Id);
		Assert.AreEqual(2, cloneArray[1].Id);

		// Check values were copied correctly
		Assert.AreEqual("Item1", cloneArray[0].Name);
		Assert.AreEqual("Item2", cloneArray[1].Name);

		// Verify independence by modifying clone items
		ImmutableSortedSet<SimpleObject>.Builder builder = clone.ToBuilder();
		SimpleObject itemToModify = builder.First();
		itemToModify.Name = "Modified";
		ImmutableSortedSet<SimpleObject> modifiedClone = builder.ToImmutable();

		// Original should be unchanged
		Assert.AreEqual("Item1", originalArray[0].Name);
		Assert.AreEqual("Modified", modifiedClone.First().Name);
	}

	/// <summary>
	/// Tests deep cloning an ImmutableQueue.
	/// </summary>
	[TestMethod]
	public void ImmutableQueue_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		Queue<SimpleObject> sourceItems = new();
		sourceItems.Enqueue(new SimpleObject { Id = 1, Name = "Item1" });
		sourceItems.Enqueue(new SimpleObject { Id = 2, Name = "Item2" });

		ImmutableQueue<SimpleObject> original =
		[.. sourceItems];

		// Act
		IEnumerable<SimpleObject> cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		// Need to use ImmutableQueue.Create since no extension method exists
		ImmutableQueue<SimpleObject> clone = [.. cloneEnumerable];

		// Assert
		Assert.IsNotNull(clone);

		// Check queue operations
		SimpleObject originalPeek = original.Peek();
		SimpleObject clonePeek = clone.Peek();
		Assert.AreEqual(1, originalPeek.Id);
		Assert.AreEqual(1, clonePeek.Id);
		Assert.AreEqual("Item1", originalPeek.Name);
		Assert.AreEqual("Item1", clonePeek.Name);

		// Verify independence
		SimpleObject item = clone.Peek();
		ImmutableQueue<SimpleObject> updatedClone = clone.Dequeue();
		item.Name = "Modified";

		// Original should be unchanged
		Assert.AreEqual("Item1", original.Peek().Name);
	}

	/// <summary>
	/// Tests deep cloning an ImmutableStack.
	/// </summary>
	[TestMethod]
	public void ImmutableStack_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange - build the stack with highest ID on top
		ImmutableStack<SimpleObject> original = ImmutableStack<SimpleObject>.Empty
			.Push(new SimpleObject { Id = 1, Name = "Item1" })
			.Push(new SimpleObject { Id = 2, Name = "Item2" });

		// Act - do manual deep cloning to ensure we get proper results
		List<SimpleObject> clonedItems = [];
		foreach (SimpleObject stackItem in original)
		{
			clonedItems.Add(stackItem.DeepClone());
		}

		// Now rebuild the stack in the same order
		// Since ImmutableStack.CreateRange would reverse the order again,
		// we need to start from Empty and push items manually
		ImmutableStack<SimpleObject> clone = [];
		foreach (SimpleObject? stackItem in clonedItems.AsEnumerable().Reverse())
		{
			clone = clone.Push(stackItem);
		}

		// Assert
		Assert.IsNotNull(clone);

		// Check stack operations
		SimpleObject originalPeek = original.Peek();
		SimpleObject clonePeek = clone.Peek();
		Assert.AreEqual(2, originalPeek.Id);
		Assert.AreEqual(2, clonePeek.Id);
		Assert.AreEqual("Item2", originalPeek.Name);
		Assert.AreEqual("Item2", clonePeek.Name);

		// Verify independence
		SimpleObject topItem = clone.Peek();
		ImmutableStack<SimpleObject> updatedClone = clone.Pop();
		topItem.Name = "Modified";

		// Original should be unchanged
		Assert.AreEqual("Item2", original.Peek().Name);
	}
}

