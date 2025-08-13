#!/bin/bash
set -e

PROGRAM_NAME="tasky"
SOURCE_FILE="Program.cs"
PROJECT_DIR="./tasky_project"
OUTPUT_DIR="$PROJECT_DIR/publish"
INSTALL_DIR="$HOME/.local/bin"

echo "🚀 Starting installation of $PROGRAM_NAME..."

echo "📦 Installing required packages..."
sudo pacman -Sy --needed dotnet-sdk mpv

# 1. Create project if missing
if [ ! -d "$PROJECT_DIR" ]; then
    echo "🆕 Creating new .NET console project..."
    dotnet new console -n tasky_project
else
    echo "📂 Found existing project folder."
fi

# 2. Copy your Program.cs into the project
echo "📝 Copying source file into project..."
cp "$SOURCE_FILE" "$PROJECT_DIR/Program.cs"

# 3. Add Newtonsoft.Json package if not present
echo "🔍 Checking for Newtonsoft.Json package..."
cd "$PROJECT_DIR"
if ! grep -q "Newtonsoft.Json" tasky_project.csproj; then
    echo "📥 Installing Newtonsoft.Json..."
    dotnet add package Newtonsoft.Json
else
    echo "✅ Newtonsoft.Json already installed."
fi

# 4. Compile program
echo "⚙️ Building $PROGRAM_NAME..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o "$OUTPUT_DIR"

# 5. Install to ~/.local/bin
echo "📤 Installing executable..."
mkdir -p "$INSTALL_DIR"
mv "$OUTPUT_DIR/tasky_project" "$INSTALL_DIR/$PROGRAM_NAME"
chmod +x "$INSTALL_DIR/$PROGRAM_NAME"
echo "✅ Installed to $INSTALL_DIR/$PROGRAM_NAME"

# 6. Ensure ~/.local/bin in PATH
if [[ ":$PATH:" != *":$HOME/.local/bin:"* ]]; then
    echo "➕ Adding ~/.local/bin to PATH..."
    echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.bashrc"
    echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.zshrc" 2>/dev/null || true
    echo "🔄 PATH updated — restart your terminal or run: source ~/.bashrc"
fi
cd ..
echo "🎉 Installation complete! You can now run '$PROGRAM_NAME' from anywhere."
echo "🎯 Restart your terminal or run 'source ~/.bashrc' to update."