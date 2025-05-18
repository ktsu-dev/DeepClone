// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks.Animals;
/// <summary>
/// Non-generic interface for mammals to enable polymorphism with more specific animal types.
/// </summary>
public interface IMammal : IAnimal
{
	/// <summary>
	/// Gets or sets the number of legs.
	/// </summary>
	public int NumberOfLegs { get; set; }

	/// <summary>
	/// Gets or sets the fur color.
	/// </summary>
	public string? FurColor { get; set; }
}

/// <summary>
/// Intermediate class in the hierarchy adding mammal-specific properties.
/// </summary>
public abstract class Mammal<TDerived> : Animal<TDerived>, IMammal
	where TDerived : Mammal<TDerived>
{
	/// <summary>
	/// Gets or sets the number of legs.
	/// </summary>
	public int NumberOfLegs { get; set; }

	/// <summary>
	/// Gets or sets the fur color.
	/// </summary>
	public string? FurColor { get; set; }

	/// <inheritdoc/>
	protected override void DeepClone(TDerived clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.NumberOfLegs = NumberOfLegs;
		clone.FurColor = FurColor;
	}
}
