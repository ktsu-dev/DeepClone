// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

/// <summary>
/// Tests for deep cloning a collection type that implements <see cref="IDeepCloneable"/> itself.
/// </summary>
[TestClass]
public class CloneableCollectionTests
{
	/// <summary>
	/// Tests that a collection implementing IDeepCloneable explicitly is cloned by its own implementation,
	/// not element-wise into a read-only list, when DeepClone() binds to the IEnumerable extension.
	/// </summary>
	[TestMethod]
	public void Enumerable_DeepClone_OfIDeepCloneableCollection_ShouldUseItsOwnClone()
	{
		// Arrange
		Bag original = new() { Label = "Tools" };
		original.Add(new SimpleObject { Id = 1, Name = "Hammer" });

		// Act
		IEnumerable<SimpleObject> clone = original.DeepClone();

		// Assert
		Bag bag = Assert.IsInstanceOfType<Bag>(clone);
		Assert.AreNotSame(original, bag);
		Assert.AreEqual("Tools", bag.Label);
		Assert.AreEqual(1, bag.Count);
		SimpleObject item = bag.First();
		Assert.AreNotSame(original.First(), item);
		Assert.AreEqual("Hammer", item.Name);
	}

	/// <summary>
	/// Tests that a List subclass implementing IDeepCloneable explicitly keeps its type and state when
	/// DeepClone() binds to the List overload (ktsu-dev/DeepClone#148).
	/// </summary>
	[TestMethod]
	public void List_DeepClone_OfIDeepCloneableSubclass_ShouldUseItsOwnClone()
	{
		// Arrange
		TaggedList original = new() { Label = "x" };
		original.Add(new SimpleObject { Id = 1, Name = "One" });

		// Act
		List<SimpleObject> clone = original.DeepClone();

		// Assert
		TaggedList tagged = Assert.IsInstanceOfType<TaggedList>(clone);
		Assert.AreNotSame(original, tagged);
		Assert.AreEqual("x", tagged.Label);
		Assert.HasCount(1, tagged);
		Assert.AreNotSame(original[0], tagged[0]);
		Assert.AreEqual("One", tagged[0].Name);
	}

	/// <summary>
	/// Tests that the same List subclass cloned through the IEnumerable overload keeps its type and state too.
	/// </summary>
	[TestMethod]
	public void Enumerable_DeepClone_OfIDeepCloneableListSubclass_ShouldUseItsOwnClone()
	{
		// Arrange
		TaggedList original = new() { Label = "x" };
		original.Add(new SimpleObject { Id = 1 });
		IEnumerable<SimpleObject> source = original;

		// Act
		IEnumerable<SimpleObject> clone = source.DeepClone();

		// Assert
		Assert.AreEqual("x", Assert.IsInstanceOfType<TaggedList>(clone).Label);
	}

	/// <summary>
	/// Tests that a HashSet subclass implementing IDeepCloneable explicitly is cloned by its own implementation.
	/// </summary>
	[TestMethod]
	public void HashSet_DeepClone_OfIDeepCloneableSubclass_ShouldUseItsOwnClone()
	{
		// Arrange
		TaggedSet original = new() { Label = "x" };
		original.Add(new SimpleObject { Id = 1 });

		// Act
		HashSet<SimpleObject> clone = original.DeepClone();

		// Assert
		TaggedSet tagged = Assert.IsInstanceOfType<TaggedSet>(clone);
		Assert.AreNotSame(original, tagged);
		Assert.AreEqual("x", tagged.Label);
		Assert.HasCount(1, tagged);
	}

	/// <summary>
	/// Tests that a Dictionary subclass implementing IDeepCloneable explicitly is cloned by its own implementation.
	/// </summary>
	[TestMethod]
	public void Dictionary_DeepClone_OfIDeepCloneableSubclass_ShouldUseItsOwnClone()
	{
		// Arrange
		TaggedDictionary original = new() { Label = "x" };
		original.Add("a", new SimpleObject { Id = 1 });

		// Act
		Dictionary<string, SimpleObject> clone = original.DeepClone();

		// Assert
		TaggedDictionary tagged = Assert.IsInstanceOfType<TaggedDictionary>(clone);
		Assert.AreNotSame(original, tagged);
		Assert.AreEqual("x", tagged.Label);
		Assert.AreNotSame(original["a"], tagged["a"]);
		Assert.AreEqual(1, tagged["a"].Id);
	}

