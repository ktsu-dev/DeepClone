// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

/// <summary>
/// Tests that one clone operation clones each object once, so cycles end and shared references stay
/// shared, and that separate clone operations share nothing (ktsu-dev/DeepClone#89).
/// </summary>
[TestClass]
public sealed class CloneContextTests
{
	/// <summary>
	/// A node that clones its links naively, with no cycle handling of its own.
	/// </summary>
	private sealed class LinkedNode : DeepCloneable<LinkedNode>
	{
		public string? Name { get; set; }

		public LinkedNode? Next { get; set; }

		public LinkedNode? Parent { get; set; }

		public List<LinkedNode> Children { get; } = [];

		protected override LinkedNode CreateInstance() => new();

		protected override void DeepClone(LinkedNode clone)
		{
			base.DeepClone(clone);
			clone.Name = Name;
			clone.Next = Next?.DeepClone();
			clone.Parent = Parent?.DeepClone();
			clone.Children.DeepCloneFrom(Children);
		}
	}

	/// <summary>
	/// An object holding two references that may point at the same object.
	/// </summary>
	private sealed class Pair : DeepCloneable<Pair>
	{
		public LinkedNode? First { get; set; }

		public LinkedNode? Second { get; set; }

		protected override Pair CreateInstance() => new();

		protected override void DeepClone(Pair clone)
		{
			base.DeepClone(clone);
			clone.First = First?.DeepClone();
			clone.Second = Second?.DeepClone();
		}
	}

	/// <summary>
	/// A type whose instances are all equal, so only reference identity tells them apart.
	/// </summary>
	private sealed class AlwaysEqual : DeepCloneable<AlwaysEqual>
	{
		public int Value { get; set; }

		public override bool Equals(object? obj) => obj is AlwaysEqual;

		public override int GetHashCode() => 0;

		protected override AlwaysEqual CreateInstance() => new();

		protected override void DeepClone(AlwaysEqual clone)
		{
			base.DeepClone(clone);
			clone.Value = Value;
		}
	}

	/// <summary>
	/// A node whose clone throws once <see cref="Fail"/> is set.
	/// </summary>
	private sealed class ThrowingNode : DeepCloneable<ThrowingNode>
	{
		public bool Fail { get; set; }

		public ThrowingNode? Child { get; set; }

		protected override ThrowingNode CreateInstance() => new();

		protected override void DeepClone(ThrowingNode clone)
		{
			base.DeepClone(clone);
			clone.Child = Child?.DeepClone();
			if (Fail)
			{
				throw new InvalidOperationException("clone failed");
			}
		}
	}

	[TestMethod]
	public void TwoNodeCycle_DeepClone_EndsAtTheCloneBeingBuilt()
	{
		LinkedNode a = new() { Name = "a" };
		LinkedNode b = new() { Name = "b" };
		a.Next = b;
		b.Next = a;

		LinkedNode clone = a.DeepClone();

		Assert.AreNotSame(a, clone);
		Assert.AreNotSame(b, clone.Next);
		Assert.AreEqual("b", clone.Next!.Name);
		Assert.AreSame(clone, clone.Next.Next);
	}

	[TestMethod]
	public void SelfReference_DeepClone_PointsAtTheClone()
	{
		LinkedNode node = new() { Name = "self" };
		node.Next = node;

		LinkedNode clone = node.DeepClone();

		Assert.AreNotSame(node, clone);
		Assert.AreSame(clone, clone.Next);
	}

	[TestMethod]
	public void ParentChildCycle_DeepClone_ChildPointsAtClonedParent()
	{
		LinkedNode root = new() { Name = "root" };
		LinkedNode child = new() { Name = "child", Parent = root };
		root.Children.Add(child);

		LinkedNode clone = root.DeepClone();

		Assert.HasCount(1, clone.Children);
		Assert.AreNotSame(child, clone.Children[0]);
		Assert.AreSame(clone, clone.Children[0].Parent);
	}

