#!/bin/bash
set -e
clear

echo "🚮 === Tasky Uninstaller ==="

# Function to detect Linux family
detect_linux() {
    if command -v pacman &> /dev/null; then
        echo "arch"
    elif command -v apt &> /dev/null; then
        echo "debian"
    elif command -v dnf &> /dev/null; then
        echo "fedora"
    elif command -v zypper &> /dev/null; then
        echo "opensuse"
    else
        echo "Unsupported OS"
        exit 1
    fi
}

LINUX_FAMILY=$(detect_linux)
echo "🔎 Detected Linux family: $LINUX_FAMILY"

# Remove the Tasky executable
if [ -f /usr/local/bin/tasky ]; then
    echo "🗑️ Removing /usr/local/bin/tasky..."
    sudo rm /usr/local/bin/tasky
    echo "✅ Tasky executable removed."
else
    echo "⚠️ Tasky executable not found in /usr/local/bin."
fi

# Remove desktop entry
if [ -f /usr/share/applications/tasky.desktop ]; then
    echo "🗑️ Removing Tasky desktop entry..."
    sudo rm /usr/share/applications/tasky.desktop
    echo "✅ Desktop entry removed."
else
    echo "⚠️ No desktop entry found for Tasky."
fi

# Optionally remove .NET SDK
if command -v dotnet &> /dev/null; then
    read -p "Do you want to remove the .NET SDK? [y/N]: " REMOVE_DOTNET
    if [[ "$REMOVE_DOTNET" =~ ^[Yy]$ ]]; then
        case "$LINUX_FAMILY" in
            arch)
                sudo pacman -Rns --noconfirm dotnet-sdk
                ;;
            debian)
                sudo apt remove --purge -y dotnet-sdk-9.0
                sudo apt autoremove -y
                ;;
            fedora)
                sudo dnf remove -y dotnet-sdk-9.0
                ;;
            opensuse)
                sudo zypper remove -y dotnet-sdk-9.0
                ;;
        esac
        echo "✅ .NET SDK removed."
    else
        echo "⏭️ Skipped removing .NET SDK."
    fi
else
    echo "⚠️ .NET SDK not found, nothing to remove."
fi

echo ""
echo "🧹 === Tasky uninstallation complete ==="
