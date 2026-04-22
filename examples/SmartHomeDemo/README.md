# SmartHomeDemo

SmartHomeDemo is the canonical rich Crimson example.

It demonstrates:

- capability-first IDL contracts such as `Device`, `Camera`, `Light`, and `Thermostat`
- vendor-specific devices defined in their own `.idl` files such as `EufyDoorbell`, `RingDoorbell`, `HueBulb`, `NestThermostat`, and `SonosSpeaker`
- a `DemoHomeRuntime` contract that registers devices, queries capability groups, and connects devices into automation chains
- one shared `contracts/**/*.idl` tree lowered through multiple target-language frontends
- user-owned behavior under `src/User`, `cpp/user`, and `rust/src/user`
- automatic `crimson build` integration from .NET, CMake, and Cargo frontends

Project files in this example root:

- `SmartHome.crimsonproj` for the .NET frontend
- `SmartHome.Cpp.crimsonproj` for the C++ frontend
- `SmartHome.Rust.crimsonproj` for the Rust frontend

Run the .NET frontend from the repo root:

```bash
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```

Run the C++ frontend from the repo root:

```bash
cmake --preset gcc-debug -S examples/SmartHomeDemo -B examples/SmartHomeDemo/build/gcc-debug \
  -DCrimsonCommand=dotnet \
  -DCrimsonCommandArguments="run --project $PWD/src/Crimson.Cli/Crimson.Cli.csproj --"
cmake --build examples/SmartHomeDemo/build/gcc-debug
./examples/SmartHomeDemo/build/gcc-debug/SmartHomeCppApp
```

Run the Rust frontend from the repo root:

```bash
cargo run --manifest-path examples/SmartHomeDemo/Cargo.toml
```

All three frontends exercise the same rich scenario. The demo will:

1. run `crimson build` automatically
2. regenerate the generated projection for the selected frontend if the IDL changed
3. query devices through the shared `DemoHomeRuntime`, `Device`, and capability contracts
4. treat different vendor devices interchangeably when they support the same capabilities
5. connect devices into an automation chain across the home
6. apply a scene that flows through those connected devices

You can also run the Crimson steps directly:

```bash
crimson validate examples/SmartHomeDemo/SmartHome.crimsonproj
crimson build examples/SmartHomeDemo/SmartHome.crimsonproj
crimson validate examples/SmartHomeDemo/SmartHome.Cpp.crimsonproj
crimson build examples/SmartHomeDemo/SmartHome.Cpp.crimsonproj
crimson validate examples/SmartHomeDemo/SmartHome.Rust.crimsonproj
crimson build examples/SmartHomeDemo/SmartHome.Rust.crimsonproj
```

Committed user-owned files live under:

```txt
src/User
cpp/user
rust/src/user
```

Generated files are materialized under:

```txt
src/Generated
cpp/generated
rust/src/generated
```
