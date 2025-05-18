// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone.Test.Mocks;

using System.Collections.ObjectModel;

/// <summary>
/// Class for testing circular references.
/// </summary>
public class NodeObject : DeepCloneable<NodeObject>
{
	/// <summary>
	/// Gets or sets the identifier for this node.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the name of this node.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets the parent node, creating a circular reference.
	/// </summary>
	public NodeObject? Parent { get; set; }

	/// <summary>
	/// Gets the collection of child nodes.
	/// </summary>
	public Collection<NodeObject> Children { get; } = [];

	/// <inheritdoc/>
	protected override NodeObject CreateInstance() => new();

	/// <inheritdoc/>
	protected override void DeepClone(NodeObject clone)
	{
		ArgumentNullException.ThrowIfNull(clone);
		base.DeepClone(clone);
		clone.Id = Id;
		clone.Name = Name;

		// Handle circular reference - if we're the parent's child, we don't need to clone parent
		if (Parent?.Children.Contains(this) == true)
		{
			clone.Parent = Parent;
		}
		else if (Parent != null)
		{
			// Otherwise clone the parent
			clone.Parent = Parent.DeepClone();
		}

		clone.Children.DeepCloneFrom(Children);

		// maintain references
		foreach (var child in clone.Children)
		{
			child.Parent = clone;
		}
	}
}
