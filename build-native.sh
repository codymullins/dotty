#!/usr/bin/env bash
# build-native.sh
# This script builds the native `libghostty` dependency for macOS and Linux using Zig.
# It should be run manually whenever the ghostty_src submodule is updated.

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GHOSTTY_SRC="$PROJECT_ROOT/ghostty_src"
NATIVE_DIR="$PROJECT_ROOT/Dotty/Native"

echo "Initializing submodules..."
cd "$PROJECT_ROOT"
git submodule update --init

echo "Building native ghostty library..."
cd "$GHOSTTY_SRC"
# Limit concurrency slightly to prevent system exhaustion on massive C++ deps
zig build -j4 -Dapp-runtime=none

echo "Copying native library to Dotty/Native..."
mkdir -p "$NATIVE_DIR"

if [[ "$OSTYPE" == "darwin"* ]]; then
    LIB_EXT="dylib"
else
    LIB_EXT="so"
fi

LIB_PATH="$GHOSTTY_SRC/zig-out/lib/libghostty.$LIB_EXT"

if [ -f "$LIB_PATH" ]; then
    cp "$LIB_PATH" "$NATIVE_DIR/"
    echo "Success! The native library has been built and copied."
else
    echo "Error: Failed to find libghostty.$LIB_EXT in zig-out/lib"
    exit 1
fi
