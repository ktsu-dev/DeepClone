// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks;

/// <summary>
/// Interface for DerivedObjectA to enable specific polymorphism.
/// </summary>
public interface IDerivedObjectA : IBaseObject
{
	/// <summary>
	/// Gets or sets the special property specific to DerivedObjectA.
	/// </summary>
	public string? SpecialPropertyA { get; set; }
}

/// <summary>
/// First derived class for testing polymorphic cloning.
/// </summary>
public class DerivedObjectA : BaseObject<DerivedObjectA>, IDerivedObjectA
{
	/// <summary>
	/// Gets or sets the special property specific to DerivedObjectA.
	/// </summary>
	public string? SpecialPropertyA { get; set; }

	/// <inheritdoc/>
	protected override DerivedObjectA CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(DerivedObjectA clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.SpecialPropertyA = SpecialPropertyA;
	}
}
