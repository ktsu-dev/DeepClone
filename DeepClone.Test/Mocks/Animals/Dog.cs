// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks.Animals;

/// <summary>
/// Interface for Dog type to enable specific polymorphism.
/// </summary>
public interface IDog : IMammal
{
	/// <summary>
	/// Gets or sets the breed of the dog.
	/// </summary>
	public string? Breed { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the dog is trained.
	/// </summary>
	public bool IsTrained { get; set; }
}

/// <summary>
/// Concrete implementation of the Animal hierarchy.
/// </summary>
public class Dog : Mammal<Dog>, IDog
{
	/// <summary>
	/// Gets or sets the breed of the dog.
	/// </summary>
	public string? Breed { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the dog is trained.
	/// </summary>
	public bool IsTrained { get; set; }

	/// <inheritdoc/>
	protected override Dog CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(Dog clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Breed = Breed;
		clone.IsTrained = IsTrained;
	}
}
