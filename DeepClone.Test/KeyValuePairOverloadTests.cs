// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone.Test;

using System.Collections.ObjectModel;

/// <summary>
/// Tests for the <see cref="KeyValuePair{TKey, TValue}"/> overloads, which clone pairs without building a cloner by
/// reflection, so they work under NativeAOT for value-type keys and values (ktsu-dev/DeepClone#92).
/// </summary>
[TestClass]
public class KeyValuePairOverloadTests
{
	/// <summary>
	/// Tests that a pair sequence binds to the pair overload of DeepClone. Before #92 there was none, so a sequence of
	/// <c>KeyValuePair&lt;int, T&gt;</c> went through the reflection path that throws under NativeAOT.
	/// </summary>
	[TestMethod]
	public void PairSequence_DeepClone_BindsToThePairOverload()
	{
		Func<IEnumerable<KeyValuePair<int, SimpleObject>>, IEnumerable<KeyValuePair<int, SimpleObject>>> clone =
			DeepCloneContainerExtensions.DeepClone<int, SimpleObject>;

		Assert.AreEqual(nameof(DeepCloneContainerExtensions.DeepClone), clone.Method.Name);
		Assert.AreEqual(typeof(IEnumerable<KeyValuePair<int, SimpleObject>>), clone.Method.GetParameters()[0].ParameterType);
	}

	/// <summary>
	/// Tests that a pair collection binds to the pair overload of DeepCloneFrom.
	/// </summary>
	[TestMethod]
	public void PairCollection_DeepCloneFrom_BindsToThePairOverload()
	{
		Action<ICollection<KeyValuePair<int, SimpleObject>>, IEnumerable<KeyValuePair<int, SimpleObject>>> cloneFrom =
			DeepCloneContainerExtensions.DeepCloneFrom<int, SimpleObject>;

		Assert.AreEqual(typeof(ICollection<KeyValuePair<int, SimpleObject>>), cloneFrom.Method.GetParameters()[0].ParameterType);
	}

	/// <summary>
	/// Tests that the pair overload of DeepClone clones the values of a sequence with value-type keys.
	/// </summary>
	[TestMethod]
	public void PairSequence_DeepClone_ClonesValues()
	{
		SimpleObject value = new() { Id = 1, Name = "One" };
		IEnumerable<KeyValuePair<int, SimpleObject>> source = new Dictionary<int, SimpleObject> { [1] = value }.Select(pair => pair);

		KeyValuePair<int, SimpleObject>[] clone = [.. source.DeepClone()];

		Assert.HasCount(1, clone);
		Assert.AreEqual(1, clone[0].Key);
		Assert.AreNotSame(value, clone[0].Value);
		Assert.AreEqual("One", clone[0].Value.Name);
	}

	/// <summary>
	/// Tests that the pair overload of DeepClone clones a value shared by two pairs once, so it stays shared.
	/// </summary>
	[TestMethod]
	public void PairSequence_DeepClone_KeepsSharedValuesShared()
	{
		SimpleObject shared = new() { Id = 1, Name = "Shared" };
		KeyValuePair<int, SimpleObject>[] source = [new(1, shared), new(2, shared)];

		KeyValuePair<int, SimpleObject>[] clone = [.. ((IEnumerable<KeyValuePair<int, SimpleObject>>)source).DeepClone()];

		Assert.AreNotSame(shared, clone[0].Value);
		Assert.AreSame(clone[0].Value, clone[1].Value);
	}

	/// <summary>
	/// Tests that the pair overload of DeepClone returns a snapshot that doesn't track later source changes.
	/// </summary>
	[TestMethod]
	public void PairSequence_DeepClone_IsASnapshot()
	{
		List<KeyValuePair<int, int>> source = [new(1, 2)];

		IEnumerable<KeyValuePair<int, int>> clone = ((IEnumerable<KeyValuePair<int, int>>)source).DeepClone();
		source.Add(new(3, 4));

		Assert.AreEqual(1, clone.Count());
	}

	/// <summary>
	/// Tests that the pair overload of DeepClone throws for a null source.
	/// </summary>
	[TestMethod]
	public void PairSequence_DeepClone_ThrowsForNull()
	{
		IEnumerable<KeyValuePair<int, int>> source = null!;

		Assert.ThrowsExactly<ArgumentNullException>(() => source.DeepClone());
	}

