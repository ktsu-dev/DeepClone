// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.Concurrent;

/// <summary>
/// Tests that <c>.DeepClone()</c> on a <see cref="ConcurrentStack{T}"/> returns a concurrent stack with the same pop
/// order and deep-cloned elements, instead of a top-first sequence that reverses when rebuilt (ktsu-dev/DeepClone#102).
/// </summary>
[TestClass]
public class ConcurrentStackCloneTests
{
	private static readonly int[] PopOrder = [3, 2, 1];

	/// <summary>
	/// Tests that the clone is a concurrent stack that pops in the same order as the source.
	/// </summary>
	[TestMethod]
	public void ConcurrentStack_DeepClone_PreservesTypeAndPopOrder()
	{
		ConcurrentStack<SimpleObject> original = new();
		original.Push(new SimpleObject { Id = 1, Name = "Bottom" });
		original.Push(new SimpleObject { Id = 2, Name = "Middle" });
		original.Push(new SimpleObject { Id = 3, Name = "Top" });

		object result = original.DeepClone();

		Assert.IsInstanceOfType<ConcurrentStack<SimpleObject>>(result);
		ConcurrentStack<SimpleObject> clone = (ConcurrentStack<SimpleObject>)result;
		List<int> popped = [];
		while (clone.TryPop(out SimpleObject? item))
		{
			popped.Add(item.Id);
		}

		Assert.AreSequenceEqual(PopOrder, popped);
		Assert.HasCount(3, original);
	}

	/// <summary>
	/// Tests that the clone's elements are deep clones rather than the source's instances.
	/// </summary>
	[TestMethod]
	public void ConcurrentStack_DeepClone_ClonesElements()
	{
		ConcurrentStack<SimpleObject> original = new();
		original.Push(new SimpleObject { Id = 1, Name = "Bottom" });
		original.Push(new SimpleObject { Id = 2, Name = "Top" });

		object result = original.DeepClone();

		Assert.IsInstanceOfType<ConcurrentStack<SimpleObject>>(result);
		SimpleObject[] sourceItems = [.. original];
		SimpleObject[] cloneItems = [.. (ConcurrentStack<SimpleObject>)result];
		Assert.HasCount(sourceItems.Length, cloneItems);
		for (int i = 0; i < sourceItems.Length; i++)
		{
			Assert.AreNotSame(sourceItems[i], cloneItems[i]);
			Assert.AreEqual(sourceItems[i].Id, cloneItems[i].Id);
			Assert.AreEqual(sourceItems[i].Name, cloneItems[i].Name);
		}
	}

	/// <summary>
	/// Tests that an empty concurrent stack clones to an empty concurrent stack.
	/// </summary>
	[TestMethod]
	public void ConcurrentStack_DeepClone_Empty()
	{
		ConcurrentStack<SimpleObject> clone = new ConcurrentStack<SimpleObject>().DeepClone();

		Assert.IsEmpty(clone);
	}
}
