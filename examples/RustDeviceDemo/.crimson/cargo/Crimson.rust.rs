fn main() {
    println!("cargo:rerun-if-changed=contracts");
    println!("cargo:rerun-if-changed=RustDeviceDemo.crimsonproj");

    let project_file = std::env::var("CRIMSON_PROJECT_FILE")
        .unwrap_or_else(|_| "RustDeviceDemo.crimsonproj".to_string());
    let (command, arguments) = resolve_crimson_command();

    let mut process = std::process::Command::new(&command);
    for argument in arguments {
        process.arg(argument);
    }

    let status = process
        .arg("build")
        .arg(&project_file)
        .status()
        .unwrap_or_else(|error| panic!("Failed to start Crimson build command '{}': {}", command, error));

    if !status.success() {
        panic!("Crimson build failed with status {:?}", status.code());
    }
}

fn resolve_crimson_command() -> (String, Vec<String>) {
    if let Ok(command) = std::env::var("CRIMSON_COMMAND") {
        let arguments = std::env::var("CRIMSON_COMMAND_ARGUMENTS")
            .unwrap_or_default()
            .split_whitespace()
            .filter(|value| !value.is_empty())
            .map(str::to_owned)
            .collect();
        return (command, arguments);
    }

    if let Some(cli_project) = find_local_cli_project() {
        return (
            "dotnet".to_string(),
            vec![
                "run".to_string(),
                "--project".to_string(),
                cli_project,
                "--".to_string(),
            ],
        );
    }

    ("crimson".to_string(), Vec::new())
}

fn find_local_cli_project() -> Option<String> {
    let manifest_dir = std::env::var("CARGO_MANIFEST_DIR").ok()?;
    let mut current = std::path::PathBuf::from(manifest_dir);

    loop {
        let candidate = current.join("src").join("Crimson.Cli").join("Crimson.Cli.csproj");
        if candidate.is_file() {
            return Some(candidate.to_string_lossy().into_owned());
        }

        if !current.pop() {
            return None;
        }
    }
}