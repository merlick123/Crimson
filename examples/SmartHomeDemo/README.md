# SmartHomeDemo

SmartHomeDemo is the main end-to-end Crimson example.

If you are reading the repository for the first time, start with the .NET frontend. It exercises the full contract and regeneration flow while using only the .NET toolchain already required by the repository.

It demonstrates:

- shared device, registry, automation, and scene contracts
- multiple concrete devices defined in focused `.idl` files
- one shared `contracts/**/*.idl` tree reused by the .NET, C++, and Rust frontends
- user-owned behavior under `src/User`, `cpp/user`, and `rust/src/user`
- automatic `crimson build` integration from the MSBuild, CMake, and Cargo frontends

Project files in this example root:

- `SmartHome.crimsonproj` for the .NET frontend
- `SmartHome.Cpp.crimsonproj` for the C++ frontend
- `SmartHome.Rust.crimsonproj` for the Rust frontend

Run the .NET frontend from the repo root:

```bash
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```

That is the recommended first run.

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

All three frontends exercise the same shared scenario. The demo will:

1. run `crimson build` automatically
2. regenerate the generated projection for the selected frontend if the IDL changed
3. query and compose devices through the shared runtime and capability contracts
4. treat different implementations consistently when they satisfy the same interfaces
5. apply automation and scene flows across the same contract model

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
