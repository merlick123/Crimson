# SmartHomeDemo

SmartHomeDemo is the main Crimson example.

It demonstrates:

- generated C# interfaces and class plumbing from `.idl`
- user-owned behavior under `src/User`
- automatic `crimson build` integration from a consuming C# project
- reflection-based discovery of interchangeable `IHomeController` implementations
- querying device features and tracing automation chains across the home

Run it from the repo root:

```bash
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```

The demo will:

1. run `crimson build` automatically
2. regenerate the generated C# projection if the IDL changed
3. discover all available `IHomeController` implementations
4. run the same scenario against each implementation without the caller caring which concrete type it got

You can also run the Crimson steps directly:

```bash
crimson validate examples/SmartHomeDemo/SmartHome.crimsonproj
crimson build examples/SmartHomeDemo/SmartHome.crimsonproj
```

Committed user-owned files live under:

```txt
src/User
```

Generated files are materialized under:

```txt
src/Generated
```
