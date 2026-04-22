# RustDeviceDemo

RustDeviceDemo is a small standalone Rust starter example.

For the main multi-frontend demo, use `examples/SmartHomeDemo`.

It demonstrates:

- a host-runnable Rust/Cargo workflow
- generated and user-owned Rust modules under `src/generated` and `src/user`
- contract-first value modeling through `struct` and enum defaults
- Cargo-triggered `crimson build` regeneration

Project layout:

- `contracts/`: Crimson IDL contracts
- `src/generated/`: Crimson-generated Rust output
- `src/user/`: merge-protected user implementation modules
- `src/main.rs`: package entry point
- `.crimson/cargo/Crimson.Cargo.rs`: tool-owned Cargo build integration helper

Run it from the repo root:

```bash
cargo run --manifest-path examples/RustDeviceDemo/Cargo.toml
```

Or run it from the example directory:

```bash
cargo run
```
