# CastoPet Cursor Nudge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an opt-in `推动鼠标` mode that gently nudges the cursor when the pet is actively moving near it.

**Architecture:** Settings and menus follow the existing `AppSettings`/`MenuCommandService`/`TrayService` pattern. Cursor movement math lives in a pure `CursorNudgePlanner`, while `PetWindow` calls a small Windows cursor service only when the planner allows a nudge.

**Tech Stack:** C#/.NET WPF, Win32 `GetCursorPos`/`SetCursorPos`, existing console-style tests.

---

## Task 1: Settings And Menu

**Files:**
- Modify `tests/CastoPet.Tests/Program.cs`
- Modify `src/CastoPet/Core/AppSettings.cs`
- Modify `src/CastoPet/Core/MenuCommandService.cs`
- Modify `src/CastoPet/Core/TrayService.cs`
- Modify `src/CastoPet/PetWindow.xaml.cs`

- [ ] Add tests for default false, round trip, and `TrayService.PushCursorText == "推动鼠标"`.
- [ ] Run `dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj` and verify failure.
- [ ] Add `PushCursor` setting, `TogglePushCursor()`, tray item, and window context item.
- [ ] Run tests and commit `feat: add push cursor setting`.

## Task 2: Cursor Nudge Planner

**Files:**
- Create `src/CastoPet/Core/CursorNudgePlanner.cs`
- Modify `tests/CastoPet.Tests/Program.cs`

- [ ] Add tests for close-distance gating, per-frame clamp, work-area clamp, and manual-movement cooldown.
- [ ] Run tests and verify failure.
- [ ] Implement planner constants and pure math.
- [ ] Run tests and commit `feat: add cursor nudge planner`.

## Task 3: Runtime Cursor Push

**Files:**
- Create `src/CastoPet/Core/WindowsCursorService.cs`
- Modify `src/CastoPet/PetWindow.xaml.cs`

- [ ] Add Win32 cursor service.
- [ ] Store `PushCursor` in `PetWindow.ApplySettings`.
- [ ] During active movement rendering, after pet movement, calculate cursor nudge from movement delta and apply it when allowed.
- [ ] Detect user manual cursor movement and pause push briefly.
- [ ] Run tests/build and commit `feat: gently push cursor during movement`.

## Verification

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet build src/CastoPet/CastoPet.csproj -c Release
```

Expected: tests pass, Release build succeeds with 0 errors.
