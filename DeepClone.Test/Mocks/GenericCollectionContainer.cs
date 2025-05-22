// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks;

using System.Collections.ObjectModel;

/// <summary>
/// Container with generic collections for testing.
/// </summary>
public class GenericCollectionContainer<TItem> : DeepCloneable<GenericCollectionContainer<TItem>>
	where TItem : IDeepCloneable
{
	/// <summary>
	/// Gets the collection of items.
	/// </summary>
	public Collection<TItem> Items { get; init; } = [];

	/// <summary>
	/// Gets the dictionary of items.
	/// </summary>
	public Dictionary<string, TItem> ItemsMapping { get; init; } = [];

	protected override GenericCollectionContainer<TItem> CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(GenericCollectionContainer<TItem> clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Items.DeepCloneFrom(Items);
		clone.ItemsMapping.DeepCloneFrom(ItemsMapping);
	}
}