	/// <summary>
	/// Tests that a read-only dictionary of value-type keys clones into a list of pairs through the pair overload of
	/// DeepCloneFrom, cloning its values.
	/// </summary>
	[TestMethod]
	public void PairCollection_DeepCloneFrom_ClonesValuesFromReadOnlyDictionary()
	{
		SimpleObject value = new() { Id = 1, Name = "One" };
		IReadOnlyDictionary<int, SimpleObject> source = new Dictionary<int, SimpleObject> { [1] = value };
		List<KeyValuePair<int, SimpleObject>> dest = [new(9, new SimpleObject())];

		dest.DeepCloneFrom(source);

		Assert.HasCount(1, dest);
		Assert.AreEqual(1, dest[0].Key);
		Assert.AreNotSame(value, dest[0].Value);
		Assert.AreEqual("One", dest[0].Value.Name);
	}

	/// <summary>
	/// Tests that the pair overload of DeepCloneFrom refuses a read-only destination before cloning.
	/// </summary>
	[TestMethod]
	public void PairCollection_DeepCloneFrom_ThrowsForReadOnlyDestination()
	{
		ICollection<KeyValuePair<int, int>> dest = new ReadOnlyCollection<KeyValuePair<int, int>>([new(1, 2)]);

		Assert.ThrowsExactly<ArgumentException>(() => dest.DeepCloneFrom([new KeyValuePair<int, int>(3, 4)]));
		Assert.AreEqual(1, dest.Single().Key);
	}

	/// <summary>
	/// Tests that the pair overload of DeepCloneFrom throws for a null destination or source.
	/// </summary>
	[TestMethod]
	public void PairCollection_DeepCloneFrom_ThrowsForNull()
	{
		ICollection<KeyValuePair<int, int>> dest = [];

		Assert.ThrowsExactly<ArgumentNullException>(() => ((ICollection<KeyValuePair<int, int>>)null!).DeepCloneFrom([]));
		Assert.ThrowsExactly<ArgumentNullException>(() => dest.DeepCloneFrom(null!));
	}

	/// <summary>
	/// Tests that a list of pairs, which binds to the <see cref="List{T}"/> overload and so reaches its pairs through a
	/// generic element type, still clones each pair's value through the reflection path where it is available (#82).
	/// </summary>
	[TestMethod]
	public void PairList_DeepClone_ClonesValuesThroughTheGenericPath()
	{
		SimpleObject value = new() { Id = 1, Name = "One" };
		List<KeyValuePair<int, SimpleObject>> source = [new(1, value)];

		List<KeyValuePair<int, SimpleObject>> clone = source.DeepClone();

		Assert.HasCount(1, clone);
		Assert.AreNotSame(value, clone[0].Value);
		Assert.AreEqual("One", clone[0].Value.Name);
	}

	/// <summary>
	/// Tests that the pair overload of DeepClone hands a source that is itself <see cref="IDeepCloneable"/> to its own
	/// DeepClone, as the <see cref="IEnumerable{T}"/> overload does.
	/// </summary>
	[TestMethod]
	public void PairSequence_DeepClone_UsesTheSourcesOwnDeepClone()
	{
		IEnumerable<KeyValuePair<int, int>> source = new CloneablePairs([new(1, 2)]);

		IEnumerable<KeyValuePair<int, int>> clone = source.DeepClone();

		Assert.IsInstanceOfType<CloneablePairs>(clone);
		Assert.AreNotSame(source, clone);
		Assert.AreEqual(2, clone.Single().Value);
	}

	/// <summary>
	/// A sequence of pairs that clones itself.
	/// </summary>
	/// <param name="pairs">The pairs it holds.</param>
	private sealed class CloneablePairs(List<KeyValuePair<int, int>> pairs) : IEnumerable<KeyValuePair<int, int>>, IDeepCloneable
	{
		public object DeepClone() => new CloneablePairs([.. pairs]);

		public IEnumerator<KeyValuePair<int, int>> GetEnumerator() => pairs.GetEnumerator();

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Tests that a dictionary still binds to the dictionary overloads rather than the pair overloads, so it keeps its
	/// type and comparer.
	/// </summary>
	[TestMethod]
	public void Dictionary_StillBindsToDictionaryOverloads()
	{
		Dictionary<string, int> source = new(StringComparer.OrdinalIgnoreCase) { ["a"] = 1 };
		Dictionary<string, int> dest = new(StringComparer.OrdinalIgnoreCase);

		Dictionary<string, int> clone = source.DeepClone();
		dest.DeepCloneFrom(source);

		Assert.AreSame(StringComparer.OrdinalIgnoreCase, clone.Comparer);
		Assert.AreEqual(1, dest["A"]);
	}
}
