#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BIN_DIR="$ROOT_DIR/.artifacts/bin"

cat <<EOF
export PATH="$BIN_DIR:\$PATH"
EOF
