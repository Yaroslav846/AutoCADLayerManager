# AutoCAD Layer Manager

A plugin for AutoCAD that provides a modern user interface for managing layers. Developed with C# and Avalonia UI, it offers a more intuitive and responsive experience compared to the default layer management tools.

One of the key features of this plugin is the **real-time, two-way synchronization** between the plugin's UI and the AutoCAD environment. Any changes made in the Layer Manager are instantly reflected in AutoCAD, and any layer modifications made directly in AutoCAD (e.g., using the `LAYER` command) will automatically update in the plugin's UI.

## Features

- **List Layers**: Displays a clear, filterable list of all layers in the current drawing.
- **Create Layers**: Quickly add new layers with a specified name and a randomly assigned color.
- **Delete Layers**: Remove layers, with built-in safeguards to prevent the deletion of the active layer, layer "0", or layers that contain objects.
- **Modify Properties**:
  - Toggle layer visibility (On/Off).
  - Change layer color using a modern color picker.
- **Highlight Objects**: Select all objects on a specific layer within the AutoCAD drawing, making them easy to identify.
- **Real-Time Sync**: Layer properties are synchronized in real-time between the plugin and AutoCAD.

## Requirements

- **Autodesk AutoCAD 2026** (The project references the AutoCAD 2026 SDK).
- **.NET 8.0** or later.

## How to Use

1.  **Build the Project**: Follow the build instructions below to compile the plugin.
2.  **Load the Plugin in AutoCAD**:
    - Open AutoCAD.
    - Type the `NETLOAD` command in the AutoCAD command line.
    - Browse to the build output directory (`bin/Debug/net8.0/`) and select `AutoCADLayerManager.dll`.
3.  **Launch the UI**:
    - Type the `LAYERUI` command in the AutoCAD command line.
    - The Layer Manager window will appear.

## How to Build

This project can be built and published from the command line using the .NET CLI.

```bash
# Navigate to the root directory of the project
# Build and publish the project for Windows x64
dotnet publish -c Debug -r win-x64 --self-contained false
```

The output files will be located in the `bin/Debug/net8.0/win-x64/publish/` directory.