// Copyright (c) 2023-2026 ktsu-dev contributors

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

	/// <inheritdoc/>
	protected override DerivedObjectB CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(DerivedObjectB clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.SpecialPropertyB = SpecialPropertyB;
	}
}
