// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test.Mocks;

/// <summary>
/// Interface for base objects to enable polymorphism.
/// </summary>
public interface IBaseObject
{
	/// <summary>
	/// Gets or sets the identifier for this object.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the name of this object.
	/// </summary>
	public string? Name { get; set; }
}

/// <summary>
/// Base class for testing polymorphic cloning.
/// </summary>
public abstract class BaseObject<TDerived> : DeepCloneable<TDerived>, IBaseObject
	where TDerived : BaseObject<TDerived>
{
	/// <summary>
	/// Gets or sets the identifier for this object.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the name of this object.
	/// </summary>
	public string? Name { get; set; }

	/// <inheritdoc/>
	protected override void DeepClone(TDerived clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Id = Id;
		clone.Name = Name;
	}
}
