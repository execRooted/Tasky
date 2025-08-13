#!/bin/bash
set -e

PROGRAM_NAME="tasky"
INSTALL_DIR="$HOME/.local/bin"
PROJECT_DIR="./tasky_project"

echo "🗑️ Starting uninstallation of $PROGRAM_NAME..."

# 1. Remove binary
if [ -f "$INSTALL_DIR/$PROGRAM_NAME" ]; then
    rm "$INSTALL_DIR/$PROGRAM_NAME"
    echo "✅ Removed binary from $INSTALL_DIR"
else
    echo "⚠️ No binary found in $INSTALL_DIR"
fi

# 2. Remove project directory
if [ -d "$PROJECT_DIR" ]; then
    rm -rf "$PROJECT_DIR"
    echo "🧹 Removed local project folder: $PROJECT_DIR"
else
    echo "📂 No local project folder found."
fi

# 3. Remove PATH entry if no other binaries in ~/.local/bin
if [ -d "$INSTALL_DIR" ] && [ -z "$(ls -A "$INSTALL_DIR")" ]; then
    echo "❌ Removing ~/.local/bin from PATH..."
    sed -i '/export PATH="\$HOME\/.local\/bin:\$PATH"/d' "$HOME/.bashrc" 2>/dev/null || true
    sed -i '/export PATH="\$HOME\/.local\/bin:\$PATH"/d' "$HOME/.zshrc" 2>/dev/null || true
else
    echo "ℹ️ ~/.local/bin still contains other programs — keeping it in PATH."
fi



echo "🎯 Uninstallation complete! Restart your terminal or run 'source ~/.bashrc' to update."