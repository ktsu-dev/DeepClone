// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test;

using System.Collections.Concurrent;

/// <summary>
/// Tests that verify the thread safety of deep cloning operations.
/// </summary>
/// <remarks>
/// When implementing DeepClone in classes with inheritance hierarchies, it's critical to:
/// <list type="bullet">
/// <item>
/// <description>Always call base.DeepClone(clone) first in derived classes to properly clone base class properties</description>
/// </item>
/// <item>
/// <description>Maintain thread safety by avoiding shared mutable state during the cloning process</description>
/// </item>
/// <item>
/// <description>Ensure the entire inheritance chain properly implements DeepClone with base.DeepClone calls</description>
/// </item>
/// </list>
///
/// For example, in a class hierarchy like Animal → Mammal → Dog:
/// <code>
/// // In Dog class
/// protected override void DeepClone(Dog clone)
/// {
///     // Always call base first to handle Mammal properties
///     base.DeepClone(clone);
///
///     // Then clone Dog-specific properties
///     clone.Breed = Breed;
///     clone.IsTrained = IsTrained;
/// }
///
/// // In Mammal class
/// protected override void DeepClone(Mammal clone)
/// {
///     // Always call base first to handle Animal properties
///     base.DeepClone(clone);
///
///     // Then clone Mammal-specific properties
///     clone.FurColor = FurColor;
///     clone.IsWarmBlooded = IsWarmBlooded;
/// }
/// </code>
///
/// This chaining of base.DeepClone calls ensures that all properties throughout the inheritance
/// hierarchy are properly cloned, even in multi-threaded scenarios.
/// </remarks>
[TestClass]
public class ThreadSafetyTests
{
	/// <summary>
	/// Tests that concurrent deep cloning operations work correctly.
	/// </summary>
	[TestMethod]
	public void ConcurrentOperations_DeepClone_ShouldWorkCorrectly()
	{
		// Arrange - create a complex object
		ComplexObject original = new()
		{
			Id = 1,
			Name = "Parent",
			Child = new SimpleObject
			{
				Id = 2,
				Name = "Child"
			}
		};

		// Create collection to hold results
		ConcurrentBag<ComplexObject> results = [];

		// Act - clone the object concurrently from multiple threads
		Parallel.For(0, 100, _ =>
		{
			ComplexObject clone = original.DeepClone();
			results.Add(clone);
		});

		// Assert - verify all clones are independent copies
		Assert.HasCount(100, results);

		foreach (ComplexObject clone in results)
		{
			// Each clone should have the same values
			Assert.AreEqual(1, clone.Id);
			Assert.AreEqual("Parent", clone.Name);
			Assert.IsNotNull(clone.Child);
			Assert.AreEqual(2, clone.Child!.Id);
			Assert.AreEqual("Child", clone.Child.Name);

			// Modifying each clone shouldn't affect the original
			clone.Id = 999;
			clone.Name = "Modified";
			clone.Child!.Name = "ModifiedChild";
		}

		// Verify original remains unchanged
		Assert.AreEqual(1, original.Id);
		Assert.AreEqual("Parent", original.Name);
		Assert.IsNotNull(original.Child);
		Assert.AreEqual(2, original.Child!.Id);
		Assert.AreEqual("Child", original.Child.Name);
	}

	/// <summary>
	/// Tests that concurrent access to a cloned collection works correctly.
	/// </summary>
	[TestMethod]
	public void ConcurrentCollection_DeepClone_ShouldWorkCorrectly()
	{
		// Arrange
		ConcurrentDictionary<int, SimpleObject> original = new();

		// Add some items
		for (int i = 0; i < 10; i++)
		{
			original.TryAdd(i, new SimpleObject { Id = i, Name = $"Item{i}" });
		}

		// Act
		ConcurrentDictionary<int, SimpleObject> clone = original.ToDictionary(
			pair => pair.Key,
			pair => pair.Value.DeepClone())
			.ToConcurrentDictionary();

		// Assert
		Assert.HasCount(10, clone);

		// Check all items were cloned correctly
		for (int i = 0; i < 10; i++)
		{
			Assert.IsTrue(clone.ContainsKey(i), $"Clone should contain key {i}");
			Assert.AreEqual(i, clone[i].Id);
			Assert.AreEqual($"Item{i}", clone[i].Name);
		}

		// Verify independence
		Parallel.For(0, 10, i =>
		{
			if (clone.TryGetValue(i, out SimpleObject? item))
			{
				item.Name = $"Modified{i}";
			}
		});

		// Original should be unchanged
		for (int i = 0; i < 10; i++)
		{
			Assert.AreEqual($"Item{i}", original[i].Name);
			Assert.AreEqual($"Modified{i}", clone[i].Name);
		}
	}
}

/// <summary>
/// Extension methods for testing concurrent collections.
/// </summary>
public static class ConcurrentCollectionExtensions
{
	/// <summary>
	/// Converts a dictionary to a concurrent dictionary.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dictionary">The source dictionary.</param>
	/// <returns>A new concurrent dictionary with the same contents.</returns>
	public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(
		this IDictionary<TKey, TValue> dictionary) where TKey : notnull
	{
		return new ConcurrentDictionary<TKey, TValue>(dictionary);
	}
}