	/// <summary>
	/// Tests that a Queue subclass implementing IDeepCloneable explicitly is cloned by its own implementation.
	/// </summary>
	[TestMethod]
	public void Queue_DeepClone_OfIDeepCloneableSubclass_ShouldUseItsOwnClone()
	{
		// Arrange
		TaggedQueue original = new() { Label = "x" };
		original.Enqueue(new SimpleObject { Id = 1 });

		// Act
		Queue<SimpleObject> clone = original.DeepClone();

		// Assert
		TaggedQueue tagged = Assert.IsInstanceOfType<TaggedQueue>(clone);
		Assert.AreNotSame(original, tagged);
		Assert.AreEqual("x", tagged.Label);
		Assert.HasCount(1, tagged);
	}

	/// <summary>
	/// Tests that a collection cloned by its own implementation clones one item it holds twice to one shared
	/// clone, as an element-wise clone does (ktsu-dev/DeepClone#132, #148).
	/// </summary>
	[TestMethod]
	public void Enumerable_DeepClone_OfIDeepCloneableCollectionHoldingOneObjectTwice_ShouldShareOneClone()
	{
		// Arrange
		SimpleObject shared = new() { Id = 1 };
		Bag original = [shared, shared];

		// Act
		SimpleObject[] clone = [.. original.DeepClone()];

		// Assert
		Assert.HasCount(2, clone);
		Assert.AreNotSame(shared, clone[0]);
		Assert.AreSame(clone[0], clone[1]);
	}

	/// <summary>
	/// Tests that a subclass whose own IDeepCloneable.DeepClone() calls the base collection's DeepClone() on itself
	/// gets the element-wise clone from that call, rather than recursing into its own implementation forever.
	/// </summary>
	[TestMethod]
	public void List_DeepClone_OfSubclassWhoseCloneCallsTheBaseOverload_ShouldNotRecurse()
	{
		// Arrange
		SelfCloningList original = new() { Label = "x" };
		original.Add(new SimpleObject { Id = 1 });

		// Act
		List<SimpleObject> clone = original.DeepClone();

		// Assert
		SelfCloningList tagged = Assert.IsInstanceOfType<SelfCloningList>(clone);
		Assert.AreEqual("x", tagged.Label);
		Assert.HasCount(1, tagged);
		Assert.AreNotSame(original[0], tagged[0]);
	}

	/// <summary>
	/// Tests that every overload whose collection type can be subclassed defers to a subclass's own
	/// IDeepCloneable.DeepClone(), including when the call binds to a dictionary interface (ktsu-dev/DeepClone#148).
	/// </summary>
	[TestMethod]
	public void EveryDerivableOverload_DeepClone_OfIDeepCloneableSubclass_ShouldUseItsOwnClone()
	{
		IDictionary<string, SimpleObject> dictionary = new TaggedDictionary();
		IReadOnlyDictionary<string, SimpleObject> readOnlyDictionary = new TaggedDictionary();

		Assert.IsInstanceOfType<TaggedDictionary>(dictionary.DeepClone());
		Assert.IsInstanceOfType<TaggedDictionary>(readOnlyDictionary.DeepClone());
		AssertOwnClone(new MarkedSortedDictionary().DeepClone());
		AssertOwnClone(new MarkedSortedList().DeepClone());
		AssertOwnClone(new MarkedOrderedDictionary().DeepClone());
		AssertOwnClone(new MarkedConcurrentDictionary().DeepClone());
		AssertOwnClone(new MarkedReadOnlyDictionary().DeepClone());
		AssertOwnClone(new MarkedLinkedList().DeepClone());
		AssertOwnClone(new MarkedSortedSet().DeepClone());
		AssertOwnClone(new MarkedStack().DeepClone());
		AssertOwnClone(new MarkedConcurrentStack().DeepClone());
		AssertOwnClone(new MarkedConcurrentQueue().DeepClone());
		AssertOwnClone(new MarkedConcurrentBag().DeepClone());
	}

	private static void AssertOwnClone(object clone) =>
		Assert.IsTrue(clone is IOwnCloneMarker { MadeByOwnClone: true }, $"{clone.GetType().Name} was not cloned by its own implementation.");

