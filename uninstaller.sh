#!/bin/bash

# Uninstall script for Tasky

# Application name
APP_NAME="Tasky"

# System-wide data directory
DATA_DIR="/var/lib/tasky"

# Check if running as root
if [ "$(id -u)" -ne 0 ]; then
    echo "This uninstall script requires root privileges."
    echo "Please run with sudo:"
    echo "  sudo ./uninstall-tasky.sh"
    exit 1
fi

# Confirm uninstallation
read -p "Are you sure you want to completely uninstall $APP_NAME? [y/N] " confirm
if [[ ! "$confirm" =~ ^[Yy]$ ]]; then
    echo "Uninstallation cancelled."
    exit 0
fi

# Step 1: Remove application binary
echo -n "Removing application binary... "
if rm -f "/usr/local/bin/tasky" 2>/dev/null || \
   rm -f "/usr/bin/tasky" 2>/dev/null; then
    echo "OK"
else
    echo "Not found"
fi

# Step 2: Remove system data directory
echo -n "Removing system data directory ($DATA_DIR)... "
if [ -d "$DATA_DIR" ]; then
    if rm -rf "$DATA_DIR"; then
        echo "OK"
    else
        echo "FAILED"
        echo "Warning: Could not remove $DATA_DIR"
        echo "You may need to remove it manually."
    fi
else
    echo "Not found"
fi

# Step 3: Remove desktop entry (if exists)
echo -n "Removing desktop entry... "
if rm -f "/usr/share/applications/tasky.desktop" 2>/dev/null; then
    echo "OK"
else
    echo "Not found"
fi

# Completion message
echo ""
echo "$APP_NAME has been completely uninstalled."
echo "Note: Your task data in $DATA_DIR has been removed."
exit 0
