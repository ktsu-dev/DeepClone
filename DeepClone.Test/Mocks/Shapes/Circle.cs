// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks.Shapes;

/// <summary>
/// Interface for circles to enable specific polymorphism.
/// </summary>
public interface ICircle : IShape
{
	/// <summary>
	/// Gets or sets the radius of the circle.
	/// </summary>
	public double Radius { get; set; }
}

/// <summary>
/// Circle shape implementation.
/// </summary>
public class Circle : Shape<Circle>, ICircle
{
	/// <summary>
	/// Gets or sets the radius of the circle.
	/// </summary>
	public double Radius { get; set; }

	/// <inheritdoc/>
	protected override Circle CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(Circle clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Radius = Radius;
	}
}
