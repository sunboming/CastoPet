# CastoPet Tests

CastoPet currently uses a dependency-free console test harness. The test source is organized
by the architectural boundary it primarily verifies while remaining in one project during
the first migration phase.

```text
CastoPet.Tests/
|-- Program.cs                  # Test runner only
|-- TestSuite.Catalog.cs        # Stable test names and execution order
|-- App/                        # WPF composition and window source contracts
|-- Core/                       # Pure policies, planners, models, and controllers
|-- Infrastructure/             # Persistence, platform, packaging, and shell behavior
`-- Support/                    # Assertions, fakes, fixtures, and shared test data
```

## Adding Tests

- Add a test method to the module matching the behavior under test.
- Register its stable display name in `TestSuite.Catalog.cs`.
- Keep `Program.cs` limited to running and reporting the catalog.
- Put reusable fixtures and source-inspection helpers under `Support/`.
- Keep Core tests independent of WPF, Win32, the file system, and network access when the
  production API permits it.

The next migration phase may extract `Core`, `Infrastructure`, and `App` into separate test
projects after their production dependencies have been separated. Until then, this layout
keeps the existing single command and complete test coverage intact.
