#!/bin/bash

# Navigate to the project directory
cd $(dirname "$0")

# Create releases directory if it doesn't exist
mkdir -p releases

# Build the macOS executable using PyInstaller
pyinstaller --onefile --distpath releases/ main.py

# Inform the user of completion
echo "Build complete. The executable is located in the releases directory."