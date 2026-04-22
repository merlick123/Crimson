namespace Crimson.Core.Projects;

public sealed class RustCargoProjectInitProfile : IProjectInitProfile
{
    public string ProfileId => "rust-cargo";

    public string DisplayName => "Rust / Cargo";

    public string Description => "Rust output with generated/user source split and Cargo integration.";

    public ProjectInitPlan CreatePlan(ProjectInitContext context)
    {
        var files = new List<ProjectInitFile>
        {
            new("Cargo.toml", RenderCargoToml(context.ProjectName, isNoStd: false)),
            new("build.rs", BuildScript),
            new("README.md", RenderReadme(context.ProjectName, isNoStd: false)),
            new(Path.Combine("src", "main.rs"), context.Starter ? StarterMain : DefaultMain),
        };

        if (context.Starter)
        {
            files.Add(new ProjectInitFile(Path.Combine("contracts", "hello.idl"), StarterIdl));
        }

        return new ProjectInitPlan(
            [
                new ProjectInitGroup(
                    "rust",
                    "rust",
                    ["contracts/**/*.idl"],
                    Array.Empty<string>(),
                    "src",
                    new
                    {
                        support = new
                        {
                            provider = "generated",
                            profile = "std",
                        },
                    },
                    new ProjectInitHost("cargo", new { }))
            ],
            files);
    }

    private static string RenderCargoToml(string projectName, bool isNoStd) => $$"""
[package]
name = "{{ToKebabCase(projectName)}}"
version = "0.1.0"
edition = "2021"
build = "build.rs"

{{(isNoStd ? "[lib]\npath = \"src/lib.rs\"\n\n" : string.Empty)}}[dependencies]
""";

    private static string RenderReadme(string projectName, bool isNoStd)
    {
        var runCommand = isNoStd ? "cargo check" : "cargo run";
        var entryPoint = isNoStd ? "`src/lib.rs`" : "`src/main.rs`";
        return $$"""
# {{projectName}}

This project uses Crimson with the `{{(isNoStd ? "rust-cargo-no-std" : "rust-cargo")}}` init profile.

Run it from this directory:

```bash
{{runCommand}}
```

The Cargo build script runs `crimson build` automatically before compile.

Override the Crimson command if you are using a local repo build:

```bash
CRIMSON_COMMAND=dotnet \
CRIMSON_COMMAND_ARGUMENTS="run --project /path/to/src/Crimson.Cli/Crimson.Cli.csproj --" \
{{runCommand}}
```

Project layout:

- `contracts/`: Crimson IDL contracts
- `src/generated/`: Crimson-generated Rust output
- `src/user/`: merge-protected user implementation stubs
- {{entryPoint}}: package entry point
- `.crimson/cargo/Crimson.rust.rs`: tool-owned Cargo build integration helper
""";
    }

    internal const string StarterIdl = """
namespace SmartHome {
    /// Simple dimmable light.
    interface LightDevice {
        /// Display name shown to users.
        string display_name;

        /// Current brightness percentage.
        int32 brightness_percent = 35;
    }
}
""";

    private const string BuildScript = """
include!(".crimson/cargo/Crimson.rust.rs");
""";

    private const string DefaultMain = """
mod generated;
mod user;

fn main() {
    println!("Crimson Rust project ready.");
}
""";

    private const string StarterMain = """
mod generated;
mod user;

use crate::generated::smart_home__light_device::LightDeviceContract;
use crate::user::smart_home__light_device::LightDevice;

fn main() {
    let mut light = LightDevice::new();
    light.set_display_name("Porch Light".into());
    light.set_brightness_percent(42);
    println!("{}: {}%", light.get_display_name(), light.get_brightness_percent());
}
""";

    private static string ToKebabCase(string value) =>
        string.Concat(value.Select((character, index) =>
            char.IsUpper(character) && index > 0
                ? "-" + char.ToLowerInvariant(character)
                : char.ToLowerInvariant(character).ToString()));
}

public sealed class RustCargoNoStdProjectInitProfile : IProjectInitProfile
{
    public string ProfileId => "rust-cargo-no-std";

    public string DisplayName => "Rust / Cargo / no_std";

    public string Description => "Rust output with Cargo integration and an alloc-based no_std support profile.";

    public ProjectInitPlan CreatePlan(ProjectInitContext context)
    {
        var files = new List<ProjectInitFile>
        {
            new("Cargo.toml", RenderCargoToml(context.ProjectName)),
            new("build.rs", BuildScript),
            new("README.md", RenderReadme(context.ProjectName)),
            new(Path.Combine("src", "lib.rs"), DefaultLibrary),
        };

        if (context.Starter)
        {
            files.Add(new ProjectInitFile(Path.Combine("contracts", "hello.idl"), RustCargoProjectInitProfile.StarterIdl));
        }

        return new ProjectInitPlan(
            [
                new ProjectInitGroup(
                    "rust",
                    "rust",
                    ["contracts/**/*.idl"],
                    Array.Empty<string>(),
                    "src",
                    new
                    {
                        support = new
                        {
                            provider = "generated",
                            profile = "no_std",
                        },
                    },
                    new ProjectInitHost("cargo", new { }))
            ],
            files);
    }

    private static string RenderCargoToml(string projectName) => $$"""
[package]
name = "{{ToKebabCase(projectName)}}"
version = "0.1.0"
edition = "2021"
build = "build.rs"

[lib]
path = "src/lib.rs"

[dependencies]
""";

    private static string RenderReadme(string projectName) => $$"""
# {{projectName}}

This project uses Crimson with the `rust-cargo-no-std` init profile.

Validate it from this directory:

```bash
cargo check
```

The Cargo build script runs `crimson build` automatically before Cargo checks the crate.

The generated support layer is alloc-based and keeps the crate in `no_std`.

Override the Crimson command if you are using a local repo build:

```bash
CRIMSON_COMMAND=dotnet \
CRIMSON_COMMAND_ARGUMENTS="run --project /path/to/src/Crimson.Cli/Crimson.Cli.csproj --" \
cargo check
```

Project layout:

- `contracts/`: Crimson IDL contracts
- `src/generated/`: Crimson-generated Rust output
- `src/user/`: merge-protected user implementation modules
- `src/lib.rs`: package entry point
- `.crimson/cargo/Crimson.rust.rs`: tool-owned Cargo build integration helper
""";

    private const string BuildScript = """
include!(".crimson/cargo/Crimson.rust.rs");
""";

    private const string DefaultLibrary = """
#![no_std]

extern crate alloc;

pub mod generated;
pub mod user;
""";

    private static string ToKebabCase(string value) =>
        string.Concat(value.Select((character, index) =>
            char.IsUpper(character) && index > 0
                ? "-" + char.ToLowerInvariant(character)
                : char.ToLowerInvariant(character).ToString()));
}
