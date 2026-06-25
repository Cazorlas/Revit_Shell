# Revit Shell

Revit Shell is a Windows Explorer context menu extension for Revit files.

It adds two commands for these file types:

- `.rvt`
- `.rfa`
- `.rft`
- `.rte`

Features:

- `Revit Version Info`
  - Shows the file name, full path, and detected Revit version.
- `Open with exact Revit version`
  - Opens the file only when the matching Revit version is installed.
  - Does not fall back to another installed version.

## Architecture

The solution is split into five projects:

- `RevitShell.Domain`
  - Domain models and shared business concepts.
  - Contains types such as `RevitInfo`, `RevitFileTypes`, and `RevitInstallationInfo`.

- `RevitShell.Application`
  - Application-level contracts and use-case services.
  - Contains interfaces such as `IRevitVersionDetector`, `IRevitFileInspector`, and `IRevitInstallationLocator`.
  - Contains `RevitFileInspector`, which turns a file path into a normalized `RevitInfo`.

- `RevitShell.Infrastructure`
  - Technical implementations for file parsing and registry access.
  - Reads Revit metadata from the `BasicFileInfo` structured storage stream.
  - Contains the fallback text detector and the Windows registry-based Revit installation locator.

- `RevitShell`
  - SharpShell-based Explorer extension.
  - Builds `RevitShell.dll`.
  - Provides the context menu UI and file launch behavior.

- `Installer`
  - WiX/WixSharp-based MSI builder.
  - Packages the shell extension and all required runtime dependencies.
  - Registers and unregisters the COM shell extension with `srm.exe`.

This keeps domain concepts, application contracts, infrastructure details, Explorer integration, and MSI packaging separated in a cleaner architecture.

## Tech Stack

- .NET Framework 4.8
- [SharpShell](https://github.com/dwmkerr/sharpshell)
- [WixSharp](https://github.com/oleg-shilo/wixsharp)
- `ServerRegistrationManager` (`srm.exe`)

Implementation references:

- [ricaun-io/RevitShell](https://github.com/ricaun-io/RevitShell)
- [ricaun-io/ricaun.Revit.FileInfo](https://github.com/ricaun-io/ricaun.Revit.FileInfo)
- [phi-ag/rvt-app](https://github.com/phi-ag/rvt-app)

## How Version Detection Works

The primary detector reads the `BasicFileInfo` stream from the Revit file's structured storage container.

Detection flow:

1. Verify that the target file is a structured storage file.
2. Open the `BasicFileInfo` stream.
3. Try to extract version information from metadata text such as:
   - `Format: 2023`
   - `Revit Build: Autodesk Revit 2023`
4. If that fails, fall back to binary parsing rules for known Revit file layouts.
5. If no structured-storage result is available, a secondary text scan is used as a backup.

This approach was chosen because it is more reliable inside Explorer than the earlier `OpenMcdf`-based experiment.

## Context Menu Behavior

### Revit Version Info

Displays:

- `Name`
- `Path`
- `Version`

### Open with exact Revit version

Behavior:

- If the file version is detected and the exact matching Revit version is installed, the file is opened with that `Revit.exe`.
- If the version cannot be detected, an error is shown.
- If the file is Revit 2024 but only Revit 2023 is installed, the command shows an error instead of launching Revit 2023.

## Build

Build the full solution in Release mode:

```powershell
dotnet build .\RevitShell.sln -c Release
```

Important outputs:

- Shell extension DLL:
  - `RevitShell\bin\Release\net48\RevitShell.dll`
- Domain library:
  - `RevitShell.Domain\bin\Release\net48\RevitShell.Domain.dll`
- Application library:
  - `RevitShell.Application\bin\Release\net48\RevitShell.Application.dll`
- Infrastructure library:
  - `RevitShell.Infrastructure\bin\Release\net48\RevitShell.Infrastructure.dll`
- MSI builder:
  - `Installer\bin\Release\net48\Installer.exe`
- MSI package:
  - `Installer\bin\Release\msi\RevitShell.msi`

## MSI Packaging

The `Installer` project is a console-based MSI generator.

During a Release build:

1. `Installer.exe` is built.
2. The project automatically runs `Installer.exe`.
3. `Installer.exe` scans `RevitShell\bin\Release\net48`.
4. It packages all DLLs from that folder plus `srm.exe`.
5. It generates `RevitShell.msi`.

Install location:

- `C:\Program Files\RevitShell`

The MSI also creates a Start Menu shortcut:

- `Uninstall Revit Shell`

## Install

Run the generated MSI as administrator:

```powershell
msiexec /i "D:\Repository\Cazorlas\Revit_Shell\Installer\bin\Release\msi\RevitShell.msi"
```

What the installer does:

1. Copies the shell extension files into `C:\Program Files\RevitShell`.
2. Runs `srm.exe install "[INSTALLDIR]RevitShell.dll" -codebase -os64`.
3. Registers the COM shell extension for all files, while the extension only shows its menu for supported Revit file types.
4. Adds the shell extension CLSID to the Windows approved shell extensions list.

If Explorer does not refresh immediately, restart Explorer manually after installation.

## Uninstall

You can uninstall in either of these ways:

- Start Menu > `Uninstall Revit Shell`
- Command line:

```powershell
msiexec /x "D:\Repository\Cazorlas\Revit_Shell\Installer\bin\Release\msi\RevitShell.msi"
```

During uninstall, the MSI runs:

```text
srm.exe uninstall "[INSTALLDIR]RevitShell.dll"
```

## Repository Layout

```text
Revit_Shell/
|-- Installer/
|-- RevitShell/
|-- RevitShell.Domain/
|-- RevitShell.Application/
|-- RevitShell.Infrastructure/
|-- sources/
|-- RevitShell.sln
|-- README.md
```

## Notes

- This project is a COM shell extension, not a simple registry verb that launches an external `.exe`.
- Installation and removal require administrator privileges because the shell extension is registered machine-wide.
- The context menu icon is embedded from:
  - `sources/images/autodesk_logo.png`

## Future Improvements

- Add automated tests for `RevitShell.Application` and `RevitShell.Infrastructure`
- Add structured diagnostics for version-detection failures
- Replace the runtime PNG resize with a dedicated `.ico`
- Add CI packaging for MSI release artifacts
