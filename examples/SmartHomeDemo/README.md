# SmartHomeDemo

SmartHomeDemo is the main Crimson example.

It demonstrates:

- capability-first IDL contracts such as `Device`, `Camera`, `Light`, and `Thermostat`
- vendor-specific devices defined in their own `.idl` files such as `EufyDoorbell`, `RingDoorbell`, `HueBulb`, `NestThermostat`, and `SonosSpeaker`
- a `DemoHomeRuntime` contract that registers devices, queries capability groups, and connects devices into automation chains
- user-owned behavior under `src/User`
- automatic `crimson build` integration from a consuming C# project

Run it from the repo root:

```bash
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```

The demo will:

1. run `crimson build` automatically
2. regenerate the generated C# projection if the IDL changed
3. query devices through the shared `IDemoHomeRuntime`, `IDevice`, and capability contracts
4. treat different vendor devices interchangeably when they support the same capabilities
5. connect devices into an automation chain across the home
6. apply a scene that flows through those connected devices

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
