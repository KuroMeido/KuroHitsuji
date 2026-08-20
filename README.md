# KuroHitsuji

KuroHitsuji is a C# Autodesk Revit add-in focused on helping users find, inspect, and fix visibility-related issues in views. It includes tools for uncovering hidden elements, categories, filters, and other view-dependent items, with support for multiple Revit versions.

## Features

- Revit add-in written in C#
- Commands for collecting and un-hiding elements in views
- Visibility troubleshooting helpers for:
  - hidden elements
  - hidden categories
  - filters
  - view range and related view settings
- Version-aware builds for Revit 2019 through 2026

## Supported versions

This project is configured for:

- Revit 2019
- Revit 2020
- Revit 2021
- Revit 2022
- Revit 2023
- Revit 2024
- Revit 2025
- Revit 2026

## Project structure

- `KuroHitsuji.sln` — Visual Studio solution
- `KuroHitsuji_command/` — main Revit add-in project

## Requirements

To build and run this project, you need:

- Windows
- Autodesk Revit installed for the target version
- Visual Studio 2022 or newer
- .NET SDKs required by the target Revit version

## Build notes

The project uses conditional configurations for different Revit releases. For example:

- `R2019` through `R2024` target .NET Framework 4.8
- `R2025` and `R2026` target `net8.0-windows`

The project also references Revit API assemblies from the local Revit installation path.

## Usage

1. Open the solution in Visual Studio.
2. Select the configuration for the Revit version you want to build.
3. Build the add-in.
4. Load the resulting add-in into Revit.

## License

Add a license here if you want to publish this project publicly.
