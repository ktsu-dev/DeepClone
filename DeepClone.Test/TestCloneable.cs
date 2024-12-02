namespace ktsu.DeepClone.Tests;

public class TestCloneable(int value) : IDeepCloneable<TestCloneable>
{
	public int Value { get; set; } = value;

	public TestCloneable DeepClone()
	{
		return new TestCloneable(Value);
	}
}

[TestClass]
public class DeepCloneTests
{
	[TestMethod]
	public void DeepClone_ShouldReturnNewInstance()
	{
		// Arrange
		var original = new TestCloneable(42);

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.IsNotNull(clone);
		Assert.AreNotSame(original, clone);
	}

	[TestMethod]
	public void DeepClone_ShouldCopyValues()
	{
		// Arrange
		var original = new TestCloneable(42);

		// Act
		var clone = original.DeepClone();

		// Assert
		Assert.AreEqual(original.Value, clone.Value);
	}
}
