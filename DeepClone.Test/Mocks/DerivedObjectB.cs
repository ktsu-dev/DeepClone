// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks;

/// <summary>
/// Interface for DerivedObjectB to enable specific polymorphism.
/// </summary>
public interface IDerivedObjectB : IBaseObject
{
	/// <summary>
	/// Gets or sets the special property specific to DerivedObjectB.
	/// </summary>
	public int SpecialPropertyB { get; set; }
}

/// <summary>
/// Second derived class for testing polymorphic cloning.
/// </summary>
public class DerivedObjectB : BaseObject<DerivedObjectB>, IDerivedObjectB
{
	/// <summary>
	/// Gets or sets the special property specific to DerivedObjectB.
	/// </summary>
	public int SpecialPropertyB { get; set; }

	protected override DerivedObjectB CreateInstance() => new();

	protected override void DeepClone(DerivedObjectB clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.SpecialPropertyB = SpecialPropertyB;
	}
}
