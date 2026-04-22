# CppDeviceDemo

CppDeviceDemo is a small standalone C++ starter example.

For the main multi-frontend demo, use `examples/SmartHomeDemo`.

It demonstrates:

- a host-runnable C++/CMake workflow
- generated and user-owned C++ code under `cpp/generated` and `cpp/user`
- reusable CMake integration through `.crimson/cmake/Crimson.Cpp.cmake`
- a small contract and implementation pair without the wider multi-frontend SmartHome scenario

Project layout:

- `contracts/`: Crimson IDL contracts
- `cpp/generated/`: Crimson-generated C++ headers and sources
- `cpp/user/`: merge-protected user implementation files
- `app/`: consuming C++ entry point
- `.crimson/cmake/Crimson.Cpp.cmake`: tool-owned CMake integration helper

Run it from the example directory:

```bash
cmake --preset gcc-debug \
  -DCrimsonCommand=dotnet \
  -DCrimsonCommandArguments="run --project ../../src/Crimson.Cli/Crimson.Cli.csproj --"
cmake --build --preset gcc-debug
./build/gcc-debug/CppDeviceDemoApp
```

If `crimson` is already on `PATH`, you can omit the `CrimsonCommand` overrides.
