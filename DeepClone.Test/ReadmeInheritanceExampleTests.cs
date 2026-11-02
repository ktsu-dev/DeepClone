// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

/// <summary>
/// Pins the "Inheritance and Deep Cloning" sample in README.md: the hierarchy below is the README's,
/// and the tests check that it clones every level and that the README still shows these declarations.
/// </summary>
[TestClass]
public class ReadmeInheritanceExampleTests
{
	private abstract class Shape<T> : DeepCloneable<T> where T : Shape<T>
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public List<string> Tags { get; set; } = [];

		protected override void DeepClone(T clone)
		{
			ArgumentNullException.ThrowIfNull(clone);
			clone.Id = Id;
			clone.Name = Name;
			clone.Tags.DeepCloneFrom(Tags);
		}
	}

	private abstract class Circle<T> : Shape<T> where T : Circle<T>
	{
		public double Radius { get; set; }
		public Dictionary<string, double> Measurements { get; set; } = [];

		protected override void DeepClone(T clone)
		{
			base.DeepClone(clone);
			clone.Radius = Radius;
			clone.Measurements.DeepCloneFrom(Measurements);
		}
	}

	private sealed class Circle : Circle<Circle>
	{
		protected override Circle CreateInstance() => new();
	}

	private sealed class ColoredCircle : Circle<ColoredCircle>
	{
		public string? Color { get; set; }
		public HashSet<string> ColorGradients { get; set; } = [];

		protected override ColoredCircle CreateInstance() => new();

		protected override void DeepClone(ColoredCircle clone)
		{
			base.DeepClone(clone);
			clone.Color = Color;
			clone.ColorGradients.DeepCloneFrom(ColorGradients);
		}
	}

	/// <summary>
	/// The README's usage sample: every level of a three-level hierarchy is cloned, and the clone is typed as the leaf class.
	/// </summary>
	[TestMethod]
	public void ColoredCircle_DeepClone_ClonesEveryLevelWithoutCasting()
	{
		ColoredCircle original = new()
		{
			Id = 1,
			Name = "My Circle",
			Tags = { "round", "geometric" },
			Radius = 10.5,
			Measurements = { ["diameter"] = 21.0 },
			Color = "Blue",
			ColorGradients = { "lightblue", "darkblue" },
		};

		ColoredCircle clone = original.DeepClone();

		Assert.AreNotSame(original, clone);
		Assert.AreEqual(1, clone.Id);
		Assert.AreEqual("My Circle", clone.Name);
		CollectionAssert.AreEqual(original.Tags, clone.Tags);
		Assert.AreNotSame(original.Tags, clone.Tags);
		Assert.AreEqual(10.5, clone.Radius);
		Assert.AreEqual(21.0, clone.Measurements["diameter"]);
		Assert.AreNotSame(original.Measurements, clone.Measurements);
		Assert.AreEqual("Blue", clone.Color);
		Assert.IsTrue(clone.ColorGradients.SetEquals(original.ColorGradients));
		Assert.AreNotSame(original.ColorGradients, clone.ColorGradients);
	}

	/// <summary>
	/// The intermediate class of the sample is also usable as a concrete class of its own.
	/// </summary>
	[TestMethod]
	public void Circle_DeepClone_ClonesShapeAndCircleProperties()
	{
		Circle original = new() { Id = 2, Tags = { "plain" }, Radius = 3.0 };

		Circle clone = original.DeepClone();

		Assert.AreNotSame(original, clone);
		Assert.AreEqual(2, clone.Id);
		CollectionAssert.AreEqual(original.Tags, clone.Tags);
		Assert.AreEqual(3.0, clone.Radius);
	}

	/// <summary>
	/// README.md declares the hierarchy exactly as this test does, so the sample above stays the one that compiles.
	/// </summary>
	/// <param name="declaration">A declaration that must appear in README.md.</param>
	[TestMethod]
	[DataRow("public abstract class Shape<T> : DeepCloneable<T> where T : Shape<T>")]
	[DataRow("public abstract class Circle<T> : Shape<T> where T : Circle<T>")]
	[DataRow("public class Circle : Circle<Circle>")]
	[DataRow("public class ColoredCircle : Circle<ColoredCircle>")]
	[DataRow("ColoredCircle clone = original.DeepClone();")]
	public void Readme_DeclaresTheTestedHierarchy(string declaration)
	{
		string readme = File.ReadAllText(FindReadme());

		Assert.Contains(declaration, readme);
	}

	/// <summary>
	/// README.md no longer derives from a class that has already closed the CRTP parameter.
	/// </summary>
	[TestMethod]
	public void Readme_DoesNotDeriveFromAClosedCrtpClass()
	{
		string readme = File.ReadAllText(FindReadme());

		Assert.DoesNotContain("public class ColoredCircle : Circle\n", readme.ReplaceLineEndings("\n"));
		Assert.DoesNotContain("public class Circle : Shape<Circle>", readme);
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
