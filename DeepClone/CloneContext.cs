// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.DeepClone;

using System.Runtime.CompilerServices;

/// <summary>
/// Tracks the objects already cloned during one top-level clone operation, so that an object reached
/// twice in the graph is cloned once, and a cycle ends at the clone already being built.
/// </summary>
/// <remarks>
/// The first clone call on a thread opens the context and the matching <see cref="Scope.Dispose"/>
/// closes it, clearing every mapping. Nested calls reuse the open context. Two separate top-level
/// calls therefore never share a clone, and nothing is kept alive once the outermost call returns
/// (ktsu-dev/DeepClone#89).
/// </remarks>
internal static class CloneContext
{
	[ThreadStatic]
	private static Dictionary<object, object>? clones;

	[ThreadStatic]
	private static int depth;

	/// <summary>
	/// Opens the clone context, or joins the one already open on this thread.
	/// </summary>
	/// <returns>A scope that leaves the context when disposed.</returns>
	internal static Scope Enter()
	{
		if (depth == 0)
		{
			clones ??= new Dictionary<object, object>(ReferenceComparer.Instance);
		}

		depth++;
		return default;
	}

	/// <summary>
	/// Gets the clone already made of <paramref name="source"/> in the open context.
	/// </summary>
	/// <param name="source">The source object.</param>
	/// <param name="clone">The clone made of it, if there is one.</param>
	/// <returns><see langword="true"/> if <paramref name="source"/> has already been cloned.</returns>
	internal static bool TryGetClone(object source, out object clone)
	{
		if (depth > 0 && clones!.TryGetValue(source, out object? existing))
		{
			clone = existing;
			return true;
		}

		clone = null!;
		return false;
	}

	/// <summary>
	/// Records <paramref name="clone"/> as the clone of <paramref name="source"/> in the open context.
	/// </summary>
	/// <param name="source">The source object.</param>
	/// <param name="clone">Its clone.</param>
	internal static void Register(object source, object clone)
	{
		if (depth > 0)
		{
			clones![source] = clone;
		}
	}

	/// <summary>
	/// Leaves the clone context. Leaving the outermost scope forgets every clone it made.
	/// </summary>
	private static void Exit()
	{
		depth--;
		if (depth == 0)
		{
			clones!.Clear();
		}
	}

	/// <summary>
	/// Leaves the clone context when disposed.
	/// </summary>
	internal readonly struct Scope : IDisposable
	{
		/// <inheritdoc />
		public void Dispose() => Exit();
	}

	/// <summary>
	/// Compares objects by reference, so that a type overriding <see cref="object.Equals(object)"/>
	/// cannot make two distinct sources share one clone.
	/// </summary>
	private sealed class ReferenceComparer : IEqualityComparer<object>
	{
		internal static ReferenceComparer Instance { get; } = new();

		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

		public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
