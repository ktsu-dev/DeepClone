// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.Concurrent;

/// <summary>
/// Tests for the in-place <c>DeepCloneFrom</c> overloads on stacks and queues (ktsu-dev/DeepClone#114).
/// </summary>
[TestClass]
public class InPlaceStackQueueTests
{
	private static readonly int[] TopFirst = [3, 2, 1];
	private static readonly int[] FrontFirst = [1, 2, 3];
	private static readonly int[] TwoThenOne = [2, 1];

	private static SimpleObject[] MakeItems() =>
	[
		new() { Id = 1, Name = "One" },
		new() { Id = 2, Name = "Two" },
		new() { Id = 3, Name = "Three" },
	];

	private static void AssertDistinctClones(SimpleObject[] expected, SimpleObject[] actual)
	{
		Assert.HasCount(expected.Length, actual);
		for (int i = 0; i < expected.Length; i++)
		{
			Assert.AreEqual(expected[i].Id, actual[i].Id, $"order differs at {i}");
			Assert.AreNotSame(expected[i], actual[i], $"item {i} is shared with the source");
		}
	}

	/// <summary>
	/// A stack cloned in place pops in the same order as its source.
	/// </summary>
	[TestMethod]
	public void Stack_DeepCloneFrom_PreservesPopOrder()
	{
		Stack<SimpleObject> source = new(MakeItems());
		Stack<SimpleObject> dest = new([new SimpleObject { Id = 99 }]);

		dest.DeepCloneFrom(source);

		AssertDistinctClones([.. source], [.. dest]);
		Assert.AreEqual(3, dest.Pop().Id);
	}

	/// <summary>
	/// A queue cloned in place dequeues in the same order as its source.
	/// </summary>
	[TestMethod]
	public void Queue_DeepCloneFrom_PreservesDequeueOrder()
	{
		Queue<SimpleObject> source = new(MakeItems());
		Queue<SimpleObject> dest = new([new SimpleObject { Id = 99 }]);

		dest.DeepCloneFrom(source);

		AssertDistinctClones([.. source], [.. dest]);
		Assert.AreEqual(1, dest.Dequeue().Id);
	}

	/// <summary>
	/// A concurrent stack cloned in place pops in the same order as its source.
	/// </summary>
	[TestMethod]
	public void ConcurrentStack_DeepCloneFrom_PreservesPopOrder()
	{
		ConcurrentStack<SimpleObject> source = new(MakeItems());
		ConcurrentStack<SimpleObject> dest = new([new SimpleObject { Id = 99 }]);

		dest.DeepCloneFrom(source);

		AssertDistinctClones([.. source], [.. dest]);
		Assert.IsTrue(dest.TryPop(out SimpleObject? top));
		Assert.AreEqual(3, top!.Id);
	}

	/// <summary>
	/// A concurrent queue cloned in place dequeues in the same order as its source.
	/// </summary>
	[TestMethod]
	public void ConcurrentQueue_DeepCloneFrom_PreservesDequeueOrder()
	{
		ConcurrentQueue<SimpleObject> source = new(MakeItems());
		ConcurrentQueue<SimpleObject> dest = new([new SimpleObject { Id = 99 }]);

		dest.DeepCloneFrom(source);

		AssertDistinctClones([.. source], [.. dest]);
		Assert.IsTrue(dest.TryDequeue(out SimpleObject? front));
		Assert.AreEqual(1, front!.Id);
	}

	/// <summary>
	/// Cloning a stack or queue from itself leaves its contents and order unchanged.
	/// </summary>
	[TestMethod]
	public void DeepCloneFrom_Self_LeavesContentsUnchanged()
	{
		Stack<SimpleObject> stack = new(MakeItems());
		Queue<SimpleObject> queue = new(MakeItems());
		ConcurrentStack<SimpleObject> concurrentStack = new(MakeItems());
		ConcurrentQueue<SimpleObject> concurrentQueue = new(MakeItems());

		stack.DeepCloneFrom(stack);
		queue.DeepCloneFrom(queue);
		concurrentStack.DeepCloneFrom(concurrentStack);
		concurrentQueue.DeepCloneFrom(concurrentQueue);

		CollectionAssert.AreEqual(TopFirst, stack.Select(i => i.Id).ToArray());
		CollectionAssert.AreEqual(FrontFirst, queue.Select(i => i.Id).ToArray());
		CollectionAssert.AreEqual(TopFirst, concurrentStack.Select(i => i.Id).ToArray());
		CollectionAssert.AreEqual(FrontFirst, concurrentQueue.Select(i => i.Id).ToArray());
	}

	/// <summary>
	/// The pattern the README teaches works for get-only stack and queue properties.
	/// </summary>
	[TestMethod]
	public void GetOnlyProperties_DeepCloneFrom_Compiles()
	{
		History original = new();
		original.Undo.Push(new SimpleObject { Id = 1 });
		original.Undo.Push(new SimpleObject { Id = 2 });
		original.Pending.Enqueue(new SimpleObject { Id = 7 });

		History clone = original.DeepClone();

		CollectionAssert.AreEqual(TwoThenOne, clone.Undo.Select(i => i.Id).ToArray());
		Assert.AreNotSame(original.Undo.Peek(), clone.Undo.Peek());
		Assert.IsTrue(clone.Pending.TryPeek(out SimpleObject? pending));
		Assert.AreEqual(7, pending!.Id);
	}

	private sealed class History : DeepCloneable<History>
	{
		public Stack<SimpleObject> Undo { get; } = new();

		public ConcurrentQueue<SimpleObject> Pending { get; } = new();

		protected override History CreateInstance() => new();

		protected override void DeepClone(History clone)
		{
			base.DeepClone(clone);
			clone.Undo.DeepCloneFrom(Undo);
			clone.Pending.DeepCloneFrom(Pending);
		}
	}
}
