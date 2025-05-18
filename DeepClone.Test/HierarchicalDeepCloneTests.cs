// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test;

using ktsu.DeepClone.Test.Mocks.Animals;

/// <summary>
/// Tests for the hierarchical deep cloning functionality.
/// </summary>
[TestClass]
public class HierarchicalDeepCloneTests
{
	/// <summary>
	/// Tests that a Dog can be properly deep cloned with all properties from the inheritance hierarchy.
	/// </summary>
	[TestMethod]
	public void Dog_DeepClone_ShouldClonePropertiesFromEntireHierarchy()
	{
		// Arrange
		var original = new Dog
		{
			Name = "Rex",
			Age = 5,
			NumberOfLegs = 4,
			FurColor = "Brown",
			Breed = "German Shepherd",
			IsTrained = true,
		};

		// Act
		var clone = original.DeepClone();
		var dogClone = clone;

		// Assert
		Assert.IsInstanceOfType<Dog>(clone);

		// Verify base Animal properties
		Assert.AreEqual("Rex", dogClone.Name);
		Assert.AreEqual(5, dogClone.Age);

		// Verify intermediate Mammal properties
		Assert.AreEqual(4, dogClone.NumberOfLegs);
		Assert.AreEqual("Brown", dogClone.FurColor);

		// Verify Dog-specific properties
		Assert.AreEqual("German Shepherd", dogClone.Breed);
		Assert.AreEqual(true, dogClone.IsTrained);
	}

	/// <summary>
	/// Tests that modifying a cloned Dog doesn't affect the original.
	/// </summary>
	[TestMethod]
	public void Dog_ModifyClone_ShouldNotAffectOriginal()
	{
		// Arrange
		var original = new Dog
		{
			Name = "Rex",
			Age = 5,
			NumberOfLegs = 4,
			FurColor = "Brown",
			Breed = "German Shepherd",
			IsTrained = true,
		};

		// Act
		var clone = original.DeepClone();
		var dogClone = clone;

		// Modify all properties in the clone
		dogClone.Name = "Rover";
		dogClone.Age = 3;
		dogClone.NumberOfLegs = 3; // Injured dog
		dogClone.FurColor = "Black";
		dogClone.Breed = "Mixed";
		dogClone.IsTrained = false;

		// Assert - original should remain unchanged
		Assert.AreEqual("Rex", original.Name);
		Assert.AreEqual(5, original.Age);
		Assert.AreEqual(4, original.NumberOfLegs);
		Assert.AreEqual("Brown", original.FurColor);
		Assert.AreEqual("German Shepherd", original.Breed);
		Assert.AreEqual(true, original.IsTrained);
	}

	/// <summary>
	/// Tests that a Cat can be properly deep cloned with all properties from the inheritance hierarchy.
	/// </summary>
	[TestMethod]
	public void Cat_DeepClone_ShouldClonePropertiesFromEntireHierarchy()
	{
		// Arrange
		var original = new Cat
		{
			Name = "Whiskers",
			Age = 3,
			NumberOfLegs = 4,
			FurColor = "Orange",
			IsIndoor = true,
			FavoriteToy = "Ball of Yarn",
		};

		// Act
		var clone = original.DeepClone();
		var catClone = clone;

		// Assert
		Assert.IsInstanceOfType<Cat>(clone);

		// Verify base Animal properties
		Assert.AreEqual("Whiskers", catClone.Name);
		Assert.AreEqual(3, catClone.Age);

		// Verify intermediate Mammal properties
		Assert.AreEqual(4, catClone.NumberOfLegs);
		Assert.AreEqual("Orange", catClone.FurColor);

		// Verify Cat-specific properties
		Assert.AreEqual(true, catClone.IsIndoor);
		Assert.AreEqual("Ball of Yarn", catClone.FavoriteToy);
	}

	/// <summary>
	/// Tests that polymorphic collections can be properly cloned.
	/// </summary>
	[TestMethod]
	public void PolymorphicCollection_DeepClone_ShouldMaintainTypeAndProperties()
	{
		// Arrange
		var original = new AnimalShelter();
		original.Animals.Add(new Dog { Name = "Rex", Age = 5, Breed = "German Shepherd" });
		original.Animals.Add(new Cat { Name = "Whiskers", Age = 3, IsIndoor = true });

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(2, clone.Animals.Count);

		// Verify first animal is a Dog with correct properties
		Assert.IsInstanceOfType<Dog>(clone.Animals[0]);
		var dog = (Dog)clone.Animals[0];
		Assert.AreEqual("Rex", dog.Name);
		Assert.AreEqual(5, dog.Age);
		Assert.AreEqual("German Shepherd", dog.Breed);

		// Verify second animal is a Cat with correct properties
		Assert.IsInstanceOfType<Cat>(clone.Animals[1]);
		var cat = (Cat)clone.Animals[1];
		Assert.AreEqual("Whiskers", cat.Name);
		Assert.AreEqual(3, cat.Age);
		Assert.AreEqual(true, cat.IsIndoor);

		// Modify the clone and ensure originals aren't affected
		dog.Name = "Modified";
		cat.Name = "Changed";

		Assert.AreEqual("Rex", ((Dog)original.Animals[0]).Name);
		Assert.AreEqual("Whiskers", ((Cat)original.Animals[1]).Name);

		// Verify clones are modified
		Assert.AreEqual("Modified", dog.Name);
		Assert.AreEqual("Changed", cat.Name);
	}
}
