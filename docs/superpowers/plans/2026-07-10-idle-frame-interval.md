# Castorice Idle Frame Interval Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make CastoPet play Castorice's eight-frame idle animation at the editor-authored 8 FPS rate.

**Architecture:** Keep timing owned by the existing `PetActionDefinition` for the built-in Castorice skin. Change only the idle action's interval from 200 ms to 125 ms and lock that contract with the existing built-in skin test.

**Tech Stack:** C# 14, .NET 10, WPF, the repository's console-based test runner

---

### Task 1: Align the built-in idle interval with the animation project

**Files:**
- Modify: `tests/CastoPet.Tests/Program.cs:448`
- Modify: `tests/CastoPet.Tests/Program.cs:725`
- Modify: `src/CastoPet/Core/BuiltInPetSkins.cs:15`

- [x] **Step 1: Write the failing timing assertion**

In `BuiltInCastoriceIdleActionPreservesCurrentFrames`, replace the existing 200 ms assertion with:

```csharp
Assert.Equal(TimeSpan.FromMilliseconds(125), idle.FrameInterval, "Idle should play at the authored 8 FPS rate.");
```

The test runner also contained a second compatibility assertion for the same built-in idle interval. Rename that test from the old "slow frame paths" wording to "authored-rate frame paths" and require the same 125 ms interval.

- [x] **Step 2: Run the test runner and verify the timing assertion fails**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
```

Expected: the test runner exits non-zero and reports that the built-in Castorice idle action returned `00:00:00.2000000` instead of `00:00:00.1250000`.

- [x] **Step 3: Apply the minimal production change**

In the Castorice idle `PetActionDefinition` in `BuiltInPetSkins.cs`, set:

```csharp
FrameInterval: TimeSpan.FromMilliseconds(125)),
```

Do not change frame paths, frame count, move timing, blink timing, or expression transition timing.

- [x] **Step 4: Verify the focused behavior and full build**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build src\CastoPet\CastoPet.csproj
```

Expected: the test runner reports all tests passed; the application build exits with zero errors.

- [x] **Step 5: Review the scoped diff**

Run:

```powershell
git diff -- tests/CastoPet.Tests/Program.cs src/CastoPet/Core/BuiltInPetSkins.cs
```

Expected: only the idle interval assertion and the built-in idle interval change from 200 ms to 125 ms.
