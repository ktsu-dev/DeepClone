// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test.Mocks.Shapes;

using System.Collections.ObjectModel;
using ktsu.DeepClone;

/// <summary>
/// Container for storing Shape objects.
/// </summary>
public class ShapeContainer : DeepCloneable<ShapeContainer>
{
	/// <summary>
	/// Gets the collection of shapes.
	/// </summary>
	public Collection<IShape> Shapes { get; } = [];

	/// <inheritdoc/>
	protected override ShapeContainer CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(ShapeContainer clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Shapes.DeepCloneFrom(Shapes);
	}
}
