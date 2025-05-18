# ktsu.DeepClone

> A lightweight .NET library providing a simple, generic interface for implementing deep cloning functionality in your classes.

[![License](https://img.shields.io/github/license/ktsu-dev/DeepClone)](https://github.com/ktsu-dev/DeepClone/blob/main/LICENSE.md)
[![NuGet](https://img.shields.io/nuget/v/ktsu.DeepClone.svg)](https://www.nuget.org/packages/ktsu.DeepClone/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.DeepClone.svg)](https://www.nuget.org/packages/ktsu.DeepClone/)
[![Build Status](https://github.com/ktsu-dev/DeepClone/workflows/build/badge.svg)](https://github.com/ktsu-dev/DeepClone/actions)
[![GitHub Stars](https://img.shields.io/github/stars/ktsu-dev/DeepClone?style=social)](https://github.com/ktsu-dev/DeepClone/stargazers)

## Introduction

The `ktsu.DeepClone` library defines the `IDeepCloneable` interface, which allows you to create deep copies of objects. The `DeepCloneable<TDerived>` base class implements this interface while providing type-safe cloning. This is particularly useful in scenarios where you need to duplicate an object while ensuring that its references to other objects are also fully cloned, not just copied.

Inspired by and based on the [ppy/osu!](https://github.com/ppy/osu) project's cloning utilities, this library is licensed under the MIT License.

## Features

- **Generic and Non-Generic Interfaces**: Works with any reference type (`class`)
- **Deep Cloning**: Ensures that the cloned object is a completely independent copy, including all nested references
- **Inheritance Support**: Easily implement deep cloning in class hierarchies
- **Circular References**: Handles circular object references correctly without stack overflows or infinite loops
- **Collection Extensions**: Deep clone collections including:
  - Standard collections (List, Dictionary, Array)
  - Specialized collections (HashSet, SortedSet, Stack, Queue, LinkedList)
  - Concurrent collections (ConcurrentDictionary)
  - Immutable collections (ImmutableArray, ImmutableList, ImmutableDictionary)
- **Thread Safety**: Safe to use in multi-threaded environments
- **Performance**: Designed for efficiency with minimal overhead
- **Lightweight**: Minimal dependencies, focused on doing one thing well
- **Simple Integration**: Easy to implement in your own classes
- **Comprehensive Testing**: Extensive test coverage for reliability

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.DeepClone
```

### .NET CLI

```bash
dotnet add package ktsu.DeepClone
```

### Package Reference

```xml
<PackageReference Include="ktsu.DeepClone" Version="x.y.z" />
```

## Usage Examples

### Basic Example

```csharp
using ktsu.DeepClone;

public class MyClass : DeepCloneable<MyClass>
{
    public int Value { get; set; }
    public MyClass? NestedObject { get; set; }

    protected override MyClass CreateInstance() => new MyClass();

    protected override void DeepClone(MyClass clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        base.DeepClone(clone);
        
        clone.Value = this.Value;
        clone.NestedObject = this.NestedObject?.DeepClone();
    }
}
```

You can then create deep copies of your objects:

```csharp
var original = new MyClass
{
    Value = 42,
    NestedObject = new MyClass { Value = 84 }
};

var copy = original.DeepClone();

// The copy is a completely independent object
copy.Value = 100;
copy.NestedObject.Value = 200;

// Original remains unchanged
Console.WriteLine(original.Value); // Outputs: 42
Console.WriteLine(original.NestedObject.Value); // Outputs: 84
```

### Understanding the Interface Structure

The library provides a non-generic interface for deep cloning:

```csharp
// Non-generic interface - Allows polymorphic cloning that returns object
public interface IDeepCloneable
{
    object DeepClone();
}
```

The `DeepCloneable<TDerived>` base class implements this interface while providing type-safe cloning through its generic implementation. This allows classes that inherit from it to have strongly-typed `DeepClone()` methods that return their specific type, while still supporting polymorphic cloning through the `IDeepCloneable` interface.

### Using the DeepCloneable Base Class

For simpler implementation, you can use the provided `DeepCloneable<TDerived>` abstract class which implements the non-generic `IDeepCloneable` interface:

```csharp
public class Person : DeepCloneable<Person>
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public List<string> Interests { get; set; } = new();
    
    // Required: Create a new instance
    protected override Person CreateInstance() => new Person();
    
    // Required: Copy your properties
    protected override void DeepClone(Person clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        clone.Name = Name;
        clone.Age = Age;
        
        // For collections, use the DeepCloneFrom extension method
        // This handles cloning of all elements in the collection
        clone.Interests.DeepCloneFrom(Interests);
    }
}
```

### Understanding the Type-Safe CRTP Pattern

The library uses a C# implementation of the Curiously Recurring Template Pattern (CRTP) to provide type safety in inheritance hierarchies. This pattern allows derived classes to receive their actual concrete type in the `DeepClone` method without any casting.

```csharp
// The generic type parameter TDerived is the derived class itself
public abstract class DeepCloneable<TDerived> : IDeepCloneable
    where TDerived : DeepCloneable<TDerived>
{
    // Public method that creates and returns a strongly typed clone
    public TDerived DeepClone()
    {
        // Create a new instance of the derived type using an abstract factory method
        TDerived clone = CreateInstance();
        
        // Call protected method to copy properties
        DeepClone(clone);
        
        return clone;
    }
    
    // Implementation of the non-generic interface
    object IDeepCloneable.DeepClone() => DeepClone();

    // To be implemented by derived classes to create instances of themselves
    protected abstract TDerived CreateInstance();

    // To be implemented by derived classes to copy properties
    protected virtual void DeepClone(TDerived clone) { }
}
```

With this pattern:
1. The `DeepClone()` method returns the exact type of the derived class
2. The `DeepClone(TDerived clone)` method receives a parameter of the derived type, not the base type
3. No casting is required in the derived class's implementation

This ensures complete type safety throughout the inheritance chain.

### Inheritance and Deep Cloning

When implementing deep cloning in a class hierarchy, it's critical to call `base.DeepClone()` first in derived classes:

```csharp
// Base class
public abstract class Shape<T> : DeepCloneable<T> where T : Shape<T>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<string> Tags { get; set; } = new();
    
    protected override void DeepClone(T clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        // Clone base class properties
        clone.Id = Id;
        clone.Name = Name;
        
        // Use DeepCloneFrom for collections
        clone.Tags.DeepCloneFrom(Tags);
    }
}

// Derived class
public class Circle : Shape<Circle>
{
    public double Radius { get; set; }
    public Dictionary<string, double> Measurements { get; set; } = new();
    
    protected override Circle CreateInstance() => new Circle();
    
    protected override void DeepClone(Circle clone)
    {
        // IMPORTANT: Always call base.DeepClone first to clone base properties
        base.DeepClone(clone);
        
        // Then clone properties specific to this type
        clone.Radius = Radius;
        
        // Use DeepCloneFrom for collections
        clone.Measurements.DeepCloneFrom(Measurements);
    }
}

// Further derived class
public class ColoredCircle : Circle
{
    public string? Color { get; set; }
    public HashSet<string> ColorGradients { get; set; } = new();
    
    protected override ColoredCircle CreateInstance() => new ColoredCircle();
    
    protected override void DeepClone(ColoredCircle clone)
    {
        // Call base first to handle Circle and Shape properties
        base.DeepClone(clone);
        
        // Then clone ColoredCircle specific properties
        clone.Color = Color;
        
        // Use DeepCloneFrom for collections
        clone.ColorGradients.DeepCloneFrom(ColorGradients);
    }
}
```

Note that each derived class's `DeepClone()` method receives a parameter of its own type:
- `Shape<T>.DeepClone(T clone)` receives a `T`
- `Circle.DeepClone(Circle clone)` receives a `Circle`
- `ColoredCircle.DeepClone(ColoredCircle clone)` receives a `ColoredCircle`

This is achieved through the CRTP pattern and means that no casting is needed in any of these methods.

Usage:

```csharp
var original = new ColoredCircle
{
    Id = 1,
    Name = "My Circle",
    Tags = { "round", "geometric" },
    Radius = 10.5,
    Measurements = { ["diameter"] = 21.0 },
    Color = "Blue",
    ColorGradients = { "lightblue", "darkblue" }
};

var clone = original.DeepClone();

// All properties from the entire hierarchy are cloned
Console.WriteLine(clone.Id);      // 1
Console.WriteLine(clone.Name);    // My Circle
Console.WriteLine(clone.Tags[0]); // round
Console.WriteLine(clone.Radius);  // 10.5
Console.WriteLine(clone.Color);   // Blue
```

### Polymorphic Deep Cloning with Common Ancestors

When working with inheritance hierarchies that have multiple derived types from a common ancestor, you need to carefully implement deep cloning to maintain polymorphism. This is especially important when dealing with collections of different derived types.

The recommended approach is to use the CRTP pattern throughout the inheritance hierarchy along with interface hierarchies for polymorphism:

```csharp
// Non-generic interface for all animals to enable polymorphism
public interface IAnimal
{
    string? Name { get; set; }
    int Age { get; set; }
}

// Base class in the hierarchy using CRTP pattern
public abstract class Animal<TDerived> : DeepCloneable<TDerived>, IAnimal
    where TDerived : Animal<TDerived>
{
    public string? Name { get; set; }
    public int Age { get; set; }

    protected override void DeepClone(TDerived clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        base.DeepClone(clone);
        clone.Name = Name;
        clone.Age = Age;
    }
}

// Interface for mammals, extending the animal interface
public interface IMammal : IAnimal
{
    int NumberOfLegs { get; set; }
    string? FurColor { get; set; }
}

// Intermediate class for mammals, continuing the CRTP pattern
public abstract class Mammal<TDerived> : Animal<TDerived>, IMammal
    where TDerived : Mammal<TDerived>
{
    public int NumberOfLegs { get; set; }
    public string? FurColor { get; set; }

    protected override void DeepClone(TDerived clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        base.DeepClone(clone);
        clone.NumberOfLegs = NumberOfLegs;
        clone.FurColor = FurColor;
    }
}

// Interface for dogs
public interface IDog : IMammal
{
    string? Breed { get; set; }
    bool IsTrained { get; set; }
}

// Concrete implementation - Dog
public class Dog : Mammal<Dog>, IDog
{
    public string? Breed { get; set; }
    public bool IsTrained { get; set; }

    protected override Dog CreateInstance() => new Dog();

    protected override void DeepClone(Dog clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        base.DeepClone(clone);
        clone.Breed = Breed;
        clone.IsTrained = IsTrained;
    }
}

// Interface for cats
public interface ICat : IMammal
{
    bool IsIndoor { get; set; }
    string? FavoriteToy { get; set; }
}

// Another concrete implementation - Cat
public class Cat : Mammal<Cat>, ICat
{
    public bool IsIndoor { get; set; }
    public string? FavoriteToy { get; set; }

    protected override Cat CreateInstance() => new Cat();

    protected override void DeepClone(Cat clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        base.DeepClone(clone);
        clone.IsIndoor = IsIndoor;
        clone.FavoriteToy = FavoriteToy;
    }
}

// Container for a collection of polymorphic animals using the common interface
public class AnimalShelter : DeepCloneable<AnimalShelter>
{
    // Collection of animals using the common interface for polymorphism
    public Collection<IAnimal> Animals { get; } = [];

    protected override AnimalShelter CreateInstance() => new AnimalShelter();

    protected override void DeepClone(AnimalShelter clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        base.DeepClone(clone);
        clone.Animals.DeepCloneFrom(Animals);
    }
}
```

With this approach:
1. Each class in the hierarchy uses CRTP for type safety
2. Each level has a corresponding interface (IAnimal, IMammal, IDog) for polymorphism
3. No casting is needed in the DeepClone methods because of CRTP
4. The AnimalShelter uses Collection<IAnimal> for polymorphic access to any animal type

Usage example with different animal types:

```csharp
// Create animals
var dog = new Dog 
{ 
    Name = "Buddy", 
    Age = 3, 
    NumberOfLegs = 4, 
    FurColor = "Golden", 
    Breed = "Labrador", 
    IsTrained = true 
};

var cat = new Cat 
{ 
    Name = "Whiskers", 
    Age = 2, 
    NumberOfLegs = 4, 
    FurColor = "Tabby", 
    IsIndoor = true, 
    FavoriteToy = "Mouse" 
};

// Create the shelter and add animals through the common interface
var shelter = new AnimalShelter();
shelter.Animals.Add(dog);   // Adding a Dog as IAnimal
shelter.Animals.Add(cat);   // Adding a Cat as IAnimal

// Deep clone the shelter and all its animals
var clonedShelter = shelter.DeepClone();

// The cloned animals maintain their concrete types through the interfaces
var clonedDog = clonedShelter.Animals[0] as Dog;  // Type checking with 'as'
var clonedCat = clonedShelter.Animals[1] as Cat;

// Access specific derived type properties
Console.WriteLine(clonedDog?.Breed);        // "Labrador"
Console.WriteLine(clonedCat?.FavoriteToy);  // "Mouse"

// Modifying the clone doesn't affect the original
clonedDog!.Breed = "Golden Retriever";
clonedCat!.FavoriteToy = "Ball";

Console.WriteLine(dog.Breed);        // Still "Labrador"
Console.WriteLine(cat.FavoriteToy);  // Still "Mouse"
```

### Working with Immutable Collections

Immutable collections require special handling. The library provides extension methods for common immutable collection types:

```csharp
public class DocumentWithImmutables : DeepCloneable<DocumentWithImmutables>
{
    public string? Title { get; set; }
    public ImmutableArray<string> Keywords { get; set; }
    public ImmutableList<Reference> References { get; set; }
    public ImmutableDictionary<string, Author> Authors { get; set; }
    
    protected override DocumentWithImmutables CreateInstance() => new DocumentWithImmutables();
    
    protected override void DeepClone(DocumentWithImmutables clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        clone.Title = Title;
        
        // For immutable collections of value types, direct assignment is fine
        clone.Keywords = Keywords;
        
        // For immutable collections of reference types, use DeepClone extension method
        clone.References = References.DeepClone();
        clone.Authors = Authors.DeepClone();
    }
}

public class Reference : DeepCloneable<Reference>
{
    public string? Citation { get; set; }
    public int Year { get; set; }
    
    protected override Reference CreateInstance() => new Reference();
    
    protected override void DeepClone(Reference clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        clone.Citation = Citation;
        clone.Year = Year;
    }
}

public class Author : DeepCloneable<Author>
{
    public string? Name { get; set; }
    
    protected override Author CreateInstance() => new Author();
    
    protected override void DeepClone(Author clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        clone.Name = Name;
    }
}
```

### Advanced Usage with Collections

```csharp
public class ComplexObject : DeepCloneable<ComplexObject>
{
    public string? Name { get; set; }
    public List<ItemObject> Items { get; set; } = new();
    public Dictionary<string, DataObject> DataMapping { get; set; } = new();
    public HashSet<int> UniqueIds { get; set; } = new();

    protected override ComplexObject CreateInstance() => new ComplexObject();
    
    protected override void DeepClone(ComplexObject clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        clone.Name = Name;

        // Use DeepCloneFrom for all collections - this handles cloning correctly
        clone.Items.DeepCloneFrom(Items);
        clone.DataMapping.DeepCloneFrom(DataMapping);
        
        // Even for value type collections, DeepCloneFrom can be used for consistency
        // This is equivalent to copying each element manually
        clone.UniqueIds.DeepCloneFrom(UniqueIds);
    }
}

public class ItemObject : DeepCloneable<ItemObject>
{
    public int Id { get; set; }
    public string? Description { get; set; }

    protected override ItemObject CreateInstance() => new ItemObject();
    
    protected override void DeepClone(ItemObject clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        clone.Id = Id;
        clone.Description = Description;
    }
}

public class DataObject : DeepCloneable<DataObject>
{
    public byte[]? Data { get; set; }

    protected override DataObject CreateInstance() => new DataObject();
    
    protected override void DeepClone(DataObject clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        // For arrays, create a new copy
        clone.Data = Data?.ToArray();
    }
}
```

## API Reference

### `IDeepCloneable` Interface

The non-generic interface for implementing deep cloning functionality.

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `DeepClone()` | `object` | Creates a deep copy of the object, returned as an object that can be cast to the appropriate type |

### `DeepCloneable<TDerived>` Abstract Class

Base class that simplifies implementing deep cloning functionality and implements `IDeepCloneable` while providing strongly-typed cloning.

#### Methods to Implement

| Name | Return Type | Description |
|------|-------------|-------------|
| `CreateInstance()` | `TDerived` | Creates a new instance of the derived type |
| `DeepClone(TDerived clone)` | `void` | Copies all properties and fields from the current instance to the clone |

### Container Extension Methods

Extension methods for deep cloning various container types.

| Name | Container Type | Description |
|------|---------------|-------------|
| `DeepClone<T>` | `IEnumerable<T?>?` | Creates a deep clone of an enumerable collection |
| `DeepClone<TKey, TValue>` | `IDictionary<TKey, TValue?>?` | Creates a deep clone of a dictionary |
| `DeepClone<TKey, TValue>` | `IReadOnlyDictionary<TKey, TValue?>?` | Creates a deep clone of a read-only dictionary |
| `DeepClone<T>` | `HashSet<T?>?` | Creates a deep clone of a hash set, preserving the comparer |
| `DeepClone<T>` | `SortedSet<T?>?` | Creates a deep clone of a sorted set, preserving the comparer |
| `DeepClone<T>` | `Stack<T?>?` | Creates a deep clone of a stack, preserving the element order |
| `DeepClone<T>` | `ImmutableArray<T>` | Creates a deep clone of an immutable array |
| `DeepClone<T>` | `ImmutableList<T>` | Creates a deep clone of an immutable list |
| `DeepClone<TKey, TValue>` | `ImmutableDictionary<TKey, TValue>` | Creates a deep clone of an immutable dictionary |
| `DeepClone<TKey, TValue>` | `ImmutableSortedDictionary<TKey, TValue>` | Creates a deep clone of an immutable sorted dictionary |
| `DeepClone<T>` | `ImmutableHashSet<T>` | Creates a deep clone of an immutable hash set |
| `DeepClone<T>` | `ImmutableSortedSet<T>` | Creates a deep clone of an immutable sorted set |
| `DeepClone<T>` | `ImmutableStack<T>` | Creates a deep clone of an immutable stack |
| `DeepClone<T>` | `ImmutableQueue<T>` | Creates a deep clone of an immutable queue |

### DeepCloneFrom Extension Methods

Convenience methods for cloning elements directly into an existing collection.

| Name | Collection Types | Description |
|------|-----------------|-------------|
| `DeepCloneFrom<T>` | `ICollection<T>` | Clones all elements from source collection into the target collection |
| `DeepCloneFrom<TKey, TValue>` | `IDictionary<TKey, TValue>` | Clones all key-value pairs from source dictionary into the target dictionary |
| `DeepCloneFrom<T>` | `ISet<T>` | Clones all elements from source set into the target set |

Usage Example:

```csharp
// For collections of IDeepCloneable objects
targetList.DeepCloneFrom(sourceList);
targetDictionary.DeepCloneFrom(sourceDictionary);
targetSet.DeepCloneFrom(sourceSet);

// This is equivalent to (but more concise than):
foreach (var item in sourceList)
{
    targetList.Add(item.DeepClone());
}
```

## Importance of base.DeepClone in Inheritance

When implementing `DeepClone` in derived classes, it's critical to call `base.DeepClone(clone)` first to ensure proper cloning of base class properties. This pattern creates a chain of calls that ensures all properties throughout the inheritance hierarchy are properly cloned.

```csharp
protected override void DeepClone(MyDerivedClass clone)
{
    // ALWAYS call base.DeepClone first
    base.DeepClone(clone);
    
    // Then clone derived class properties
    clone.DerivedProperty1 = DerivedProperty1;
    clone.DerivedProperty2 = DerivedProperty2?.DeepClone();
    
    // Use DeepCloneFrom for collections
    clone.DerivedCollection.DeepCloneFrom(DerivedCollection);
}
```

Benefits of this approach:
- Each class is responsible only for cloning its own properties
- Changes to base class properties only need to be updated in the base class
- The entire object graph is properly cloned through the inheritance chain
- Maintainable and less error-prone as the hierarchy evolves

## Implementation Tips

When implementing deep cloning, follow these guidelines:

1. For value types and strings, a simple copy is sufficient
2. For reference types, call their `DeepClone()` method (with null check)
3. For collections, use the `DeepCloneFrom` extension methods
4. Always call `base.DeepClone(clone)` first in derived classes
5. For arrays, create a new array with the same dimensions
6. Handle circular references to avoid infinite recursion
7. Always check for null with `ArgumentNullException.ThrowIfNull` or null conditionals

## Performance Considerations

Deep cloning can be a resource-intensive operation, especially for complex object graphs. Here are some tips to optimize performance:

1. **Lazy Clone When Possible**: If a property is expensive to clone and rarely modified, consider implementing lazy cloning.
2. **Avoid Excessive Nesting**: Highly nested object structures with many references are more expensive to clone.
3. **Consider Caching**: For frequently cloned immutable objects, consider caching the cloned instances.
4. **Use ValueType Collections Wisely**: Collections of value types don't need full deep cloning.
5. **Benchmark Critical Paths**: If deep cloning is on a performance-critical path, benchmark different approaches.

Example of optimized cloning for a large object:

```csharp
public class OptimizedObject : DeepCloneable<OptimizedObject>
{
    // Expensive to clone, but rarely modified
    private ExpensiveResource? _resource;
    private ExpensiveResource? _clonedResource;
    
    public ExpensiveResource? Resource 
    { 
        get => _resource;
        set 
        {
            _resource = value;
            _clonedResource = null; // Invalidate cached clone
        }
    }

    protected override OptimizedObject CreateInstance() => new OptimizedObject();
    
    protected override void DeepClone(OptimizedObject clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        
        // Lazy clone the expensive resource only when needed
        if (_resource != null)
        {
            // Cache the cloned resource to avoid redundant deep cloning
            _clonedResource ??= _resource.DeepClone();
            clone._resource = _clonedResource;
        }
    }
}
```

## Troubleshooting Common Issues

### 1. Objects Not Fully Independent After Cloning

**Symptom**: Changes to a cloned object affect the original object.

**Solution**: Ensure all reference-type properties are being properly deep cloned. Check for:
- Missing DeepClone calls on reference-type properties
- Not using DeepCloneFrom for collections
- Forgetting to call base.DeepClone in derived classes

### 2. Stack Overflow During Cloning

**Symptom**: StackOverflowException occurs during deep cloning.

**Solution**: This typically indicates a circular reference issue:
- Implement circular reference detection in your DeepClone method
- Use a Dictionary to track already-cloned objects

```csharp
public class NodeWithCircularReferences : DeepCloneable<NodeWithCircularReferences>
{
    public string? Name { get; set; }
    public NodeWithCircularReferences? Parent { get; set; }
    public List<NodeWithCircularReferences> Children { get; set; } = new();
    
    // Dictionary for tracking already cloned instances across the object graph
    private static readonly ThreadLocal<Dictionary<NodeWithCircularReferences, NodeWithCircularReferences>> CloneTracker = 
        new(() => new Dictionary<NodeWithCircularReferences, NodeWithCircularReferences>());
    
    protected override NodeWithCircularReferences CreateInstance() => new();
    
    protected override void DeepClone(NodeWithCircularReferences clone)
    {
        ArgumentNullException.ThrowIfNull(clone);
        base.DeepClone(clone);
        
        // Ensure we have a clone tracker
        var cloneTracker = CloneTracker.Value!;
        
        // Track this instance and its clone
        cloneTracker[this] = clone;
        
        // Set simple properties
        clone.Name = Name;
        
        // Handle potential circular reference with Parent
        if (Parent != null)
        {
            // Check if parent was already cloned in this cloning operation
            if (cloneTracker.TryGetValue(Parent, out var parentClone))
                clone.Parent = parentClone;
            else
                clone.Parent = Parent.DeepClone(); // This will add to the tracker
        }
        
        // Handle collection of children that might have circular references
        foreach (var child in Children)
        {
            // Check if child was already cloned in this operation
            if (cloneTracker.TryGetValue(child, out var childClone))
                clone.Children.Add(childClone);
            else
                clone.Children.Add(child.DeepClone()); // This will add to the tracker
        }
    }
}
```

### 3. Type Mismatch in Polymorphic Collections

**Symptom**: After cloning a polymorphic collection, objects don't maintain their derived types.

**Solution**: Ensure you're implementing `IDeepCloneable` correctly for polymorphic use:
- For base classes inheriting from DeepCloneable<TDerived>, the non-generic interface is already implemented
- Override CreateInstance() to return the correct concrete type
- Use proper type checking and casting in DeepClone methods if needed
- Consider using interface hierarchies (like IAnimal, IMammal, IDog) for type-safe polymorphism

### 4. Immutable Collection Cloning Issues

**Symptom**: Immutable collections don't clone properly or reference equality is maintained.

**Solution**: Use the appropriate extension methods specific to immutable collections:
- For value-type immutable collections, direct assignment is usually sufficient
- For reference-type immutable collections, use the DeepClone extension methods
- Never manually iterate and rebuild immutable collections

## Contributing

Contributions are welcome! Here's how you can help:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please make sure to update tests as appropriate and adhere to the existing coding style.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

This library is inspired by the [ppy/osu!](https://github.com/ppy/osu) project's cloning utilities.
