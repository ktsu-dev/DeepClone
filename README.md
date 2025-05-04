# ktsu.DeepClone

> A lightweight .NET library providing a simple, generic interface for implementing deep cloning functionality in your classes.

[![License](https://img.shields.io/github/license/ktsu-dev/DeepClone)](https://github.com/ktsu-dev/DeepClone/blob/main/LICENSE.md)
[![NuGet](https://img.shields.io/nuget/v/ktsu.DeepClone.svg)](https://www.nuget.org/packages/ktsu.DeepClone/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.DeepClone.svg)](https://www.nuget.org/packages/ktsu.DeepClone/)
[![Build Status](https://github.com/ktsu-dev/DeepClone/workflows/build/badge.svg)](https://github.com/ktsu-dev/DeepClone/actions)
[![GitHub Stars](https://img.shields.io/github/stars/ktsu-dev/DeepClone?style=social)](https://github.com/ktsu-dev/DeepClone/stargazers)

## Introduction

The `ktsu.DeepClone` library defines the `IDeepCloneable<T>` interface, which allows you to create deep copies of objects. This is particularly useful in scenarios where you need to duplicate an object while ensuring that its references to other objects are also fully cloned, not just copied.

Inspired by and based on the [ppy/osu!](https://github.com/ppy/osu) project's cloning utilities, this library is licensed under the MIT License.

## Features

- **Generic Interface**: Works with any reference type (`class`)
- **Deep Cloning**: Ensures that the cloned object is a completely independent copy, including all nested references
- **Lightweight**: Minimal dependencies, focused on doing one thing well
- **Simple Integration**: Easy to implement in your own classes

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

public class MyClass : IDeepCloneable<MyClass>
{
    public int Value { get; set; }
    public MyClass NestedObject { get; set; }

    public MyClass DeepClone()
    {
        return new MyClass
        {
            Value = this.Value,
            NestedObject = this.NestedObject?.DeepClone()
        };
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

### Advanced Usage with Collections

```csharp
public class ComplexObject : IDeepCloneable<ComplexObject>
{
    public string Name { get; set; }
    public List<ItemObject> Items { get; set; } = new List<ItemObject>();
    public Dictionary<string, DataObject> DataMapping { get; set; } = new Dictionary<string, DataObject>();

    public ComplexObject DeepClone()
    {
        var clone = new ComplexObject
        {
            Name = this.Name
        };

        // Deep clone list items
        foreach (var item in Items)
        {
            clone.Items.Add(item.DeepClone());
        }

        // Deep clone dictionary items
        foreach (var kvp in DataMapping)
        {
            clone.DataMapping[kvp.Key] = kvp.Value.DeepClone();
        }

        return clone;
    }
}

public class ItemObject : IDeepCloneable<ItemObject>
{
    public int Id { get; set; }
    public string Description { get; set; }

    public ItemObject DeepClone() => new ItemObject
    {
        Id = this.Id,
        Description = this.Description
    };
}

public class DataObject : IDeepCloneable<DataObject>
{
    public byte[] Data { get; set; }

    public DataObject DeepClone() => new DataObject
    {
        // For arrays, create a new copy
        Data = this.Data?.ToArray()
    };
}
```

## API Reference

### `IDeepCloneable<T>` Interface

The core interface for implementing deep cloning functionality.

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `DeepClone()` | `T` | Creates a deep copy of the object, including all nested references |

## Implementation Tips

### Cloning Different Types of Properties

When implementing `DeepClone()`, follow these guidelines for different property types:

- **Value Types**: Simple assignment is sufficient (`clone.Value = this.Value`)
- **Strings**: Simple assignment is sufficient as strings are immutable
- **Cloneable Objects**: Call `DeepClone()` on the property (`clone.Object = this.Object?.DeepClone()`)
- **Collections**: Create new collection instances and clone each item individually
- **Arrays**: Use `.ToArray()` to create a new array copy
- **Non-Cloneable Objects**: Consider implementing `IDeepCloneable` for these types if possible

### Thread Safety Considerations

The `DeepClone()` method should be implemented in a thread-safe manner to ensure it can be called from multiple threads simultaneously. Avoid using shared state or mutable static variables in your implementation.

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

This library is inspired by the [ppy/osu!](https://github.com/ppy/osu) project's cloning utilities. Many thanks to their team for their foundational work and open-source contributions.
