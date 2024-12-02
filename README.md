# ktsu.DeepClone

A lightweight .NET library providing a simple, generic interface for implementing deep cloning functionality in your classes.

## Overview

The `ktsu.DeepClone` library defines the `IDeepCloneable<T>` interface, which allows you to create deep copies of objects. This is particularly useful in scenarios where you need to duplicate an object while ensuring that its references to other objects are also fully cloned, not just copied.

Inspired by and based on the [ppy/osu!](https://github.com/ppy/osu) project's cloning utilities, this library is licensed under the MIT License. See the `LICENSE` file for more details.

## Features

- **Generic Interface**: Works with any reference type (`class`).
- **Deep Cloning**: Ensures that the cloned object is a completely independent copy, including all nested references.
- Lightweight and easy to integrate into your project.

## Getting Started

### Installation

Install the library via [NuGet](https://www.nuget.org/) (coming soon):

```sh
dotnet add package ktsu.DeepClone
```

Or, clone the repository and include the source code directly in your project.

### Usage

To use `ktsu.DeepClone`, implement the `IDeepCloneable<T>` interface in your class:

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

## Contributing

Contributions are welcome! If you have suggestions or feature requests, please feel free to open an issue or submit a pull request.

### Development

1. Clone the repository:
   ```sh
   git clone https://github.com/yourusername/ktsu.DeepClone.git
   ```
2. Open the project in your preferred IDE.
3. Build and run tests to ensure functionality.

## License

This library is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

### Acknowledgments

This library is inspired by the [ppy/osu!](https://github.com/ppy/osu) project's cloning utilities. Many thanks to their team for their foundational work and open-source contributions.
