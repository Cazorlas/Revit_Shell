# Revit_Shell

Tien ich Explorer cho file Revit `.rvt`, `.rfa`, `.rft`, `.rte`.

## Cau truc

- `RevitShell.Core`
  - Domain logic doc version va inspect file Revit.
- `RevitShell`
  - SharpShell-based class library build ra `RevitShell.dll`.
  - Chua context menu extension cho Explorer.
- `Installer`
  - Console MSI builder dung `WixSharp`.
  - Dong goi `RevitShell.dll`, `RevitShell.Core.dll`, `SharpShell.dll`, `srm.exe` vao file `MSI`.
  - Goi `srm.exe` trong custom action de register/unregister shell extension khi install/uninstall.
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
- MSI builder: `Installer\bin\Release\net48\Installer.exe`
- MSI: `Installer\bin\Release\msi\RevitShell.msi`

## Cach dung

1. Build solution.
2. Chay `Installer\bin\Release\msi\RevitShell.msi`.
3. Hoan tat wizard cai dat.
4. Neu Explorer chua refresh thi restart File Explorer.
5. Right-click file Revit trong Explorer.

## Context Menu

- `Revit Version Info`
  - Hien thong tin version bang `MessageBox`.
- `Open with exact Revit version`
  - Co gang mo bang dung version Revit cua file.
  - Neu may khong co dung version thi fallback sang ban Revit cao nhat dang cai.

## Ghi chu

- `SharpShell` la COM shell extension, nen khac voi huong registry verb + exe launcher cu.
- `MSI` hien tai dung `WixSharp` + `srm.exe` theo flow duoc SharpShell huong dan.
- Vi shell extension duoc register machine-wide, qua trinh install/uninstall can quyen admin.