	[TestMethod]
	public void ConsecutiveDeepClones_ShareNoObjects()
	{
		LinkedNode root = new() { Name = "root" };
		LinkedNode child = new() { Name = "child", Parent = root };
		root.Children.Add(child);

		LinkedNode first = root.DeepClone();
		root.Name = "root2";
		child.Name = "child2";
		LinkedNode second = root.DeepClone();

		Assert.AreNotSame(first, second);
		Assert.AreNotSame(first.Children[0], second.Children[0]);
		Assert.AreEqual("root2", second.Name);
		Assert.AreEqual("child2", second.Children[0].Name);
		Assert.AreSame(second, second.Children[0].Parent);
		Assert.AreEqual("child", first.Children[0].Name);
		Assert.AreSame(first, first.Children[0].Parent);
	}

	[TestMethod]
	public void SharedReference_InOneObject_IsClonedOnce()
	{
		LinkedNode shared = new() { Name = "shared" };
		Pair pair = new() { First = shared, Second = shared };

		Pair clone = pair.DeepClone();

		Assert.AreNotSame(shared, clone.First);
		Assert.AreSame(clone.First, clone.Second);
	}

	[TestMethod]
	public void ListHoldingOneObjectTwice_DeepClone_YieldsOneCloneTwice()
	{
		LinkedNode x = new() { Name = "x" };
		List<LinkedNode> list = [x, x];

		List<LinkedNode> clone = list.DeepClone();

		Assert.HasCount(2, clone);
		Assert.AreNotSame(x, clone[0]);
		Assert.AreSame(clone[0], clone[1]);
	}

	[TestMethod]
	public void EnumerableHoldingOneObjectTwice_DeepClone_YieldsOneCloneTwice()
	{
		LinkedNode x = new() { Name = "x" };
		IEnumerable<LinkedNode> source = [x, x];

		LinkedNode[] clone = [.. source.DeepClone()];

		Assert.AreNotSame(x, clone[0]);
		Assert.AreSame(clone[0], clone[1]);
	}

	[TestMethod]
	public void DeepCloneFrom_HoldingOneObjectTwice_YieldsOneCloneTwice()
	{
		LinkedNode x = new() { Name = "x" };
		List<LinkedNode> dest = [];

		dest.DeepCloneFrom([x, x]);

		Assert.HasCount(2, dest);
		Assert.AreNotSame(x, dest[0]);
		Assert.AreSame(dest[0], dest[1]);
	}

	[TestMethod]
	public void DictionaryHoldingOneValueTwice_DeepClone_YieldsOneCloneTwice()
	{
		LinkedNode x = new() { Name = "x" };
		Dictionary<string, LinkedNode> dictionary = new() { ["a"] = x, ["b"] = x };

		Dictionary<string, LinkedNode> clone = dictionary.DeepClone();

		Assert.AreNotSame(x, clone["a"]);
		Assert.AreSame(clone["a"], clone["b"]);
	}

	[TestMethod]
	public void ConsecutiveCollectionClones_ShareNoObjects()
	{
		LinkedNode x = new() { Name = "x" };
		List<LinkedNode> list = [x];

		List<LinkedNode> first = list.DeepClone();
		List<LinkedNode> second = list.DeepClone();

		Assert.AreNotSame(first[0], second[0]);
	}

	[TestMethod]
	public void EqualButDistinctSources_AreClonedSeparately()
	{
		AlwaysEqual one = new() { Value = 1 };
		AlwaysEqual two = new() { Value = 2 };
		List<AlwaysEqual> list = [one, two];

		List<AlwaysEqual> clone = list.DeepClone();

		Assert.AreNotSame(clone[0], clone[1]);
		Assert.AreEqual(1, clone[0].Value);
		Assert.AreEqual(2, clone[1].Value);
	}

	[TestMethod]
	public void FailedClone_LeavesNothingForTheNextClone()
	{
		ThrowingNode child = new();
		ThrowingNode root = new() { Child = child, Fail = true };

		Assert.ThrowsExactly<InvalidOperationException>(root.DeepClone);

		ThrowingNode firstChildClone = child.DeepClone();
		ThrowingNode secondChildClone = child.DeepClone();

		Assert.AreNotSame(firstChildClone, secondChildClone);
	}
}
