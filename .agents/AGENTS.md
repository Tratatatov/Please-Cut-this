# Project Rules

## Service Architecture & Dependency Injection
- **Bootstrapper Initialization**: There are two main bootstrappers: `Main` and `Scene`. All services must be initialized within them. The bootstrappers should hold all references to `MonoBehaviour` components (if required).
- **Dependency Passing**: Pass all required dependencies to services during their initialization in the bootstrapper.
- **C# Classes over MonoBehaviour**: Implement services as classic C# classes instead of `MonoBehaviour` whenever possible.
- **Composition Objects**: Use objects (e.g., `Client`) for storing and composing necessary classes and services, acting as a binding/glue class.
- **Configuration**: If any part of the system needs configuration, it must be handled through a `ScriptableObject`.

- **MonoBehaviour Initialization**: Do not use `Start()` or `Awake()` for initializing `MonoBehaviour` components that hold references. Implement an explicit `Initialize()` method instead.

## Code Style
- **Explicit Access Modifiers**: Always explicitly declare `private` for private fields, properties, and methods. Do not rely on default access modifiers.
