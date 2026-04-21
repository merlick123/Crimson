#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_DIR="${1:-$ROOT_DIR/.artifacts/crimson}"

dotnet publish "$ROOT_DIR/src/Crimson.Cli/Crimson.Cli.csproj" \
  -c Release \
  -o "$OUTPUT_DIR"

echo "Published Crimson to: $OUTPUT_DIR"
