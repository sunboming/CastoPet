# Local Packaging

CastoPet uses Velopack 1.2.0 to create a per-user Windows installer and local update packages.

Run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools\package-local.ps1 -Version 0.1.0
```

Generated files are written only to:

`artifacts\local-package\packages`

The script restores the repository-local `vpk` tool, publishes a self-contained `win-x64` application, and creates an installer. It contains no GitHub upload or release command.

The current test package is unsigned. Windows may display an unknown-publisher or SmartScreen warning. Code signing can be supplied later without changing the application update architecture.

The internal Velopack package ID is `CastoPet.App`, so installation files remain separate from user data stored under `%LocalAppData%\CastoPet`.

Before any future public release, verify installation, launch, update checking, restart, and uninstall behavior on a clean Windows user account.
