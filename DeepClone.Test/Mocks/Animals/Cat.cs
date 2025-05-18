// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks.Animals;

/// <summary>
/// Interface for Cat type to enable specific polymorphism.
/// </summary>
public interface ICat : IMammal
{
	/// <summary>
	/// Gets or sets a value indicating whether the cat is an indoor cat.
	/// </summary>
	public bool IsIndoor { get; set; }

	/// <summary>
	/// Gets or sets the favorite toy of the cat.
	/// </summary>
	public string? FavoriteToy { get; set; }
}

/// <summary>
/// Another concrete implementation of the Animal hierarchy
/// demonstrating sibling inheritance.
/// </summary>
public class Cat : Mammal<Cat>, ICat
{
	/// <summary>
	/// Gets or sets a value indicating whether the cat is an indoor cat.
	/// </summary>
	public bool IsIndoor { get; set; }

	/// <summary>
	/// Gets or sets the favorite toy of the cat.
	/// </summary>
	public string? FavoriteToy { get; set; }

	/// <inheritdoc/>
	protected override Cat CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(Cat clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.IsIndoor = IsIndoor;
		clone.FavoriteToy = FavoriteToy;
	}
}
