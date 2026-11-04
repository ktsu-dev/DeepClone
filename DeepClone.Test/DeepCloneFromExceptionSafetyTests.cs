// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

/// <summary>
/// Tests that a <c>DeepCloneFrom</c> that throws part-way leaves the destination with its original contents,
/// instead of cleared and half-filled with the source's (ktsu-dev/DeepClone#90).
/// </summary>
[TestClass]
public class DeepCloneFromExceptionSafetyTests
{
	private static readonly string[] OriginalNames = ["keep1", "keep2"];

	/// <summary>
	/// Tests that an element whose DeepClone throws leaves the destination list unchanged.
	/// </summary>
	[TestMethod]
	public void Collection_ElementCloneThrows_DestinationUnchanged()
	{
		ThrowingItem keep1 = new("keep1");
		ThrowingItem keep2 = new("keep2");
		List<ThrowingItem> dest = [keep1, keep2];
		List<ThrowingItem> source = [new("a"), new("boom"), new("c")];

		Assert.ThrowsExactly<InvalidOperationException>(() => dest.DeepCloneFrom(source));

		Assert.AreSequenceEqual(OriginalNames, dest.Select(item => item.Name));
		Assert.AreSame(keep1, dest[0]);
		Assert.AreSame(keep2, dest[1]);
	}

	/// <summary>
	/// Tests that a key the destination's comparer rejects as a duplicate leaves the destination dictionary unchanged.
	/// </summary>
	[TestMethod]
	public void Dictionary_AddRejected_DestinationUnchanged()
	{
		Dictionary<string, int> dest = new(StringComparer.OrdinalIgnoreCase) { ["keep"] = 1 };
		Dictionary<string, int> source = new() { ["a"] = 1, ["A"] = 2 };

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom(source));

		Assert.HasCount(1, dest);
		Assert.AreEqual(1, dest["keep"]);
		Assert.IsFalse(dest.ContainsKey("a"));
	}

	/// <summary>
	/// Tests that a value whose DeepClone throws leaves the destination dictionary unchanged.
	/// </summary>
	[TestMethod]
	public void Dictionary_ValueCloneThrows_DestinationUnchanged()
	{
		ThrowingItem keep = new("keep");
		Dictionary<string, ThrowingItem> dest = new() { ["keep"] = keep };
		Dictionary<string, ThrowingItem> source = new() { ["a"] = new("a"), ["b"] = new("boom") };

		Assert.ThrowsExactly<InvalidOperationException>(() => dest.DeepCloneFrom(source));

		Assert.HasCount(1, dest);
		Assert.AreSame(keep, dest["keep"]);
	}

	/// <summary>
	/// Tests that a successful DeepCloneFrom still replaces the destination's contents with clones.
	/// </summary>
	[TestMethod]
	public void Collection_Succeeds_ReplacesContentsWithClones()
	{
		ThrowingItem a = new("a");
		List<ThrowingItem> dest = [new("keep")];

		dest.DeepCloneFrom([a]);

		Assert.HasCount(1, dest);
		Assert.AreEqual("a", dest[0].Name);
		Assert.AreNotSame(a, dest[0]);
	}

	/// <summary>
	/// A cloneable item whose clone throws when its name is "boom".
	/// </summary>
	/// <param name="name">The item's name.</param>
	private sealed class ThrowingItem(string name) : DeepCloneable<ThrowingItem>
	{
		/// <summary>
		/// Gets the item's name.
		/// </summary>
		public string Name { get; private set; } = name;

		/// <inheritdoc/>
		protected override ThrowingItem CreateInstance() =>
			Name == "boom" ? throw new InvalidOperationException("boom") : new(string.Empty);

		/// <inheritdoc/>
		protected override void DeepClone(ThrowingItem clone)
		{
			base.DeepClone(clone);
			clone.Name = Name;
		}
	}
}
