// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test;

using System.Collections.ObjectModel;
using ktsu.DeepClone.Test.Mocks;
using ktsu.DeepClone.Test.Mocks.Shapes;
using ktsu.Extensions;

/// <summary>
/// Contains tests for deep cloning functionality.
/// </summary>
[TestClass]
public class DeepCloneTests
{
	/// <summary>
	/// Tests that a simple object can be deep cloned and modifications to the clone don't affect the original.
	/// </summary>
	[TestMethod]
	public void SimpleObject_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		var original = new SimpleObject
		{
			Id = 1,
			Name = "Test"
		};

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(original.Id, clone.Id);
		Assert.AreEqual(original.Name, clone.Name);

		// Modify the clone
		clone.Id = 2;
		clone.Name = "Modified";

		// Original should remain unchanged
		Assert.AreEqual(1, original.Id);
		Assert.AreEqual("Test", original.Name);
	}

	/// <summary>
	/// Tests that a complex object with nested objects can be deep cloned and modifications 
	/// to nested objects in the clone don't affect the original.
	/// </summary>
	[TestMethod]
	public void ComplexObject_DeepClone_ShouldCreateIndependentCopyWithNestedObjects()
	{
		// Arrange
		var original = new ComplexObject
		{
			Id = 1,
			Name = "Parent",
			Child = new SimpleObject
			{
				Id = 2,
				Name = "Child"
			},
			Items =
			{
				new SimpleObject { Id = 3, Name = "Item1" },
				new SimpleObject { Id = 4, Name = "Item2" }
			}
		};

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(original.Id, clone.Id);
		Assert.AreEqual(original.Name, clone.Name);
		Assert.IsNotNull(original.Child);
		Assert.IsNotNull(clone.Child);
		Assert.AreEqual(original.Child!.Id, clone.Child!.Id);
		Assert.AreEqual(original.Child.Name, clone.Child.Name);
		Assert.AreEqual(original.Items.Count, clone.Items.Count);
		Assert.AreEqual(original.Items[0].Id, clone.Items[0].Id);
		Assert.AreEqual(original.Items[1].Name, clone.Items[1].Name);

		// Modify clone's nested objects
		if (clone.Child != null)
		{
			clone.Child.Id = 99;
		}

		clone.Items[0].Name = "Modified";

		// Original's nested objects should remain unchanged
		Assert.AreEqual(2, original.Child!.Id);
		Assert.AreEqual("Item1", original.Items[0].Name);
	}

	/// <summary>
	/// Tests that a polymorphic hierarchy can be properly deep cloned with the correct derived types.
	/// </summary>
	[TestMethod]
	public void PolymorphicObject_DeepClone_ShouldMaintainTypeHierarchy()
	{
		// Arrange
		var original = new Container();
		original.DerivedObjects.ReplaceWith
		([
			new DerivedObjectA { Id = 1, Name = "A", SpecialPropertyA = "SpecialA" },
			new DerivedObjectB { Id = 2, Name = "B", SpecialPropertyB = 42 },
		]);

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(2, clone.DerivedObjects.Count);

		// Verify correct polymorphic types are preserved
		Assert.IsInstanceOfType<DerivedObjectA>(clone.DerivedObjects[0]);
		Assert.IsInstanceOfType<DerivedObjectB>(clone.DerivedObjects[1]);

		// Verify properties are correctly cloned
		var clonedA = (DerivedObjectA)clone.DerivedObjects[0];
		var clonedB = (DerivedObjectB)clone.DerivedObjects[1];

		Assert.AreEqual("SpecialA", clonedA.SpecialPropertyA);
		Assert.AreEqual(42, clonedB.SpecialPropertyB);

		// Modify clone
		clonedA.SpecialPropertyA = "Modified";
		clonedB.SpecialPropertyB = 100;

		// Verify originals are unchanged
		var originalA = (DerivedObjectA)original.DerivedObjects[0];
		var originalB = (DerivedObjectB)original.DerivedObjects[1];

		Assert.AreEqual("SpecialA", originalA.SpecialPropertyA);
		Assert.AreEqual(42, originalB.SpecialPropertyB);

		// Verify clones are modified
		Assert.AreEqual("Modified", clonedA.SpecialPropertyA);
		Assert.AreEqual(100, clonedB.SpecialPropertyB);
	}

	/// <summary>
	/// Tests that a deep clone works correctly with nested generic collections.
	/// </summary>
	[TestMethod]
	public void NestedGenericCollections_DeepClone_ShouldCreateIndependentCopies()
	{
		// Arrange
		var original = new GenericCollectionContainer<SimpleObject>();

		original.Items.ReplaceWith
		([
			new() { Id = 1, Name = "Item1" },
			new() { Id = 2, Name = "Item2" }
		]);

		original.ItemsMapping.ReplaceWith
		([
			new( "key1", new SimpleObject { Id = 3, Name = "MappedItem1" }),
			new( "key2", new SimpleObject { Id = 4, Name = "MappedItem2" }),
		]);

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(2, clone.Items.Count);
		Assert.AreEqual(2, clone.ItemsMapping.Count);

		// Verify list items are cloned
		Assert.AreEqual(1, clone.Items[0].Id);
		Assert.AreEqual("Item1", clone.Items[0].Name);

		// Verify dictionary items are cloned
		Assert.AreEqual(3, clone.ItemsMapping["key1"].Id);
		Assert.AreEqual("MappedItem1", clone.ItemsMapping["key1"].Name);

		// Modify clone
		clone.Items[0].Name = "ModifiedItem";
		clone.ItemsMapping["key1"].Name = "ModifiedMappedItem";

		// Verify originals unchanged
		Assert.AreEqual("Item1", original.Items[0].Name);
		Assert.AreEqual("MappedItem1", original.ItemsMapping["key1"].Name);

		// Verify clones are modified
		Assert.AreEqual("ModifiedItem", clone.Items[0].Name);
		Assert.AreEqual("ModifiedMappedItem", clone.ItemsMapping["key1"].Name);
	}

	/// <summary>
	/// Tests that circular references are handled properly during deep cloning.
	/// </summary>
	[TestMethod]
	public void CircularReference_DeepClone_ShouldCreateIndependentCopy()
	{
		// Arrange
		var original = new NodeObject { Id = 1, Name = "Parent" };
		var child = new NodeObject { Id = 2, Name = "Child", Parent = original };
		original.Children.Add(child);

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(1, clone.Id);
		Assert.AreEqual("Parent", clone.Name);
		Assert.AreEqual(1, clone.Children.Count);

		var clonedChild = clone.Children[0];
		Assert.AreEqual(2, clonedChild.Id);
		Assert.AreEqual("Child", clonedChild.Name);

		// Verify circular reference is maintained but with new objects
		Assert.IsNotNull(clonedChild.Parent);
		Assert.AreSame(clone, clonedChild.Parent); // Should reference the cloned parent
		Assert.AreNotSame(original, clonedChild.Parent); // Should not reference the original parent

		// Modify clone
		clone.Name = "ModifiedParent";
		clonedChild.Name = "ModifiedChild";

		// Verify originals unchanged
		Assert.AreEqual("Parent", original.Name);
		Assert.AreEqual("Child", original.Children[0].Name);

		// Verify clones are modified
		Assert.AreEqual("ModifiedParent", clone.Name);
		Assert.AreEqual("ModifiedChild", clonedChild.Name);
	}

	/// <summary>
	/// Tests deep cloning with abstract base classes and factory methods.
	/// </summary>
	[TestMethod]
	public void AbstractBaseClass_DeepClone_ShouldCreateCorrectDerivedTypes()
	{
		// Arrange
		var original = new ShapeContainer();
		original.Shapes.ReplaceWith
		([
			new Circle { Id = 1, Name = "Circle1", Radius = 5.0 },
			new Rectangle { Id = 2, Name = "Rectangle1", Width = 10.0, Height = 20.0 }
		]);

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(2, clone.Shapes.Count);

		// Verify correct derived types
		Assert.IsInstanceOfType<Circle>(clone.Shapes[0]);
		Assert.IsInstanceOfType<Rectangle>(clone.Shapes[1]);

		// Verify properties
		var clonedCircle = (Circle)clone.Shapes[0];
		var clonedRectangle = (Rectangle)clone.Shapes[1];

		Assert.AreEqual(5.0, clonedCircle.Radius);
		Assert.AreEqual(10.0, clonedRectangle.Width);
		Assert.AreEqual(20.0, clonedRectangle.Height);

		// Modify clone
		clonedCircle.Radius = 7.0;
		clonedRectangle.Width = 15.0;

		// Verify originals unchanged
		var originalCircle = (Circle)original.Shapes[0];
		var originalRectangle = (Rectangle)original.Shapes[1];

		Assert.AreEqual(5.0, originalCircle.Radius);
		Assert.AreEqual(10.0, originalRectangle.Width);

		// Verify clones are modified
		Assert.AreEqual(7.0, clonedCircle.Radius);
		Assert.AreEqual(15.0, clonedRectangle.Width);
	}

	/// <summary>
	/// Tests that containers with IDeepCloneable objects can be deep cloned using container extensions.
	/// </summary>
	[TestMethod]
	public void ContainerExtensions_DeepClone_ShouldCreateIndependentCopies()
	{
		// Arrange
		var originalList = new List<SimpleObject?>
		{
			new() { Id = 1, Name = "Item1" },
			new() { Id = 2, Name = "Item2" }
		};

		var originalDict = new Dictionary<string, SimpleObject?>
		{
			["key1"] = new() { Id = 3, Name = "Dict1" },
			["key2"] = new() { Id = 4, Name = "Dict2" }
		};

		var originalSet = new HashSet<SimpleObject?>
		{
			new() { Id = 5, Name = "Set1" },
			new() { Id = 6, Name = "Set2" }
		};

		var originalStack = new Stack<SimpleObject?>();
		originalStack.Push(new() { Id = 7, Name = "Stack1" });
		originalStack.Push(new() { Id = 8, Name = "Stack2" });

		// Act
		// Use IEnumerable for list to avoid ambiguity
		var clonedList = originalList.DeepClone()?.ToList();
		// Cast to IDictionary to resolve ambiguity
		var clonedDict = ((IDictionary<string, SimpleObject?>)originalDict).DeepClone();
		// Use nullable types for HashSet and Stack
		var clonedSet = originalSet.DeepClone();
		var clonedStack = originalStack.DeepClone();

		// Assert - List
		Assert.IsNotNull(clonedList);
		Assert.AreEqual(2, clonedList!.Count);
		Assert.IsNotNull(clonedList[0]);
		Assert.AreEqual(1, clonedList[0]!.Id);
		Assert.AreEqual("Item1", clonedList[0]!.Name);
		Assert.IsNotNull(clonedList[1]);
		Assert.AreEqual(2, clonedList[1]!.Id);
		Assert.AreEqual("Item2", clonedList[1]!.Name);

		// Verify list items are independent
		if (clonedList[0] != null)
		{
			clonedList[0]!.Name = "ModifiedItem1";
		}

		Assert.AreEqual("Item1", originalList[0]!.Name);

		// Assert - Dictionary
		Assert.IsNotNull(clonedDict);
		Assert.AreEqual(2, clonedDict!.Count);
		Assert.IsNotNull(clonedDict["key1"]);
		Assert.AreEqual(3, clonedDict["key1"]!.Id);
		Assert.AreEqual("Dict1", clonedDict["key1"]!.Name);
		Assert.IsNotNull(clonedDict["key2"]);
		Assert.AreEqual(4, clonedDict["key2"]!.Id);
		Assert.AreEqual("Dict2", clonedDict["key2"]!.Name);

		// Verify dictionary items are independent
		if (clonedDict["key1"] != null)
		{
			clonedDict["key1"]!.Name = "ModifiedDict1";
		}

		Assert.AreEqual("Dict1", originalDict["key1"]!.Name);

		// Assert - HashSet
		Assert.IsNotNull(clonedSet);
		Assert.AreEqual(2, clonedSet!.Count);

		// Check set elements by converting to list and sorting by ID to ensure consistent order
		var setItems = clonedSet.Where(item => item != null).OrderBy(item => item!.Id).ToList();
		Assert.AreEqual(5, setItems[0]!.Id);
		Assert.AreEqual("Set1", setItems[0]!.Name);
		Assert.AreEqual(6, setItems[1]!.Id);
		Assert.AreEqual("Set2", setItems[1]!.Name);

		// Assert - Stack (should maintain LIFO order)
		Assert.IsNotNull(clonedStack);
		Assert.AreEqual(2, clonedStack!.Count);

		var firstPopped = clonedStack.Pop();
		Assert.IsNotNull(firstPopped);
		Assert.AreEqual(8, firstPopped!.Id);
		Assert.AreEqual("Stack2", firstPopped!.Name);

		var secondPopped = clonedStack.Pop();
		Assert.IsNotNull(secondPopped);
		Assert.AreEqual(7, secondPopped!.Id);
		Assert.AreEqual("Stack1", secondPopped!.Name);
	}

	/// <summary>
	/// Tests that the IDeepCloneable interface works correctly with non-generic implementation.
	/// </summary>
	[TestMethod]
	public void IDeepCloneable_NonGeneric_ShouldCreateIndependentCopy()
	{
		// Arrange
		var original = new SimpleObject { Id = 1, Name = "TestObject" };

		// Act - Cast to IDeepCloneable and call DeepClone
		var clone = (SimpleObject)((IDeepCloneable)original).DeepClone();

		// Assert
		Assert.AreEqual(1, clone.Id);
		Assert.AreEqual("TestObject", clone.Name);

		// Modify clone
		clone.Name = "Modified";

		// Original should remain unchanged
		Assert.AreEqual("TestObject", original.Name);
	}

	/// <summary>
	/// Tests handling of null references during deep cloning.
	/// </summary>
	[TestMethod]
	public void NullReferences_DeepClone_ShouldHandleCorrectly()
	{
		// Arrange
		var original = new ComplexObject
		{
			Id = 1,
			Name = "Parent",
			Child = null, // Explicitly null
			Items = { } // Empty collection
		};

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(original.Id, clone.Id);
		Assert.AreEqual(original.Name, clone.Name);
		Assert.IsNull(clone.Child, "Null references should remain null after cloning");
		Assert.IsNotNull(clone.Items, "Empty collections should be initialized, not null");
		Assert.AreEqual(0, clone.Items.Count, "Empty collections should remain empty after cloning");
	}

	/// <summary>
	/// Tests that empty collections are properly cloned.
	/// </summary>
	[TestMethod]
	public void EmptyCollections_DeepClone_ShouldCreateIndependentEmptyCollections()
	{
		// Arrange
		var original = new Container();
		// Intentionally leave collections empty

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.IsNotNull(clone.DerivedObjects);
		Assert.AreEqual(0, clone.DerivedObjects.Count);

		// Add to clone's collection
		clone.DerivedObjects.Add(new DerivedObjectA { Id = 1, Name = "A" });

		// Original should still be empty
		Assert.AreEqual(0, original.DerivedObjects.Count);
		Assert.AreEqual(1, clone.DerivedObjects.Count);
	}

	/// <summary>
	/// Tests cloning large object hierarchies to ensure no stack overflow occurs.
	/// </summary>
	[TestMethod]
	public void LargeObjectHierarchy_DeepClone_ShouldNotStackOverflow()
	{
		// Arrange - Create a deep object hierarchy
		var root = new NodeObject { Id = 1, Name = "Root" };
		var current = root;

		// Create a chain of 100 nodes (deep hierarchy)
		for (var i = 2; i <= 100; i++)
		{
			var next = new NodeObject { Id = i, Name = $"Node{i}", Parent = current };
			current.Children.Add(next);
			current = next;
		}

		// Act - Should complete without stack overflow
		var clone = root.DeepClone();

		// Assert - Verify depth of hierarchy is maintained
		Assert.AreEqual(1, clone.Id);
		Assert.AreEqual("Root", clone.Name);

		// Navigate to the deepest node
		current = clone;
		for (var i = 2; i <= 100; i++)
		{
			Assert.AreEqual(1, current.Children.Count, $"Node at level {i - 1} should have one child");
			current = current.Children[0];
			Assert.AreEqual(i, current.Id, $"Node at level {i} should have ID {i}");
			Assert.AreEqual($"Node{i}", current.Name, $"Node at level {i} should have name Node{i}");
		}

		// Verify we reached the leaf node
		Assert.AreEqual(0, current.Children.Count, "Leaf node should have no children");
	}
}

