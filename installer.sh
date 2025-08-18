#!/bin/bash

# Tasky System-Wide Installer for Arch Linux
# Version 2.0

echo "🚀 Starting Tasky system-wide installation..."

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "🔵 This installer needs root privileges for system-wide installation"
    sudo -v || (echo "❌ Failed to get root privileges"; exit 1)
fi

# Check if yay is installed (for AUR packages)
if ! command -v yay &> /dev/null; then
    echo "🔵 yay (AUR helper) not found. Installing..."
    sudo pacman -S --needed base-devel git
    git clone https://aur.archlinux.org/yay.git
    cd yay
    makepkg -si --noconfirm
    cd ..
    rm -rf yay
fi

# Check if .NET 9.0 is installed
if ! dotnet --list-sdks | grep -q '9.0'; then
    echo "🔵 .NET 9.0 not found. Installing..."
    yay -S --noconfirm dotnet-sdk-bin
    echo "✅ .NET 9.0 installed successfully"
else
    echo "🔵 .NET 9.0 is already installed"
fi

# Check if in Tasky project directory
if [ ! -f "Tasky.csproj" ]; then
    echo "🔵 Tasky project not found in current directory"
    read -p "Do you want to clone the repository? (y/n): " clone_choice
    if [ "$clone_choice" = "y" ]; then
        git clone https://github.com/your-repo/tasky.git
        cd tasky || exit
    else
        echo "❌ Please run this script in the Tasky project directory"
        exit 1
    fi
fi

# Restore NuGet packages
echo "🔵 Restoring NuGet packages..."
dotnet restore

# Build the application
echo "🔵 Building Tasky..."
dotnet build -c Release

# Create system-wide symlink
echo "🔵 Making 'tasky' command available system-wide..."
TARGET_PATH="$(pwd)/bin/Release/net9.0/Tasky.dll"

# Create a wrapper script in /usr/local/bin
sudo tee /usr/local/bin/tasky > /dev/null <<EOF
#!/bin/bash
dotnet "$TARGET_PATH" "\$@"
EOF

sudo chmod +x /usr/local/bin/tasky

# Install mpv for sound notifications if not present
if ! command -v mpv &> /dev/null; then
    echo "🔵 Installing mpv for sound notifications..."
    sudo pacman -S --noconfirm mpv
fi

# Create desktop entry (optional)
echo "🔵 Creating desktop entry..."
sudo tee /usr/share/applications/tasky.desktop > /dev/null <<EOF
[Desktop Entry]
Name=Tasky Productivity Master
Comment=Task Management Application
Exec=tasky
Icon=utilities-terminal
Terminal=true
Type=Application
Categories=Utility;Productivity;
EOF

echo ""
echo "🎉 System-wide installation complete!"
echo "You can now run Tasky from anywhere by typing:"
echo "  tasky"




echo ""






echo "Thanks for installing my program"
echo "Made by execRooted"
echo "github: github.com/execRooted/Tasky"
