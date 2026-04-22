# CppDeviceDemo

CppDeviceDemo is a small standalone C++ starter example.

For the canonical rich cross-target demo, use `examples/SmartHomeDemo`.

It demonstrates:

- a host-runnable C++/CMake workflow
- generated and user-owned C++ code under `cpp/generated` and `cpp/user`
- reusable CMake integration through `.crimson/cmake/Crimson.Cpp.cmake`
- a checked-in generated projection alongside user-owned implementation stubs

Run it from the example directory:

```bash
cmake --preset gcc-debug \
  -DCrimsonCommand=dotnet \
  -DCrimsonCommandArguments="run --project ../../src/Crimson.Cli/Crimson.Cli.csproj --"
cmake --build --preset gcc-debug
./build/gcc-debug/CppDeviceDemoApp
```

If `crimson` is already on `PATH`, you can omit the `CrimsonCommand` overrides.
