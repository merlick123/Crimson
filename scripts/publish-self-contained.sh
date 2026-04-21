#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "usage: $0 <runtime-id> [output-dir]" >&2
  echo "example: $0 linux-x64" >&2
  exit 1
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RID="$1"
OUTPUT_DIR="${2:-$ROOT_DIR/.artifacts/crimson-$RID}"

dotnet publish "$ROOT_DIR/src/Crimson.Cli/Crimson.Cli.csproj" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o "$OUTPUT_DIR"

echo "Published self-contained Crimson build to: $OUTPUT_DIR"
