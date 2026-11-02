// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone;

/// <summary>
/// Interface for objects that can be deep cloned.
/// </summary>
/// <remarks>
/// When implementing this interface:
/// <list type="bullet">
/// <item>
/// <description>Create a new instance of the same type</description>
/// </item>
/// <item>
/// <description>Copy all fields and properties to the new instance</description>
/// </item>
/// <item>
/// <description>For reference types, call DeepClone() on those objects when copying</description>
/// </item>
/// <item>
/// <description>Handle circular references carefully to avoid infinite recursion</description>
/// </item>
/// <item>
/// <description>Consider using the <see cref="DeepCloneable{TDerived}"/> base class for easier implementation</description>
/// </item>
/// </list>
/// </remarks>
public interface IDeepCloneable
{
	/// <summary>
	/// Creates a deep clone of the current instance.
	/// </summary>
	/// <returns>A new instance that is a deep clone of the current instance.</returns>
	/// <remarks>
	/// This non-generic method should typically delegate to a strongly-typed implementation.
	/// Implement it explicitly, so it does not clash with the strongly-typed method and
	/// does not call itself:
	/// <code>
	/// public Foo DeepClone() => new() { Value = Value };
	/// object IDeepCloneable.DeepClone() => DeepClone();
	/// </code>
	/// A public <c>object DeepClone()</c> cannot sit beside <c>Foo DeepClone()</c> (CS0111),
	/// and <c>public object DeepClone() => DeepClone();</c> on its own calls itself until
	/// the stack overflows.
	/// </remarks>
	public object DeepClone();
}
