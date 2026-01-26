# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ktsu.DeepClone** is a lightweight .NET library providing a generic interface for implementing deep cloning functionality. It uses the Curiously Recurring Template Pattern (CRTP) for type-safe cloning in inheritance hierarchies.

The library is part of the ktsu.dev ecosystem and uses the custom `ktsu.Sdk` for standardized build configuration.

## Build and Test Commands

```bash
# Build the project
dotnet build

# Run all tests
dotnet test

# Run a specific test by filter
dotnet test --filter "FullyQualifiedName~DeepCloneTests.SimpleObject_DeepClone_ShouldCreateIndependentCopy"

# Build for release
dotnet build --configuration Release

# Create NuGet package
dotnet pack --configuration Release --output ./staging
```

## Architecture

### Core Components

- **`IDeepCloneable`** (`DeepClone/IDeepCloneable.cs`) - Non-generic interface defining `object DeepClone()` for polymorphic cloning
- **`DeepCloneable<TDerived>`** (`DeepClone/DeepCloneable.cs`) - Abstract base class implementing CRTP pattern with:
  - `TDerived DeepClone()` - Public method returning strongly-typed clone
  - `CreateInstance()` - Abstract factory method for derived types to implement
  - `DeepClone(TDerived clone)` - Virtual method for copying properties
- **`DeepCloneContainerExtensions`** (`DeepClone/DeepCloneContainerExtensions.cs`) - Extension methods for cloning collections (List, Dictionary, HashSet, Stack, etc.)

### Implementation Pattern

Classes inherit from `DeepCloneable<T>` using CRTP and implement two methods:

```csharp
public class MyClass : DeepCloneable<MyClass>
{
    protected override MyClass CreateInstance() => new MyClass();

    protected override void DeepClone(MyClass clone)
    {
        base.DeepClone(clone);  // Always call base first in derived classes
        clone.Property = Property;
        clone.NestedObject = NestedObject?.DeepClone();
        clone.Collection.DeepCloneFrom(Collection);  // Use extension for collections
    }
}
```

### Key Design Decisions

- **CRTP Pattern**: Enables type-safe cloning without casting in inheritance hierarchies
- **Factory Method**: `CreateInstance()` separates object creation from property copying
- **Extension Methods**: `DeepCloneFrom()` handles in-place collection cloning; `DeepClone()` returns new collections
- **Polymorphic Support**: Non-generic `IDeepCloneable` interface enables cloning through base type references

## Project Structure

```
DeepClone/
├── DeepClone/                 # Main library
│   ├── IDeepCloneable.cs
│   ├── DeepCloneable.cs
│   └── DeepCloneContainerExtensions.cs
└── DeepClone.Test/            # MSTest test project
    ├── DeepCloneTests.cs
    ├── HierarchicalDeepCloneTests.cs
    ├── ImmutableCollectionTests.cs
    ├── SpecializedCollectionTests.cs
    ├── ThreadSafetyTests.cs
    └── Mocks/                 # Test helper classes
```

## Testing

Tests use MSTest.Sdk. Test categories:
- **DeepCloneTests** - Core functionality (simple objects, nested objects, polymorphism, circular references)
- **HierarchicalDeepCloneTests** - Inheritance hierarchy cloning
- **ImmutableCollectionTests** - ImmutableArray, ImmutableList, ImmutableDictionary support
- **SpecializedCollectionTests** - HashSet, SortedSet, Stack, Queue, LinkedList, ConcurrentDictionary
- **ThreadSafetyTests** - Multi-threaded cloning scenarios

## Multi-targeting

The library targets: `net10.0`, `net9.0`, `net8.0`, `net7.0`, `net6.0`, `net5.0`, `netstandard2.1`, `netstandard2.0`

Tests target only `net10.0`.
