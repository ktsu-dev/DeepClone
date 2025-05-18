// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks.Shapes;

using ktsu.DeepClone;

/// <summary>
/// Non-generic interface for shapes to enable polymorphism.
/// </summary>
public interface IShape
{
	/// <summary>
	/// Gets or sets the identifier for this shape.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the name of this shape.
	/// </summary>
	public string? Name { get; set; }
}

/// <summary>
/// Abstract base class for testing inheritance with factory methods.
/// </summary>
public abstract class Shape<T> : DeepCloneable<T>, IShape
	where T : Shape<T>
{
	/// <summary>
	/// Gets or sets the identifier for this shape.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the name of this shape.
	/// </summary>
	public string? Name { get; set; }

	/// <inheritdoc/>
	protected override void DeepClone(T clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Id = Id;
		clone.Name = Name;
	}
}
