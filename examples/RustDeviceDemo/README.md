# RustDeviceDemo

RustDeviceDemo is the main Crimson Rust example.

It demonstrates:

- a host-runnable Rust/Cargo workflow
- generated and user-owned Rust modules under `src/generated` and `src/user`
- contract-first value modeling through `struct` and enum defaults
- Cargo-triggered `crimson build` regeneration

Run it from the repo root:

```bash
cargo run --manifest-path examples/RustDeviceDemo/Cargo.toml
```

Or run it from the example directory:

```bash
cargo run
```
