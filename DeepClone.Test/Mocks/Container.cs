// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks;

using System.Collections.ObjectModel;

/// <summary>
/// Container for storing polymorphic objects.
/// </summary>
public class Container : DeepCloneable<Container>
{
	/// <summary>
	/// Gets the collection of base objects.
	/// </summary>
	public Collection<IBaseObject> DerivedObjects { get; } = [];

	/// <inheritdoc/>
	protected override Container CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(Container clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.DerivedObjects.DeepCloneFrom(DerivedObjects);
	}
}
