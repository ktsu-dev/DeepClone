// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DeepClone;

/// <summary>
/// Base class for objects that can be deep cloned.
/// </summary>
/// <typeparam name="TDerived">The derived type that implements this abstract class.</typeparam>
/// <remarks>
/// This abstract class simplifies implementing deep cloning by:
/// <list type="bullet">
/// <item>
/// <description>Providing a standard DeepClone method that handles the object creation</description>
/// </item>
/// <item>
/// <description>Implementing the IDeepCloneable interface automatically</description>
/// </item>
/// <item>
/// <description>Allowing derived classes to focus on just creating instances and copying fields</description>
/// </item>
/// </list>
/// 
/// To implement in a derived class:
/// <list type="number">
/// <item>
/// <description>Override the CreateInstance method to create a new instance of your class</description>
/// </item>
/// <item>
/// <description>Override the DeepClone method to copy all your properties and fields</description>
/// </item>
/// <item>
/// <description>For collections, use the DeepCloneFrom extension methods where possible</description>
/// </item>
/// </list>
/// </remarks>
public abstract class DeepCloneable<TDerived> : IDeepCloneable
	where TDerived : DeepCloneable<TDerived>
{
	/// <summary>
	/// Creates a deep clone of the current instance.
	/// </summary>
	/// <returns>A new instance that is a deep clone of the current instance.</returns>
	/// <remarks>
	/// This method handles the cloning process by first creating a new instance
	/// using <see cref="CreateInstance"/> and then copying data using <see cref="DeepClone(TDerived)"/>.
	/// </remarks>
	public TDerived DeepClone()
	{
		var clone = CreateInstance();
		DeepClone(clone);
		return clone;
	}

	/// <summary>
	/// Implements the IDeepCloneable interface.
	/// </summary>
	/// <returns>A deep clone of the current instance.</returns>
	object IDeepCloneable.DeepClone() => DeepClone();

	/// <summary>
	/// Creates a new instance of the derived type.
	/// </summary>
	/// <returns>A new instance of the derived type.</returns>
	/// <remarks>
	/// This method should be implemented to create a new, empty instance of your class.
	/// It should NOT copy any data - that's the job of <see cref="DeepClone(TDerived)"/>.
	/// </remarks>
	protected abstract TDerived CreateInstance();

	/// <summary>
	/// Performs the deep cloning operation by copying all relevant data from the current instance to the clone.
	/// </summary>
	/// <param name="clone">The instance to copy data to.</param>
	/// <remarks>
	/// This method should be overridden to copy all fields and properties.
	/// For reference type properties, call DeepClone() on them.
	/// For collections, use the DeepCloneFrom extension methods.
	/// 
	/// Example implementation:
	/// <code>
	/// protected override void DeepClone(MyClass clone)
	/// {
	///     clone.Name = Name; // Value types and strings are simply copied
	///     clone.Child = Child?.DeepClone(); // Reference types should be deep cloned
	///     clone.Items.DeepCloneFrom(Items); // Collections can use DeepCloneFrom
	/// }
	/// </code>
	/// </remarks>
	protected virtual void DeepClone(TDerived clone) { }
}
