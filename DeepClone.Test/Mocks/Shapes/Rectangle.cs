// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks.Shapes;

/// <summary>
/// Interface for rectangles to enable specific polymorphism.
/// </summary>
public interface IRectangle : IShape
{
	/// <summary>
	/// Gets or sets the width of the rectangle.
	/// </summary>
	public double Width { get; set; }

	/// <summary>
	/// Gets or sets the height of the rectangle.
	/// </summary>
	public double Height { get; set; }
}

/// <summary>
/// Rectangle shape implementation.
/// </summary>
public class Rectangle : Shape<Rectangle>, IRectangle
{
	/// <summary>
	/// Gets or sets the width of the rectangle.
	/// </summary>
	public double Width { get; set; }

	/// <summary>
	/// Gets or sets the height of the rectangle.
	/// </summary>
	public double Height { get; set; }

	protected override Rectangle CreateInstance() => new();

	protected override void DeepClone(Rectangle clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Width = Width;
		clone.Height = Height;
	}
}