/// <summary>
/// A simple class for testing deep cloning.
/// </summary>
public class SimpleObject : DeepCloneable<SimpleObject>
{
	/// <summary>
	/// Gets or sets the identifier.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string? Name { get; set; }

	/// <inheritdoc/>
	protected override SimpleObject CreateInstance()
	{
		return new SimpleObject();
	}

	/// <inheritdoc/>
	/// <remarks>
	/// If this class were to be used as a base class, derived classes should call base.DeepClone(clone)
	/// at the beginning of their DeepClone method to ensure all base class properties are cloned properly.
	/// 
	/// Example for a derived class:
	/// <code>
	/// protected override void DeepClone(DerivedObject clone)
	/// {
	///     // Always call base.DeepClone first to handle base class properties
	///     base.DeepClone(clone);
	///     
	///     // Then clone derived class properties
	///     clone.DerivedProperty = DerivedProperty?.DeepClone();
	/// }
	/// </code>
	/// </remarks>
	protected override void DeepClone(SimpleObject clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		clone.Id = Id;
		clone.Name = Name;
	}
}

/// <summary>
/// A complex class with nested objects for testing deep cloning.
/// </summary>
public class ComplexObject : DeepCloneable<ComplexObject>
{
	/// <summary>
	/// Gets or sets the identifier.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the child object.
	/// </summary>
	public SimpleObject? Child { get; set; }

