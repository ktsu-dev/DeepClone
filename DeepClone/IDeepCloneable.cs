// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
	/// This non-generic method should typically delegate to a generic implementation.
	/// For example: <c>public object DeepClone() => DeepClone();</c> where the other
	/// DeepClone() method is the strongly-typed version.
	/// </remarks>
	public object DeepClone();
}
