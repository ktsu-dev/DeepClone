// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks.Animals;

using System.Collections.ObjectModel;
using ktsu.DeepClone;

/// <summary>
/// Class that demonstrates a collection of polymorphic objects.
/// </summary>
public class AnimalShelter : DeepCloneable<AnimalShelter>
{
	/// <summary>
	/// Gets the collection of animals in the shelter.
	/// </summary>
	public Collection<IAnimal> Animals { get; } = [];

	/// <inheritdoc/>
	protected override AnimalShelter CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(AnimalShelter clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Animals.DeepCloneFrom(Animals);
	}
}