	/// <summary>
	/// Gets the collection of items.
	/// </summary>
	public Collection<SimpleObject> Items { get; } = [];

	/// <inheritdoc/>
	protected override ComplexObject CreateInstance()
	{
		return new ComplexObject();
	}

	/// <inheritdoc/>
	/// <remarks>
	/// When implementing DeepClone in a class hierarchy:
	/// 1. Always call base.DeepClone(clone) first in derived classes
	/// 2. This ensures the base class properties are properly cloned before handling derived class properties
	/// 3. The chain of base.DeepClone calls is crucial for maintaining the integrity of the object graph
	/// 
	/// For example, if ComplexObject were extended:
	/// <code>
	/// public class MoreComplexObject : ComplexObject
	/// {
	///     public AdditionalData? ExtraInfo { get; set; }
	///     
	///     protected override void DeepClone(MoreComplexObject clone)
	///     {
	///         // First call base to handle ComplexObject properties
	///         base.DeepClone(clone);
	///         
	///         // Then handle MoreComplexObject properties
	///         clone.ExtraInfo = ExtraInfo?.DeepClone();
	///     }
	/// }
	/// </code>
	/// </remarks>
	protected override void DeepClone(ComplexObject clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		clone.Id = Id;
		clone.Name = Name;
		clone.Child = Child?.DeepClone();

		foreach (var item in Items)
		{
			clone.Items.Add(item.DeepClone());
		}
	}
}
