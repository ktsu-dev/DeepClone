// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.Concurrent;

/// <summary>
/// Tests that <c>.DeepClone()</c> on a <see cref="ConcurrentQueue{T}"/> or a <see cref="ConcurrentBag{T}"/> returns a
/// collection of the same type with deep-cloned elements, instead of the read-only list the
/// <see cref="IEnumerable{T}"/> overload builds (ktsu-dev/DeepClone#128).
/// </summary>
[TestClass]
public class ConcurrentQueueAndBagCloneTests
{
	private static readonly int[] DequeueOrder = [1, 2, 3];
	private static readonly int[] BagIds = [1, 2, 3];

	/// <summary>
	/// Tests that the clone is a concurrent queue that dequeues in the same order as the source.
	/// </summary>
	[TestMethod]
	public void ConcurrentQueue_DeepClone_PreservesTypeAndDequeueOrder()
	{
		ConcurrentQueue<SimpleObject> original = new();
		original.Enqueue(new SimpleObject { Id = 1, Name = "First" });
		original.Enqueue(new SimpleObject { Id = 2, Name = "Second" });
		original.Enqueue(new SimpleObject { Id = 3, Name = "Third" });

		object result = original.DeepClone();

		Assert.IsInstanceOfType<ConcurrentQueue<SimpleObject>>(result);
		ConcurrentQueue<SimpleObject> clone = (ConcurrentQueue<SimpleObject>)result;
		List<int> dequeued = [];
		while (clone.TryDequeue(out SimpleObject? item))
		{
			dequeued.Add(item.Id);
		}

		Assert.AreSequenceEqual(DequeueOrder, dequeued);
		Assert.HasCount(3, original);
	}

	/// <summary>
	/// Tests that the queue clone's elements are deep clones rather than the source's instances.
	/// </summary>
	[TestMethod]
	public void ConcurrentQueue_DeepClone_ClonesElements()
	{
		ConcurrentQueue<SimpleObject> original = new();
		original.Enqueue(new SimpleObject { Id = 1, Name = "First" });
		original.Enqueue(new SimpleObject { Id = 2, Name = "Second" });

		object result = original.DeepClone();

		Assert.IsInstanceOfType<ConcurrentQueue<SimpleObject>>(result);
		SimpleObject[] sourceItems = [.. original];
		SimpleObject[] cloneItems = [.. (ConcurrentQueue<SimpleObject>)result];
		Assert.HasCount(sourceItems.Length, cloneItems);
		for (int i = 0; i < sourceItems.Length; i++)
		{
			Assert.AreNotSame(sourceItems[i], cloneItems[i]);
			Assert.AreEqual(sourceItems[i].Id, cloneItems[i].Id);
			Assert.AreEqual(sourceItems[i].Name, cloneItems[i].Name);
		}
	}

	/// <summary>
	/// Tests that an empty concurrent queue clones to an empty concurrent queue.
	/// </summary>
	[TestMethod]
	public void ConcurrentQueue_DeepClone_Empty()
	{
		ConcurrentQueue<SimpleObject> clone = new ConcurrentQueue<SimpleObject>().DeepClone();

		Assert.IsEmpty(clone);
	}

	/// <summary>
	/// Tests that a null concurrent queue throws rather than cloning to null.
	/// </summary>
	[TestMethod]
	public void ConcurrentQueue_DeepClone_NullSource_Throws()
	{
		ConcurrentQueue<SimpleObject> source = null!;

		Assert.ThrowsExactly<ArgumentNullException>(() => source.DeepClone());
	}

	/// <summary>
	/// Tests that the clone is a concurrent bag holding deep clones of the source's elements.
	/// </summary>
	[TestMethod]
	public void ConcurrentBag_DeepClone_PreservesTypeAndClonesElements()
	{
		ConcurrentBag<SimpleObject> original =
		[
			new SimpleObject { Id = 1, Name = "One" },
			new SimpleObject { Id = 2, Name = "Two" },
			new SimpleObject { Id = 3, Name = "Three" },
		];

		object result = original.DeepClone();

		Assert.IsInstanceOfType<ConcurrentBag<SimpleObject>>(result);
		SimpleObject[] cloneItems = [.. (ConcurrentBag<SimpleObject>)result];
		Assert.HasCount(3, cloneItems);
		int[] cloneIds = [.. cloneItems.Select(item => item.Id).Order()];
		Assert.AreSequenceEqual(BagIds, cloneIds);
		foreach (SimpleObject cloned in cloneItems)
		{
			SimpleObject source = original.Single(item => item.Id == cloned.Id);
			Assert.AreNotSame(source, cloned);
			Assert.AreEqual(source.Name, cloned.Name);
		}

		Assert.HasCount(3, original);
	}

	/// <summary>
	/// Tests that an empty concurrent bag clones to an empty concurrent bag.
	/// </summary>
	[TestMethod]
	public void ConcurrentBag_DeepClone_Empty()
	{
		ConcurrentBag<SimpleObject> clone = new ConcurrentBag<SimpleObject>().DeepClone();

		Assert.IsEmpty(clone);
	}

	/// <summary>
	/// Tests that a null concurrent bag throws rather than cloning to null.
	/// </summary>
	[TestMethod]
	public void ConcurrentBag_DeepClone_NullSource_Throws()
	{
		ConcurrentBag<SimpleObject> source = null!;

		Assert.ThrowsExactly<ArgumentNullException>(() => source.DeepClone());
	}
}
