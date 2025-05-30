// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test;

using System.Collections.Concurrent;

/// <summary>
/// Tests for specialized collection types not covered by other tests.
/// </summary>
[TestClass]
public class SpecializedCollectionTests
{
	/// <summary>
	/// Tests deep cloning a HashSet.
	/// </summary>
	[TestMethod]
	public void HashSet_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		var original = new HashSet<SimpleObject>
		{
			new() { Id = 1, Name = "Item1" },
			new() { Id = 2, Name = "Item2" }
		};

		// Act
		var clone = DeepCloneContainerExtensions.DeepClone(original);

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		var originalItem = original.First(item => item.Id == 1);
		var cloneItem = clone.First(item => item.Id == 1);

		// Verify independence
		cloneItem.Name = "Modified";
		Assert.AreEqual("Modified", cloneItem.Name);
		Assert.AreEqual("Item1", originalItem.Name);
	}

	/// <summary>
	/// Tests deep cloning a SortedSet.
	/// </summary>
	[TestMethod]
	public void SortedSet_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		var comparer = Comparer<SimpleObject>.Create((x, y) => x.Id.CompareTo(y.Id));
		var original = new SortedSet<SimpleObject>(comparer)
		{
			new() { Id = 2, Name = "Item2" },
			new() { Id = 1, Name = "Item1" }
		};

		// Act
		var clone = DeepCloneContainerExtensions.DeepClone(original);

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// First item should be the one with smallest ID
		var originalFirst = original.First();
		var cloneFirst = clone.First();

		Assert.AreEqual(1, originalFirst.Id);
		Assert.AreEqual(1, cloneFirst.Id);

		// Verify independence
		cloneFirst.Name = "Modified";
		Assert.AreEqual("Modified", cloneFirst.Name);
		Assert.AreEqual("Item1", originalFirst.Name);
	}

	/// <summary>
	/// Tests deep cloning a Stack.
	/// </summary>
	[TestMethod]
	public void Stack_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		var original = new Stack<SimpleObject>();
		original.Push(new SimpleObject { Id = 1, Name = "Item1" });
		original.Push(new SimpleObject { Id = 2, Name = "Item2" });

		// Stack is LIFO, so Id=2 is on top of the stack now
		Assert.AreEqual(2, original.Peek().Id);

		// Act - Deep clone the stack
		IEnumerable<SimpleObject> clonedItems = DeepCloneContainerExtensions.DeepClone(original);

		// The DeepClone method just returns an IEnumerable of cloned items
		// It doesn't preserve Stack as a special type, so we need to create a new Stack

		// Check the enumeration order
		var itemsList = clonedItems.ToList();

		// In the cloned enumeration, items maintain LIFO order - the top of the stack (Id=2) comes first
		Assert.AreEqual(2, itemsList[0].Id, "First item in enumeration is the top item from original stack");
		Assert.AreEqual(1, itemsList[1].Id, "Second item in enumeration is the bottom item from original stack");

		// When we create a new Stack from this enumeration, the order will be reversed again
		// because Stack(IEnumerable) pushes items in the order they're enumerated
		var clone = new Stack<SimpleObject>(clonedItems);

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// The top of the new stack will be the last item pushed, which was the bottom item (Id=1)
		Assert.AreEqual(2, original.Peek().Id, "Top of original stack is Id=2");
		Assert.AreEqual(1, clone.Peek().Id, "Top of cloned stack is Id=1 due to reversed order");

		// Verify independence - modify the clone, verify original unchanged
		var cloneTop = clone.Peek();
		cloneTop.Name = "Modified";
		Assert.AreEqual("Modified", cloneTop.Name);

		// The original item with Id=1 should be unchanged
		var originalItem1 = original.FirstOrDefault(item => item.Id == 1);
		Assert.IsNotNull(originalItem1);
		Assert.AreEqual("Item1", originalItem1.Name);
	}

	/// <summary>
	/// Tests deep cloning a Queue.
	/// </summary>
	[TestMethod]
	public void Queue_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		var original = new Queue<SimpleObject>();
		original.Enqueue(new SimpleObject { Id = 1, Name = "Item1" });
		original.Enqueue(new SimpleObject { Id = 2, Name = "Item2" });

		// Act - create a new Queue from the cloned IEnumerable
		var clone = new Queue<SimpleObject>(DeepCloneContainerExtensions.DeepClone(original));

		// Convert to arrays to examine without modifying the queues
		var originalArray = original.ToArray();
		var cloneArray = clone.ToArray();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);
		Assert.AreEqual(originalArray.Length, cloneArray.Length);

		// Verify order preserved
		Assert.AreEqual(1, originalArray[0].Id);
		Assert.AreEqual(1, cloneArray[0].Id);
		Assert.AreEqual(2, originalArray[1].Id);
		Assert.AreEqual(2, cloneArray[1].Id);

		// Verify independence
		var originalItem = original.Peek();
		var cloneItem = clone.Peek();

		cloneItem.Name = "Modified";
		Assert.AreEqual("Modified", cloneItem.Name);
		Assert.AreEqual("Item1", originalItem.Name);
	}

	/// <summary>
	/// Tests deep cloning a LinkedList.
	/// </summary>
	[TestMethod]
	public void LinkedList_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		var original = new LinkedList<SimpleObject>();
		original.AddLast(new SimpleObject { Id = 1, Name = "Item1" });
		original.AddLast(new SimpleObject { Id = 2, Name = "Item2" });

		// Act - create a new LinkedList from the cloned IEnumerable
		var clone = new LinkedList<SimpleObject>(DeepCloneContainerExtensions.DeepClone(original));

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		var originalFirst = original.First;
		var cloneFirst = clone.First;

		Assert.IsNotNull(originalFirst);
		Assert.IsNotNull(cloneFirst);
		Assert.AreEqual(1, originalFirst.Value.Id);
		Assert.AreEqual(1, cloneFirst.Value.Id);

		// Verify independence
		cloneFirst.Value.Name = "Modified";
		Assert.AreEqual("Modified", cloneFirst.Value.Name);
		Assert.AreEqual("Item1", originalFirst.Value.Name);
	}

	/// <summary>
	/// Tests deep cloning a ConcurrentDictionary.
	/// </summary>
	[TestMethod]
	public void ConcurrentDictionary_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		var original = new ConcurrentDictionary<int, SimpleObject>();
		original.TryAdd(1, new SimpleObject { Id = 1, Name = "Item1" });
		original.TryAdd(2, new SimpleObject { Id = 2, Name = "Item2" });

		// Act - explicit cast to IDictionary to resolve ambiguity
		IDictionary<int, SimpleObject> originalDict = original;
		var clone = DeepCloneContainerExtensions.DeepClone(originalDict);

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreEqual(original.Count, clone.Count);

		// Verify keys are present
		Assert.IsTrue(clone.ContainsKey(1));
		Assert.IsTrue(clone.ContainsKey(2));

		// Verify values match
		Assert.AreEqual(1, clone[1].Id);
		Assert.AreEqual("Item1", clone[1].Name);
		Assert.AreEqual(2, clone[2].Id);
		Assert.AreEqual("Item2", clone[2].Name);

		// Verify independence - modify the clone, verify original unchanged
		clone[1].Name = "Modified";
		Assert.AreEqual("Modified", clone[1].Name);
		Assert.AreEqual("Item1", original[1].Name);

		// Verify adding to clone doesn't affect original
		clone.Add(3, new SimpleObject { Id = 3, Name = "Item3" });
		Assert.AreEqual(3, clone.Count);
		Assert.AreEqual(2, original.Count);
		Assert.IsFalse(original.ContainsKey(3));
	}
}

#pragma warning restore IDE0007
#pragma warning restore IDE0028
#pragma warning restore IDE0005
#pragma warning restore IDE0161
#pragma warning restore IDE0055
