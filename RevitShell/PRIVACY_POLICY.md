# Privacy Policy for PaperEngineer Shell

Effective date: October 10, 2026

PaperEngineer Shell respects user privacy. It does not collect, sell, or share personal data, and it never uploads Revit files.

## Local Processing

PaperEngineer Shell runs on the user's Windows machine. It reads the metadata of the Revit files the user selects in File Explorer to detect their Revit version and worksharing state.

Supported file types:

- `.rvt`
- `.rfa`
- `.rft`
- `.rte`

It reads the local Windows registry to find the installed Autodesk Revit versions, so it can open a selected file in the matching version.

## Files and Settings Written on This Machine

- When the user chooses **Detach from central** or **Create new local**, a small request file is written to `%TEMP%\RevitShell` and read by the PaperEngineer Shell add-in inside Revit. Revit then creates the detached copy next to the original, or the local copy in the user's Documents folder.
- The update check stores two values under `HKEY_CURRENT_USER\Software\PaperEngineer\Shell`: the time of the last check and a version the user chose to skip.
- Downloaded updates are kept in `%TEMP%\PaperEngineerShell\Updates`.

## Update Checks

PaperEngineer Shell checks for new versions on GitHub Releases (`api.github.com`, repository `Cazorlas/Revit_Shell`):

- automatically, at most once every 24 hours, after the user runs one of its commands;
- on demand, when the user chooses **Check for updates**.

The request contains only what any web request carries (the IP address seen by GitHub) and a User-Agent naming PaperEngineer Shell and its version. No file names, file contents, user names, or machine identifiers are sent. An update is downloaded and installed only after the user chooses **Update now**, and the download is verified against the SHA-256 digest published by GitHub before the installer runs. GitHub's own privacy statement applies to these requests.

## No Data Collection

PaperEngineer Shell does not use:

- Analytics
- Telemetry
- Advertising SDKs
- Account login
- Third-party tracking

No Revit project, family, template, or library files are uploaded or transmitted.

## No Personal Data Storage

Because PaperEngineer Shell does not collect personal data, it does not retain personal data and has no personal data to delete. Uninstalling removes the application; the update settings above can be removed by deleting the registry key.

## Changes to This Policy

This policy is updated when PaperEngineer Shell changes how it handles data.

## Contact

For privacy questions, open an issue at https://github.com/Cazorlas/Revit_Shell/issues.
