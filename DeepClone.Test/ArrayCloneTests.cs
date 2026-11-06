// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

/// <summary>
/// Tests that deep cloning an array returns an array (ktsu-dev/DeepClone#130).
/// </summary>
[TestClass]
public class ArrayCloneTests
{
	/// <summary>
	/// The clone of an array is assignable to an array of the same type, with the same length and order.
	/// </summary>
	[TestMethod]
	public void Array_DeepClone_ReturnsAnArrayOfTheSameLength()
	{
		// Arrange
		SimpleObject[] original = [new() { Id = 1, Name = "Item1" }, new() { Id = 2, Name = "Item2" }];

		// Act
		SimpleObject[] clone = original.DeepClone();

		// Assert
		Assert.AreNotSame(original, clone);
		Assert.AreEqual(typeof(SimpleObject[]), clone.GetType());
		Assert.HasCount(2, clone);
		Assert.AreEqual(1, clone[0].Id);
		Assert.AreEqual("Item2", clone[1].Name);
		Assert.AreNotSame(original[0], clone[0]);
		Assert.AreNotSame(original[1], clone[1]);
	}

	/// <summary>
	/// Replacing an element of the clone, or editing one of its elements, leaves the original alone.
	/// </summary>
	[TestMethod]
	public void Array_DeepClone_IsIndependentOfTheOriginal()
	{
		// Arrange
		SimpleObject[] original = [new() { Id = 1, Name = "Item1" }, new() { Id = 2, Name = "Item2" }];
		SimpleObject[] clone = original.DeepClone();

		// Act
		clone[0].Name = "Edited";
		clone[1] = new() { Id = 3, Name = "Replaced" };

		// Assert
		Assert.AreEqual("Item1", original[0].Name);
		Assert.AreEqual(2, original[1].Id);
		Assert.AreEqual("Item2", original[1].Name);
	}

	/// <summary>
	/// An empty array clones to a new empty array, and a value-type array copies its values.
	/// </summary>
	[TestMethod]
	public void Array_DeepClone_HandlesEmptyAndValueTypeArrays()
	{
		SimpleObject[] empty = [];
		int[] numbers = [1, 2, 3];

		SimpleObject[] emptyClone = empty.DeepClone();
		int[] numbersClone = numbers.DeepClone();
		numbersClone[0] = 42;

		Assert.IsEmpty(emptyClone);
		Assert.AreNotSame(empty, emptyClone);
		Assert.HasCount(3, numbersClone);
		Assert.AreEqual(42, numbersClone[0]);
		Assert.AreEqual(3, numbersClone[2]);
		Assert.AreEqual(1, numbers[0]);
	}

	/// <summary>
	/// A null array is rejected.
	/// </summary>
	[TestMethod]
	public void Array_DeepClone_WithNull_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => ((SimpleObject[])null!).DeepClone());

	/// <summary>
	/// An array held as an IEnumerable still binds to the IEnumerable overload, so existing callers keep compiling.
	/// </summary>
	[TestMethod]
	public void Array_HeldAsEnumerable_StillClones()
	{
		SimpleObject[] items = [new() { Id = 1, Name = "Item1" }];
		IEnumerable<SimpleObject> original = items;

		SimpleObject only = original.DeepClone().Single();

		Assert.AreEqual(1, only.Id);
		Assert.AreNotSame(items[0], only);
	}
}