	/// <summary>
	/// Marks a clone made by a test collection's own IDeepCloneable.DeepClone().
	/// </summary>
	private interface IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; }
	}

	private sealed class MarkedSortedDictionary : SortedDictionary<string, SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedSortedDictionary { MadeByOwnClone = true };
	}

	private sealed class MarkedSortedList : SortedList<string, SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedSortedList { MadeByOwnClone = true };
	}

	private sealed class MarkedOrderedDictionary : OrderedDictionary<string, SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedOrderedDictionary { MadeByOwnClone = true };
	}

	private sealed class MarkedConcurrentDictionary : ConcurrentDictionary<string, SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedConcurrentDictionary { MadeByOwnClone = true };
	}

	private sealed class MarkedReadOnlyDictionary() : ReadOnlyDictionary<string, SimpleObject>(new Dictionary<string, SimpleObject>()), IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedReadOnlyDictionary { MadeByOwnClone = true };
	}

	private sealed class MarkedLinkedList : LinkedList<SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedLinkedList { MadeByOwnClone = true };
	}

	private sealed class MarkedSortedSet : SortedSet<SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedSortedSet { MadeByOwnClone = true };
	}

	private sealed class MarkedStack : Stack<SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedStack { MadeByOwnClone = true };
	}

	private sealed class MarkedConcurrentStack : ConcurrentStack<SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedConcurrentStack { MadeByOwnClone = true };
	}

	private sealed class MarkedConcurrentQueue : ConcurrentQueue<SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedConcurrentQueue { MadeByOwnClone = true };
	}

	private sealed class MarkedConcurrentBag : ConcurrentBag<SimpleObject>, IDeepCloneable, IOwnCloneMarker
	{
		public bool MadeByOwnClone { get; private init; }

		object IDeepCloneable.DeepClone() => new MarkedConcurrentBag { MadeByOwnClone = true };
	}

	private sealed class TaggedList : List<SimpleObject>, IDeepCloneable
	{
		public string Label { get; set; } = string.Empty;

		object IDeepCloneable.DeepClone()
		{
			TaggedList clone = new() { Label = Label };
			foreach (SimpleObject item in this)
			{
				clone.Add(item.DeepClone());
			}

			return clone;
		}
	}

	private sealed class TaggedSet : HashSet<SimpleObject>, IDeepCloneable
	{
		public string Label { get; set; } = string.Empty;

		object IDeepCloneable.DeepClone()
		{
			TaggedSet clone = new() { Label = Label };
			foreach (SimpleObject item in this)
			{
				clone.Add(item.DeepClone());
			}

			return clone;
		}
	}

	private sealed class TaggedDictionary : Dictionary<string, SimpleObject>, IDeepCloneable
	{
		public string Label { get; set; } = string.Empty;

		object IDeepCloneable.DeepClone()
		{
			TaggedDictionary clone = new() { Label = Label };
			foreach (KeyValuePair<string, SimpleObject> pair in this)
			{
				clone.Add(pair.Key, pair.Value.DeepClone());
			}

			return clone;
		}
	}

	private sealed class TaggedQueue : Queue<SimpleObject>, IDeepCloneable
	{
		public string Label { get; set; } = string.Empty;

		object IDeepCloneable.DeepClone()
		{
			TaggedQueue clone = new() { Label = Label };
			foreach (SimpleObject item in this)
			{
				clone.Enqueue(item.DeepClone());
			}

			return clone;
		}
	}

	private sealed class SelfCloningList : List<SimpleObject>, IDeepCloneable
	{
		public string Label { get; set; } = string.Empty;

		object IDeepCloneable.DeepClone()
		{
			SelfCloningList clone = new() { Label = Label };
			clone.AddRange(DeepCloneContainerExtensions.DeepClone<SimpleObject>(this));
			return clone;
		}
	}

	private sealed class Bag : IDeepCloneable, IEnumerable<SimpleObject>
	{
		private readonly List<SimpleObject> items = [];

		public string Label { get; set; } = string.Empty;

		public int Count => items.Count;

		public void Add(SimpleObject item) => items.Add(item);

		object IDeepCloneable.DeepClone()
		{
			Bag clone = new() { Label = Label };
			foreach (SimpleObject item in items)
			{
				clone.Add(item.DeepClone());
			}

			return clone;
		}

		public IEnumerator<SimpleObject> GetEnumerator() => items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
