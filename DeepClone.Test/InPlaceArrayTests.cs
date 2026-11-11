// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.ObjectModel;

/// <summary>
/// Tests for the in-place <c>DeepCloneFrom</c> overload on arrays (ktsu-dev/DeepClone#137).
/// </summary>
[TestClass]
public class InPlaceArrayTests
{
	private static readonly int[] FourFiveSix = [4, 5, 6];

	private static SimpleObject[] MakeItems() =>
	[
		new() { Id = 1, Name = "One" },
		new() { Id = 2, Name = "Two" },
		new() { Id = 3, Name = "Three" },
	];

	/// <summary>
	/// An equal-length array is filled with distinct clones, in order.
	/// </summary>
	[TestMethod]
	public void Array_DeepCloneFrom_FillsWithClonesInOrder()
	{
		SimpleObject[] source = MakeItems();
		SimpleObject[] dest = new SimpleObject[3];

		dest.DeepCloneFrom(source);

		for (int i = 0; i < source.Length; i++)
		{
			Assert.AreEqual(source[i].Id, dest[i].Id, $"order differs at {i}");
			Assert.AreEqual(source[i].Name, dest[i].Name);
			Assert.AreNotSame(source[i], dest[i], $"item {i} is shared with the source");
		}
	}

	/// <summary>
	/// A source of a different length is refused before the destination is touched.
	/// </summary>
	[TestMethod]
	public void Array_DeepCloneFrom_LengthMismatch_ThrowsAndLeavesDestinationUnchanged()
	{
		SimpleObject keep = new() { Id = 99 };
		SimpleObject[] dest = [keep, keep];

		ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(MakeItems()));

		Assert.AreEqual("source", ex.ParamName);
		Assert.AreSame(keep, dest[0]);
		Assert.AreSame(keep, dest[1]);
	}

	/// <summary>
	/// Cloning an array into itself keeps its values, as clones.
	/// </summary>
	[TestMethod]
	public void Array_DeepCloneFrom_Self_KeepsValues()
	{
		SimpleObject[] array = MakeItems();
		SimpleObject[] originals = [.. array];

		array.DeepCloneFrom(array);

		for (int i = 0; i < array.Length; i++)
		{
			Assert.AreEqual(originals[i].Id, array[i].Id);
			Assert.AreNotSame(originals[i], array[i]);
		}
	}

	/// <summary>
	/// Null elements are copied as null, and non-cloneable elements as they are.
	/// </summary>
	[TestMethod]
	public void Array_DeepCloneFrom_CopiesNullsAndValues()
	{
		SimpleObject?[] dest = new SimpleObject?[2];
		dest.DeepCloneFrom([null, new SimpleObject { Id = 7 }]);
		Assert.IsNull(dest[0]);
		Assert.AreEqual(7, dest[1]?.Id);

		int[] numbers = new int[3];
		numbers.DeepCloneFrom(FourFiveSix);
		Assert.AreSequenceEqual(FourFiveSix, numbers);
	}

	/// <summary>
	/// A get-only array property clones through <c>DeepClone(TDerived)</c>.
	/// </summary>
	[TestMethod]
	public void GetOnlyArrayProperty_ClonesThroughDeepClone()
	{
		SlotHolder holder = new();
		holder.Slots[0] = new SimpleObject { Id = 5, Name = "Five" };

		SlotHolder clone = holder.DeepClone();

		Assert.HasCount(2, clone.Slots);
		Assert.AreEqual(5, clone.Slots[0]?.Id);
		Assert.AreNotSame(holder.Slots[0], clone.Slots[0]);
		Assert.IsNull(clone.Slots[1]);
	}

	/// <summary>
	/// A read-only collection is refused with an argument error naming the destination.
	/// </summary>
	[TestMethod]
	public void ReadOnlyCollection_DeepCloneFrom_ThrowsArgumentException()
	{
		ICollection<SimpleObject> dest = new ReadOnlyCollection<SimpleObject>(MakeItems());

		ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(MakeItems()));

		Assert.AreEqual("dest", ex.ParamName);
	}

	/// <summary>
	/// A holder whose array property has no setter.
	/// </summary>
	private sealed class SlotHolder : DeepCloneable<SlotHolder>
	{
		/// <summary>
		/// Gets the fixed-size slots.
		/// </summary>
		public SimpleObject?[] Slots { get; } = new SimpleObject?[2];

		/// <inheritdoc/>
		protected override SlotHolder CreateInstance() => new();

		/// <inheritdoc/>
		protected override void DeepClone(SlotHolder clone)
		{
			base.DeepClone(clone);
			clone.Slots.DeepCloneFrom(Slots);
		}
	}
}
