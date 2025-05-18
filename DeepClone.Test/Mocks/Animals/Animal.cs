// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks.Animals;

using ktsu.DeepClone;

/// <summary>
/// Non-generic base interface for all animals to enable polymorphism.
/// </summary>
public interface IAnimal
{
	/// <summary>
	/// Gets or sets the name of the animal.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the age of the animal.
	/// </summary>
	public int Age { get; set; }
}

/// <summary>
/// Base class in a polymorphic hierarchy demonstrating the DeepCloneableBase pattern.
/// </summary>
public abstract class Animal<TDerived> : DeepCloneable<TDerived>, IAnimal
	where TDerived : Animal<TDerived>
{
	/// <summary>
	/// Gets or sets the name of the animal.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the age of the animal.
	/// </summary>
	public int Age { get; set; }

	/// <inheritdoc/>
	protected override void DeepClone(TDerived clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Name = Name;
		clone.Age = Age;
	}
}
