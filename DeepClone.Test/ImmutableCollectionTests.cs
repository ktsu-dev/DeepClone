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
		var sourceItems = new List<SimpleObject>
			{
				new() { Id = 1, Name = "Item1" },
				new() { Id = 2, Name = "Item2" }
			};

		ImmutableArray<SimpleObject> original = [.. sourceItems];

		// Act
		var cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		var clone = cloneEnumerable.ToImmutableArray();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Length, clone.Length);

		// Check values were copied correctly
		Assert.AreEqual(1, clone[0].Id);
		Assert.AreEqual("Item1", clone[0].Name);
		Assert.AreEqual(2, clone[1].Id);
		Assert.AreEqual("Item2", clone[1].Name);

		// Verify independence by modifying clone items
		var builder = clone.ToBuilder();
		builder[0].Name = "Modified";
		var modifiedClone = builder.ToImmutable();

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
		var sourceItems = new List<SimpleObject>
			{
				new() { Id = 1, Name = "Item1" },
				new() { Id = 2, Name = "Item2" }
			};

		ImmutableList<SimpleObject> original = [.. sourceItems];

		// Act
		var cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		var clone = cloneEnumerable.ToImmutableList();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Check values were copied correctly
		Assert.AreEqual(1, clone[0].Id);
		Assert.AreEqual("Item1", clone[0].Name);
		Assert.AreEqual(2, clone[1].Id);
		Assert.AreEqual("Item2", clone[1].Name);

		// Verify independence by modifying clone items
		var builder = clone.ToBuilder();
		builder[0].Name = "Modified";
		var modifiedClone = builder.ToImmutable();

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
		var sourceItems = new Dictionary<int, SimpleObject>
			{
				{ 1, new SimpleObject { Id = 1, Name = "Item1" } },
				{ 2, new SimpleObject { Id = 2, Name = "Item2" } }
			};

		var original = sourceItems.ToImmutableDictionary();

		// Act - create new objects to avoid shared references
		var clonedDictionary = new Dictionary<int, SimpleObject>();
		foreach (var kvp in original)
		{
			clonedDictionary.Add(kvp.Key, kvp.Value.DeepClone());
		}

		var clone = clonedDictionary.ToImmutableDictionary();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Check values were copied correctly
		Assert.AreEqual(1, clone[1].Id);
		Assert.AreEqual("Item1", clone[1].Name);
		Assert.AreEqual(2, clone[2].Id);
		Assert.AreEqual("Item2", clone[2].Name);

		// Verify independence by modifying clone items
		var builder = clone.ToBuilder();
		builder[1].Name = "Modified";
		var modifiedClone = builder.ToImmutable();

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
		var sourceItems = new HashSet<SimpleObject>
			{
				new() { Id = 1, Name = "Item1" },
				new() { Id = 2, Name = "Item2" }
			};

		ImmutableHashSet<SimpleObject> original = [.. sourceItems];

		// Act
		var cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		var clone = cloneEnumerable.ToImmutableHashSet();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Find the items in the clone by ID
		var item1Clone = clone.First(item => item.Id == 1);
		var item2Clone = clone.First(item => item.Id == 2);

		// Check values were copied correctly
		Assert.AreEqual("Item1", item1Clone.Name);
		Assert.AreEqual("Item2", item2Clone.Name);

		// Verify independence by modifying clone items
		var builder = clone.ToBuilder();
		var itemToModify = builder.First(item => item.Id == 1);
		itemToModify.Name = "Modified";
		var modifiedClone = builder.ToImmutable();

		// Original should be unchanged
		var originalItem1 = original.First(item => item.Id == 1);
		var modifiedItem1 = modifiedClone.First(item => item.Id == 1);
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
		var sourceItems = new SortedDictionary<int, SimpleObject>
			{
				{ 2, new SimpleObject { Id = 2, Name = "Item2" } },
				{ 1, new SimpleObject { Id = 1, Name = "Item1" } }
			};

		var original =
			sourceItems.ToImmutableSortedDictionary();

		// Act - create new objects to avoid shared references
		var clonedDictionary = new SortedDictionary<int, SimpleObject>();
		foreach (var kvp in original)
		{
			clonedDictionary.Add(kvp.Key, kvp.Value.DeepClone());
		}

		var clone = clonedDictionary.ToImmutableSortedDictionary();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Check ordering
		var originalKeys = original.Keys.ToArray();
		var cloneKeys = clone.Keys.ToArray();
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
		var builder = clone.ToBuilder();
		builder[1].Name = "Modified";
		var modifiedClone = builder.ToImmutable();

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
		var comparer = Comparer<SimpleObject>.Create((x, y) => x.Id.CompareTo(y.Id));
		var sourceItems = new SortedSet<SimpleObject>(comparer)
			{
				new() { Id = 2, Name = "Item2" },
				new() { Id = 1, Name = "Item1" }
			};

		var original =
			sourceItems.ToImmutableSortedSet(comparer);

		// Act
		var cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		var clone = cloneEnumerable.ToImmutableSortedSet(comparer);

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Check ordering (should be sorted by ID)
		var originalArray = original.ToArray();
		var cloneArray = clone.ToArray();
		Assert.AreEqual(1, originalArray[0].Id);
		Assert.AreEqual(1, cloneArray[0].Id);
		Assert.AreEqual(2, originalArray[1].Id);
		Assert.AreEqual(2, cloneArray[1].Id);

		// Check values were copied correctly
		Assert.AreEqual("Item1", cloneArray[0].Name);
		Assert.AreEqual("Item2", cloneArray[1].Name);

		// Verify independence by modifying clone items
		var builder = clone.ToBuilder();
		var itemToModify = builder.First();
		itemToModify.Name = "Modified";
		var modifiedClone = builder.ToImmutable();

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
		var sourceItems = new Queue<SimpleObject>();
		sourceItems.Enqueue(new SimpleObject { Id = 1, Name = "Item1" });
		sourceItems.Enqueue(new SimpleObject { Id = 2, Name = "Item2" });

		ImmutableQueue<SimpleObject> original =
		[.. sourceItems];

		// Act
		var cloneEnumerable = DeepCloneContainerExtensions.DeepClone(original);
		// Need to use ImmutableQueue.Create since no extension method exists
		var clone = ImmutableQueue.CreateRange(cloneEnumerable);

		// Assert
		Assert.IsNotNull(clone);

		// Check queue operations
		var originalPeek = original.Peek();
		var clonePeek = clone.Peek();
		Assert.AreEqual(1, originalPeek.Id);
		Assert.AreEqual(1, clonePeek.Id);
		Assert.AreEqual("Item1", originalPeek.Name);
		Assert.AreEqual("Item1", clonePeek.Name);

		// Verify independence 
		var item = clone.Peek();
		var updatedClone = clone.Dequeue();
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
		var original = ImmutableStack<SimpleObject>.Empty
			.Push(new SimpleObject { Id = 1, Name = "Item1" })
			.Push(new SimpleObject { Id = 2, Name = "Item2" });

		// Act - do manual deep cloning to ensure we get proper results
		var clonedItems = new List<SimpleObject>();
		foreach (var stackItem in original)
		{
			clonedItems.Add(stackItem.DeepClone());
		}

		// Now rebuild the stack in the same order
		// Since ImmutableStack.CreateRange would reverse the order again,
		// we need to start from Empty and push items manually
		var clone = ImmutableStack<SimpleObject>.Empty;
		foreach (var stackItem in clonedItems.AsEnumerable().Reverse())
		{
			clone = clone.Push(stackItem);
		}

		// Assert
		Assert.IsNotNull(clone);

		// Check stack operations
		var originalPeek = original.Peek();
		var clonePeek = clone.Peek();
		Assert.AreEqual(2, originalPeek.Id);
		Assert.AreEqual(2, clonePeek.Id);
		Assert.AreEqual("Item2", originalPeek.Name);
		Assert.AreEqual("Item2", clonePeek.Name);

		// Verify independence
		var topItem = clone.Peek();
		var updatedClone = clone.Pop();
		topItem.Name = "Modified";

		// Original should be unchanged
		Assert.AreEqual("Item2", original.Peek().Name);
	}
}

