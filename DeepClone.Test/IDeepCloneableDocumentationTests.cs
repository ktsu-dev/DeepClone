// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Xml.Linq;

/// <summary>
/// Tests that the implementation pattern documented on <see cref="IDeepCloneable.DeepClone"/> works.
/// </summary>
[TestClass]
public class IDeepCloneableDocumentationTests
{
	private const string ExplicitImplementation = "object IDeepCloneable.DeepClone() => DeepClone();";
	private const string SelfRecursiveImplementation = "public object DeepClone() => DeepClone();";

	/// <summary>
	/// A type that implements <see cref="IDeepCloneable"/> exactly as the interface's remarks show.
	/// </summary>
	private sealed class Foo : IDeepCloneable
	{
		public List<int> Value { get; init; } = [];

		public Foo DeepClone() => new() { Value = [.. Value] };

		object IDeepCloneable.DeepClone() => DeepClone();
	}

	/// <summary>
	/// The documented pattern clones through the interface without recursing into itself.
	/// </summary>
	[TestMethod]
	public void DocumentedPattern_DeepCloneThroughInterface_ReturnsIndependentCopy()
	{
		Foo original = new() { Value = [1, 2, 3] };

		object clone = ((IDeepCloneable)original).DeepClone();

		Foo typedClone = Assert.IsInstanceOfType<Foo>(clone);
		Assert.AreNotSame(original, typedClone);
		Assert.AreNotSame(original.Value, typedClone.Value);
		CollectionAssert.AreEqual(original.Value, typedClone.Value);
	}

	/// <summary>
	/// The remarks shown in IntelliSense recommend the explicit interface implementation,
	/// not the self-recursive <c>public object DeepClone() => DeepClone();</c>.
	/// </summary>
	[TestMethod]
	public void Remarks_RecommendExplicitInterfaceImplementation()
	{
		string xmlPath = Path.ChangeExtension(typeof(IDeepCloneable).Assembly.Location, ".xml");
		Assert.IsTrue(File.Exists(xmlPath), $"Documentation file not found: {xmlPath}");

		XElement member = XDocument.Load(xmlPath)
			.Descendants("member")
			.Single(m => (string?)m.Attribute("name") == "M:ktsu.DeepClone.IDeepCloneable.DeepClone");
		string remarks = member.Element("remarks")?.Value ?? string.Empty;

		Assert.Contains(ExplicitImplementation, remarks);
		Assert.DoesNotContain("For example: " + SelfRecursiveImplementation, remarks);
	}
}
