# Revit_Shell

Tien ich Explorer cho file Revit `.rvt`, `.rfa`, `.rft`, `.rte`.

## Cau truc

- `RevitShell.Core`
  - Domain logic doc version va inspect file Revit.
- `RevitShell`
  - SharpShell-based class library build ra `RevitShell.dll`.
  - Chua context menu extension cho Explorer.
- `Installer`
  - WinForms installer build ra `Installer.exe`.
  - Copy cac file can thiet va register/unregister `RevitShell.dll` bang `srm.exe`.
- `RevitShell.sln`
  - Solution entry point cho Visual Studio va `dotnet build`.

Kien truc nay tach ro:
- domain logic
- shell extension
- installer/runtime packaging

Nen de maintain va de mo rong hon so voi viec de tat ca trong mot app `.exe`.

## Build

```powershell
dotnet build .\RevitShell.sln -c Release
```

Output chinh:

- Shell extension: `RevitShell\bin\Release\net48\RevitShell.dll`
- Installer: `Installer\bin\Release\net48\Installer.exe`

## Cach dung

1. Build solution.
2. Chay `Installer.exe` voi quyen admin.
3. Bam `Install`.
4. Right-click file Revit trong Explorer.

## Context Menu

- `Revit Version Info`
  - Hien thong tin version cua file Revit.
- `Open with exact Revit version`
  - Co gang mo bang dung version Revit cua file.
  - Neu may khong co dung version thi fallback sang ban Revit cao nhat dang cai.

## Ghi chu

- `SharpShell` la COM shell extension, nen khac voi huong registry verb + exe launcher cu.
- Installer hien tai dung `srm.exe` theo flow duoc SharpShell huong dan.
- Vi shell extension duoc register machine-wide, installer can quyen admin.
