# Design and Make Marketplace listing: PaperEngineer Shell

Text to paste into the store listing. Screenshots are in `screenshots/` (1280 x 800); the 120 px icon is
`sources/images/PaperEngineerShell_120.png`.

## App name

PaperEngineer Shell

## Short description

Right-click a Revit file in File Explorer to see its Revit version, then open it in that exact version - detached, as a new local, or directly.

## Description

PaperEngineer Shell adds Revit commands to the File Explorer right-click menu for .rvt, .rfa, .rft and .rte files.

**Revit Version Info**
See which Revit version saved a file, and whether it is workshared (central, local or not enabled), without opening Revit. Select several files to check them all at once.

**Open with exact Revit version**
Opens the file in the Revit version that saved it, so a 2023 model never gets upgraded by accident in 2026. For a workshared model you choose how to open it:
- Detach from central (worksets preserved)
- Create new local (saved in your Documents folder; an existing local is kept as a backup)
- Open directly

**Check for updates**
PaperEngineer Shell checks GitHub Releases at most once a day and asks before installing. Choose Update now, Skip this version, or Later. Downloads are verified by SHA-256 before the installer runs.

## Supported versions

- Detect version: files from any Revit version
- Open with exact version, Detach from central, Create new local: Revit 2014-2027
- Windows 10 and Windows 11, .NET Framework 4.8

## Installation notes

- Close File Explorer windows and Revit, then run the installer as administrator. It upgrades any earlier version.
- On Windows 11 the commands are under "Show more options" in the right-click menu.
- The helper add-in is unsigned. When Revit asks, choose "Always Load"; "Load Once" asks again at every start.

## Privacy

No analytics, telemetry or account. Revit files are never uploaded. The only network request is the update check to api.github.com. See `RevitShell/PRIVACY_POLICY.md`.

## Screenshots (in order)

1. `1-revit-version-info.png` - See the Revit version before you open a file
2. `2-open-workshared.png` - Open central models the safe way
3. `3-update-prompt.png` - Updates only when you say so
4. `4-check-for-updates.png` - Check for updates any time

The right-click menu itself is not in these images. After updating to 1.4.2, a screenshot of the menu
(Revit Version Info, Open with exact Revit version, Check for updates) can be added as a fifth image.
