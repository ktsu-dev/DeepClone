// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.Immutable;

/// <summary>
/// Pins the example in README.md's "Performance Considerations" section: the types below are the README's,
/// and the tests check that its clones stay independent and that the README still shows this pattern.
/// </summary>
[TestClass]
public class ReadmePerformanceExampleTests
{
	private static readonly int[] OneTwoThree = [1, 2, 3];

	private sealed class LookupTable(IEnumerable<int> values)
	{
		public ImmutableArray<int> Values { get; } = [.. values];
	}

	private sealed class OptimizedObject : DeepCloneable<OptimizedObject>
	{
		public LookupTable? Table { get; set; }

		public List<int> Scratch { get; set; } = [];

		protected override OptimizedObject CreateInstance() => new();

		protected override void DeepClone(OptimizedObject clone)
		{
			ArgumentNullException.ThrowIfNull(clone);
			clone.Table = Table;
			clone.Scratch.DeepCloneFrom(Scratch);
		}
	}

	/// <summary>
	/// Clones of one source share the immutable table but not their mutable state,
	/// so an edit through one clone does not appear in another or in the original.
	/// </summary>
	[TestMethod]
	public void OptimizedObject_Clones_AreIndependentOfEachOther()
	{
		OptimizedObject original = new() { Table = new LookupTable([1, 2, 3]), Scratch = { 1, 2, 3 } };

		OptimizedObject a = original.DeepClone();
		OptimizedObject b = original.DeepClone();

		Assert.AreNotSame(a.Scratch, b.Scratch);
		a.Scratch.Add(99);
		CollectionAssert.AreEqual(OneTwoThree, b.Scratch);

		original.Scratch.Add(4);
		OptimizedObject c = original.DeepClone();
		CollectionAssert.AreEqual(original.Scratch, c.Scratch);

		a.Table = new LookupTable([7]);
		Assert.AreSame(original.Table, b.Table);
		CollectionAssert.AreEqual(OneTwoThree, b.Table!.Values.ToArray());
	}

	/// <summary>
	/// README.md declares the example exactly as this test does.
	/// </summary>
	/// <param name="line">A line that must appear in README.md.</param>
	[TestMethod]
	[DataRow("public sealed class LookupTable")]
	[DataRow("public ImmutableArray<int> Values { get; }")]
	[DataRow("clone.Table = Table;")]
	[DataRow("clone.Scratch.DeepCloneFrom(Scratch);")]
	public void Readme_ShowsTheTestedExample(string line)
	{
		Assert.Contains(line, File.ReadAllText(FindReadme()));
	}

	/// <summary>
	/// README.md no longer hands one cached clone of a mutable resource to every clone.
	/// </summary>
	[TestMethod]
	public void Readme_DoesNotCacheAClonedMutableResource()
	{
		string readme = File.ReadAllText(FindReadme());

		Assert.DoesNotContain("_clonedResource", readme);
		Assert.DoesNotContain("Lazy Clone When Possible", readme);
	}

	private static string FindReadme()
	{
		for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
		{
			string candidate = Path.Combine(dir.FullName, "README.md");
			if (File.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "DeepClone.sln")))
			{
				return candidate;
			}
		}

		Assert.Fail("README.md not found above the test output directory.");
		return string.Empty;
	}
}
