# Crimson IDL Reference

Crimson IDL describes contracts that lower into generated C# and C++ code.

## Declarations

Crimson supports five top-level declaration kinds:

```idl
namespace Demo.Contracts {
    const int32 Answer = 42;

    enum Mode {
        Idle,
        Active,
    }

    struct SensorSnapshot {
        float64 dry_bulb_c;
    }

    abstract interface Device {
        readonly string device_id;
        string describe();
    }

    interface Controller {
        Mode mode = Idle;
        void reset();
    }
}
```

### `namespace`

Namespaces can span multiple files.

```idl
namespace Demo.Contracts {
    interface Device;
}
```

### `interface`

Concrete interfaces generate:

- an interface projection
- a generated implementation base
- a user-owned partial/user class stub

Abstract interfaces generate only the interface projection.

```idl
interface Controller {
    string name;
    void reset();
}
```

```idl
abstract interface Device {
    readonly string device_id;
    string describe();
}
```

Interfaces can inherit from one or more base interfaces:

```idl
interface Doorbell : Device, Camera {
    void ring();
}
```

### `struct`

Structs are value-only declarations.

Use them for DTO-style shapes such as:

- snapshots
- commands
- configuration objects
- telemetry records

Example:

```idl
struct SensorSnapshot {
    float64 dry_bulb_c;
    float64 wet_bulb_c;
    bool door_closed = true;
}
```

Rules:

- structs contain value members and constant members
- structs do not support methods
- structs do not support inheritance
- structs lower as concrete value types in code generators

### `enum`

Enums can be declared without associated values:

```idl
enum Mode {
    Idle,
    Active,
}
```

Crimson also supports enums with explicit associated values in the language model:

```idl
enum HttpStatus : int32 {
    Ok = 200,
    NotFound = 404,
}
```

Current target limitation:

- the C++ target still rejects enums with associated values

### `const`

Constants must declare a value.

```idl
const int32 DefaultTimeoutMs = 5000;
```

## Members

Interfaces can contain:

- value members
- method members
- constant members
- nested interfaces
- nested enums

### Value members

```idl
interface Config {
    string name;
    readonly int32 retry_count = 3;
}
```

### Method members

```idl
interface Controller {
    void reset();
    string describe_state();
    bool try_start(string profile_name, int32 timeout_ms = 1000);
}
```

### Constant members

```idl
interface Limits {
    const int32 MaxZones = 8;
}
```

## Types

Primitive types:

- `bool`
- `string`
- `int8`, `uint8`
- `int16`, `uint16`
- `int32`, `uint32`
- `int64`, `uint64`
- `float32`, `float64`

Collection types:

- `list<T>`
- `set<T>`
- `map<TKey, TValue>`
- `T[]`
- `T[Length]`

Nullability:

```idl
string? display_name;
list<string?>? aliases;
```

Named types can be referenced relatively or globally:

```idl
Common.Device local_device;
.Vendor.Platform.Device global_device;
```

## Default Values

Value members, parameters, constants, and enum associated values can declare constant expressions.

Supported literal defaults:

- integers
- floating-point values
- strings
- booleans

Enum-typed declarations can also use enum members as defaults:

```idl
enum OvenPhase {
    Idle,
    Bake,
}

interface SteamBakeController {
    OvenPhase phase = Idle;
    OvenPhase target_phase = OvenPhase.Bake;
}
```

Shorthand enum member references resolve against the declared enum type. Qualified references are also allowed.

## Value Types

Use `struct` for value-only types:

```idl
namespace Demo {
    struct SensorSnapshot {
        string label;
        float64 temperature_c;
    }

    interface Controller {
        SensorSnapshot latest;
        list<SensorSnapshot> history;
    }
}
```

## Annotations

Crimson supports annotations on declarations and parameters:

```idl
@audit
struct Snapshot {
    string label;
}
```

```idl
interface Controller {
    void start(@audit string profile_name);
}
```

Annotations are preserved in the semantic model for generators and tooling.

## C++ Target Notes

The C++ target now emits through a generated support header:

- `Crimson/Cpp/Support.g.hpp`

That header centralizes:

- string aliases
- optional/container aliases
- interface handle aliases

The `cpp` target supports a generic ownership option:

```json
{
  "targets": {
    "cpp": {
      "output": "cpp",
      "interfaceHandleStyle": "shared_ptr"
    }
  }
}
```

Supported `interfaceHandleStyle` values:

- `shared_ptr`
- `raw_ptr`

This is intended as a generic codegen policy knob rather than a platform-specific feature.

## Diagnostics

Common validation failures include:

- duplicate declarations
- unresolved named types
- interface inheritance cycles
- missing constant values
- incompatible defaults
- invalid struct member modifiers

## Current Limitations

- the C++ target does not yet support enums with associated values
