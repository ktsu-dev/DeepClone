// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections;

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
