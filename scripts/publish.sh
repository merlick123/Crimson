#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_DIR="${1:-$ROOT_DIR/.artifacts/crimson}"
BIN_DIR="$ROOT_DIR/.artifacts/bin"

dotnet publish "$ROOT_DIR/src/Crimson.Cli/Crimson.Cli.csproj" \
  -c Release \
  -o "$OUTPUT_DIR"

mkdir -p "$BIN_DIR"

if [[ -f "$OUTPUT_DIR/crimson" ]]; then
  ln -sfn "$OUTPUT_DIR/crimson" "$BIN_DIR/crimson"
  echo "Updated dev link: $BIN_DIR/crimson"
elif [[ -f "$OUTPUT_DIR/crimson.exe" ]]; then
  ln -sfn "$OUTPUT_DIR/crimson.exe" "$BIN_DIR/crimson.exe"
  echo "Updated dev link: $BIN_DIR/crimson.exe"
fi

echo "Published Crimson to: $OUTPUT_DIR"
