# CastoPet Tests

CastoPet currently uses a dependency-free console test harness. The test source is organized
by the architectural boundary it primarily verifies while remaining in one project during
the first migration phase.

```text
CastoPet.Tests/
|-- Program.cs                  # Console entry point only
|-- TestSuite.Catalog.cs        # Ordered feature-catalog aggregation
|-- Application/               # Use cases and orchestration boundaries
|-- Architecture/              # Repository and dependency-direction contracts
|-- Catalog/                   # Stable test names grouped by feature
|-- Core/                      # Pure policies, planners, models, and controllers
|-- Harness/                   # Test case model and runner
|-- Infrastructure/            # Persistence, assets, platform, and shell behavior
|-- Presentation/              # WPF composition and window source contracts
`-- Support/                   # Assertions, fakes, fixtures, and shared test data
```

## Adding Tests

Repository-tool checks live separately under `Tooling/` and run with PowerShell 7:

```powershell
pwsh -NoProfile -File tests/Tooling/LineEndings.Tests.ps1
```

These tests use a temporary Git repository to verify the line-ending checker and its
explicit fix mode without rewriting application fixtures or assets.

- Add a test method to the module matching the behavior under test.
- Register its stable display name in the matching file under `Catalog/`.
- Add a new feature catalog to `TestSuite.Catalog.cs` only when introducing a new test area.
- Keep `Program.cs` limited to passing the aggregated catalog to `TestRunner`.
- Put reusable fixtures and source-inspection helpers under `Support/`.
- Keep Core tests independent of WPF, Win32, the file system, and network access when the
  production API permits it.

The next migration phase may extract `Core`, `Infrastructure`, and `Presentation` into separate test
projects after their production dependencies have been separated. Until then, this layout
keeps the existing single command and complete test coverage intact.
