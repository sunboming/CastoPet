# Castorice Per-Expression Transitions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate, edit, write back, load, and play eight independent idle-to-expression transition sequences while reusing Castorice's existing idle layers and static expression targets.

**Architecture:** CastoPetAnimator owns deterministic layer rendering, expression project scaffolding, batch rendering, validation, and atomic writeback. CastoPet owns expression metadata and playback, preferring per-expression frames and reversing them on exit while retaining the existing generic transition as a compatibility fallback.

**Tech Stack:** TypeScript 5.7, React 18, Konva, Sharp, Vite, Vitest, C# 14, .NET 10, WPF, Playwright

---

## File Map

CastoPetAnimator:

- `src/core/renderLayer.ts`: render one evaluated layer on a full transparent canvas with Konva-compatible pivot transforms.
- `src/core/castopetExpressions.ts`: expression IDs, project paths, source-target scanning, and six-frame project scaffolding.
- `src/cli/expressionTransitions.ts`: single/batch render, endpoint validation, backups, and atomic Runtime writeback.
- `src/cli/render.ts`: delegate layer rasterization to `renderLayer.ts` and expose a reusable project-render function.
- `src/cli/castopet.ts`, `src/cli/main.ts`: route expression CLI subcommands and flags.
- `server/castopetApi.ts`: expose expression scaffold/connect/save/render/apply operations to the local webpage.
- `src/app/App.tsx`: add regular-action/expression-transition selection without duplicating the editor surface.

CastoPet:

- `src/CastoPet/Core/PetExpressionDefinition.cs`: add transition paths and interval.
- `src/CastoPet/Core/ExpressionTransitionPlanner.cs`: choose specific/fallback frames and reverse specific frames on exit.
- `src/CastoPet/Core/PetSkinManifestLoader.cs`, `PetSkinManifestWriter.cs`: load schema 1 string expressions and schema 2 expression objects.
- `src/CastoPet/Core/AssetService.cs`: load per-expression transition images.
- `src/CastoPet/PetWindow.xaml.cs`: play the selected expression's own transition sequence.
- `src/CastoPet/Assets/Skins/Castorice/expressions/targets`: CastoPet-owned immutable expression targets.
- `src/CastoPet/Assets/Skins/Castorice/actions/expressions`: CastoPet-owned editable transition projects.
- `src/CastoPet/Assets/Runtime/Castorice/Expressions/<Label>/Transition`: generated runtime PNG sequences.

### Task 1: Make CLI rendering honor pivot, rotation, and scale

**Files:**
- Create: `D:/Projects/CastoPetAnimator/src/core/renderLayer.ts`
- Create: `D:/Projects/CastoPetAnimator/src/core/__tests__/renderLayer.test.ts`
- Modify: `D:/Projects/CastoPetAnimator/src/cli/render.ts`

- [x] **Step 1: Write failing full-canvas transform tests**

Create tests that render a small opaque marker layer through this API:

```ts
export interface LayerRenderOptions {
  canvas: { width: number; height: number };
  sourcePath: string;
  layer: EvaluatedLayer;
}

export async function renderLayerToCanvas(options: LayerRenderOptions): Promise<Buffer>;
```

Assert that a pivoted `x/y` translation moves the marker to the expected quadrant, a 90-degree rotation changes its horizontal/vertical bounds, `scaleX: 2` doubles its visible width, and `opacity: 0.5` produces alpha near 128.

- [x] **Step 2: Run the focused test and verify RED**

Run: `npm test -- src/core/__tests__/renderLayer.test.ts`

Expected: FAIL because `renderLayerToCanvas` does not exist.

- [x] **Step 3: Implement full-canvas SVG-backed transforms**

Read the source PNG as base64 and rasterize a canvas-sized SVG whose transform matches the current Konva node configuration:

```ts
const transform = [
  `translate(${pivot.x + value.x} ${pivot.y + value.y})`,
  `rotate(${value.rotation})`,
  `scale(${value.scaleX} ${value.scaleY})`,
  `translate(${-pivot.x} ${-pivot.y})`
].join(" ");
```

Apply opacity on the SVG group, embed the PNG with its real metadata width/height, and return a transparent canvas-sized PNG. Reject non-finite transforms and non-positive canvas dimensions.

- [x] **Step 4: Replace translation-only CLI composition**

Extract the frame loop as:

```ts
export async function renderProjectAction(options: RenderCommandOptions): Promise<string[]>;
```

For each visible evaluated layer, call `renderLayerToCanvas`, then composite every returned buffer at `{ left: 0, top: 0 }`. Keep `renderCommand` as the CLI wrapper.

- [x] **Step 5: Verify renderer tests and existing suite**

Run:

```powershell
npm test -- src/core/__tests__/renderLayer.test.ts
npm test
npm run build
```

Expected: all Vitest tests pass and both TypeScript builds plus Vite build succeed.

- [ ] **Step 6: Commit only renderer files**

```powershell
git add src/core/renderLayer.ts src/core/__tests__/renderLayer.test.ts src/cli/render.ts
git commit -m "feat: render pivoted layer transforms in cli"
```

### Task 2: Scan expressions and scaffold eight editable projects

**Files:**
- Create: `D:/Projects/CastoPetAnimator/src/core/castopetExpressions.ts`
- Create: `D:/Projects/CastoPetAnimator/src/core/__tests__/castopetExpressions.test.ts`
- Modify: `D:/Projects/CastoPetAnimator/src/core/castopetLink.ts`

- [x] **Step 1: Write failing scan and scaffold tests**

Define these public types and functions in the tests:

```ts
export type CastoPetExpressionId =
  | "happy" | "shy" | "sleepy" | "surprised"
  | "pouting" | "confused" | "proud" | "crying";

export interface CastoPetExpressionScan {
  id: CastoPetExpressionId;
  label: string;
  targetPath: string;
  projectPath: string;
  runtimeDirectory: string;
  available: boolean;
  error?: string;
}

export async function scanCastoPetExpressions(root: string): Promise<CastoPetExpressionScan[]>;
export async function scaffoldCastoPetExpressionProjects(root: string): Promise<CastoPetExpressionScan[]>;
```

Assert stable order, exact target/project/runtime paths, copied source targets, six frames at 15 FPS, `loop: false`, eight manual layers plus `expressionTarget`, and export name `Castorice.Expression.Happy.Transition.{frame}.png`.

- [x] **Step 2: Run the focused test and verify RED**

Run: `npm test -- src/core/__tests__/castopetExpressions.test.ts`

Expected: FAIL because the module does not exist.

- [x] **Step 3: Implement stable expression conventions**

Add one readonly convention table with the eight IDs and labels. Resolve targets from `Assets/Runtime/Castorice/Expressions/Castorice.Expression.<Label>.png`, source targets under `Assets/Skins/Castorice/expressions/targets/<Label>.png`, projects under `actions/expressions/<id>.transition.animator.json`, and runtime frames under `Expressions/<Label>/Transition`.

- [x] **Step 4: Scaffold non-looping projects without overwriting edits**

Create a project only when its JSON is absent. Use the existing manual layer order and pivots, relative paths `../../layers/manual/<layer>.png`, and target path `../../expressions/targets/<Label>.png`.

Use these shared fade keys:

```ts
const sourceOpacity = [1, 1, 1, 0.82, 0.35, 0];
const targetOpacity = [0, 0, 0, 0.18, 0.65, 1];
```

Use frame indices `0..5`, `easeInOutSine`, and these pose intents at frames 1–4:

| ID | Body/face motion | Arms | Hair |
|---|---|---|---|
| happy | rise to `y=-4`, slight `scaleY=1.01` | inward 5 px, up 5 px, rotations `+8/-8` | rise 3 px with one-frame lag |
| shy | sink to `y=2`, `scaleX=0.99` | inward 4 px, down 1 px | sink 1 px |
| sleepy | sink to `y=4`, face rotation `2` | down 2 px | sink 3 px with lag |
| surprised | frame 1 `scaleY=0.98`, then rise to `y=-5`, `scaleY=1.02` | outward 4 px, up 4 px, rotations `-10/+10` | rise 3 px after body |
| pouting | face `y=-1`, body `scaleX=1.01` | toward hips 3 px, down 1 px, rotations `+8/-8` | settle without overshoot |
| confused | upper body rotation `2`, face/front hair rotation `5` | inward 2 px, up 3 px | back hair rotation `3` with lag |
| proud | rise to `y=-3`, body `scaleX=1.01` | inward 3 px, up 4 px | rise 2 px with slight overshoot |
| crying | sink to `y=3`, body `scaleX=0.98` | inward 5 px, up 6 px | sink 2 px; frame 4 horizontal tremble 1 px |

- [x] **Step 5: Include expressions in the CastoPet scan result**

Add `expressions: CastoPetExpressionScan[]` to `CastoPetProjectScan` and call `scanCastoPetExpressions` after the existing idle/move/blink scans. Missing individual expression targets must be reported in that expression's status without making idle/move/blink scanning fail.

- [x] **Step 6: Verify scan/scaffold tests and build**

Run:

```powershell
npm test -- src/core/__tests__/castopetExpressions.test.ts src/core/__tests__/castopetLink.test.ts
npm run build
```

Expected: expression tests pass; existing action scans still return idle, move, blink unchanged.

- [ ] **Step 7: Commit expression domain files**

```powershell
git add src/core/castopetExpressions.ts src/core/__tests__/castopetExpressions.test.ts src/core/castopetLink.ts src/core/__tests__/castopetLink.test.ts
git commit -m "feat: scaffold CastoPet expression transitions"
```

### Task 3: Add single and batch expression CLI workflows

**Files:**
- Create: `D:/Projects/CastoPetAnimator/src/cli/expressionTransitions.ts`
- Create: `D:/Projects/CastoPetAnimator/src/cli/__tests__/expressionTransitions.test.ts`
- Modify: `D:/Projects/CastoPetAnimator/src/cli/castopet.ts`
- Modify: `D:/Projects/CastoPetAnimator/src/cli/main.ts`

- [x] **Step 1: Write failing CLI workflow tests**

Test these operations against temporary CastoPet roots:

```ts
initializeExpressionTransitions({ root });
connectExpressionTransition({ root, expression: "happy" });
renderExpressionTransition({ root, expression: "happy", out });
renderAllExpressionTransitions({ root, outRoot });
applyExpressionTransition({ root, expression: "happy", frames, workspaceRoot });
applyAllExpressionTransitions({ root, framesRoot, workspaceRoot });
```

Assert that render output has six expected names, frame 00 is byte-identical to Runtime Idle.00, frame 05 is byte-identical to the Happy target, validation rejects wrong size/count/endpoints, and failed validation leaves Runtime untouched.

- [x] **Step 2: Run the focused test and verify RED**

Run: `npm test -- src/cli/__tests__/expressionTransitions.test.ts`

Expected: FAIL because the workflow functions do not exist.

- [x] **Step 3: Implement render and endpoint enforcement**

Call `renderProjectAction`, then replace output frame 00 with `Castorice.Idle.00.png` and frame 05 with the expression source target. Validate every output with Sharp metadata: PNG, 320×320, alpha channel, six consecutive names, exact first/last hashes.

- [x] **Step 4: Implement validated directory swap and backups**

Stage all frames in a sibling temporary directory. After validation, rename the current Runtime transition directory to a sibling rollback directory, rename staging into place, copy the old directory into `workspaces/<project>/backups/expression-<id>-<stamp>`, then remove the rollback directory. On any rename failure, restore the previous Runtime directory before rethrowing.

- [x] **Step 5: Route explicit AI-friendly subcommands**

Support:

```powershell
npm run castopet -- expressions-init --root D:\Projects\CastoPet
npm run castopet -- expression-connect --root D:\Projects\CastoPet --expression happy
npm run castopet -- expression-render --root D:\Projects\CastoPet --expression happy --out exports\expressions\happy
npm run castopet -- expressions-render --root D:\Projects\CastoPet --out exports\expressions
npm run castopet -- expression-apply --root D:\Projects\CastoPet --expression happy --frames exports\expressions\happy
npm run castopet -- expressions-apply --root D:\Projects\CastoPet --frames exports\expressions
```

Add `expression?: string` and `out?: string` to parsed options. Reject unknown expression IDs with a message listing all eight valid IDs.

- [x] **Step 6: Verify CLI tests and full editor suite**

Run:

```powershell
npm test -- src/cli/__tests__/expressionTransitions.test.ts src/cli/__tests__/castopet.test.ts src/cli/__tests__/args.test.ts
npm test
npm run build
```

Expected: all tests and build pass.

- [ ] **Step 7: Commit CLI workflow files**

```powershell
git add src/cli/expressionTransitions.ts src/cli/__tests__/expressionTransitions.test.ts src/cli/castopet.ts src/cli/main.ts
git commit -m "feat: render and apply expression transitions"
```

### Task 4: Expose expression editing in the Chinese web UI

**Files:**
- Modify: `D:/Projects/CastoPetAnimator/server/castopetApi.ts`
- Modify: `D:/Projects/CastoPetAnimator/server/__tests__/castopetApi.test.ts`
- Modify: `D:/Projects/CastoPetAnimator/src/app/App.tsx`
- Modify: `D:/Projects/CastoPetAnimator/src/app/styles.css`

- [x] **Step 1: Write failing local API tests**

Add HTTP tests for:

```text
POST /api/castopet/expressions/init
POST /api/castopet/expression/connect
POST /api/castopet/expression/save
POST /api/castopet/expression/render
POST /api/castopet/expression/apply
POST /api/castopet/expressions/render
POST /api/castopet/expressions/apply
```

Verify `expression` validation, embedded data URLs for all nine project layers, saved project path, output frame count, and backup path.

- [x] **Step 2: Run API tests and verify RED**

Run: `npm test -- server/__tests__/castopetApi.test.ts`

Expected: new endpoints return 404.

- [x] **Step 3: Implement API routing through the CLI workflow module**

Keep request parsing in `castopetApi.ts`; do not duplicate scanning, rendering, or writeback logic. Extend the save helper to accept an exact validated expression project path rather than forcing idle/move/blink filenames.

- [x] **Step 4: Add a compact editor mode and expression selector**

Add a two-option mode selector: `常规动作` and `情绪过渡`. In expression mode show one dropdown with the eight Chinese labels, hide the default/manual variant selector, and reuse the existing timeline, layer list, transform inspector, visibility controls, play/pause, and save controls.

Switching mode or expression must call `setIsPlaying(false)`, reset frame to 0, load the selected project and nine layer assets, and set the output directory to `exports\\expressions\\<id>`.

- [x] **Step 5: Add render/writeback commands without duplicating action controls**

In expression mode, the primary commands become `生成当前过渡`, `写回当前过渡`, and `生成并写回全部情绪`. Require the existing confirmation dialog before Runtime changes and report the returned backup directory.

- [x] **Step 6: Verify API, React build, and unit suite**

Run:

```powershell
npm test -- server/__tests__/castopetApi.test.ts
npm test
npm run build
```

Expected: API tests, all Vitest tests, TypeScript, and Vite build pass.

- [ ] **Step 7: Commit server and UI files**

```powershell
git add server/castopetApi.ts server/__tests__/castopetApi.test.ts src/app/App.tsx src/app/styles.css
git commit -m "feat: edit expression transitions in web ui"
```

### Task 5: Extend CastoPet expression metadata with backward-compatible manifests

**Files:**
- Modify: `src/CastoPet/Core/PetExpressionDefinition.cs`
- Modify: `src/CastoPet/Core/PetSkinManifestLoader.cs`
- Modify: `src/CastoPet/Core/PetSkinManifestWriter.cs`
- Modify: `src/CastoPet/Core/BuiltInPetSkins.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [x] **Step 1: Write failing model and manifest tests**

Require this model:

```csharp
public sealed record PetExpressionDefinition(
    string Id,
    string Label,
    string ResourcePath,
    IReadOnlyList<string>? TransitionFramePaths = null,
    TimeSpan? TransitionFrameInterval = null);
```

Test that schema 1 still accepts `"Happy": "Expressions/Happy.png"`, schema 2 accepts an object with `image`, `transitionFrames`, and `transitionFrameIntervalMs`, and writer output reloads with all paths and timing intact.

- [x] **Step 2: Run the CastoPet test runner and verify RED**

Run: `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj`

Expected: compilation or assertions fail because transition metadata and schema 2 are unsupported.

- [x] **Step 3: Implement schema 1/string and schema 2/object loading**

Keep schema 1 accepted. Emit schema 2 from the writer. Parse expression values through `JsonElement`: a string creates a static-only expression; an object requires `image`, resolves every transition frame relative to `resourceRoot`, and converts `transitionFrameIntervalMs` to `TimeSpan`.

Writer shape:

```json
"expressions": {
  "Happy": {
    "image": "Expressions/Castorice.Expression.Happy.png",
    "transitionFrames": [
      "Expressions/Happy/Transition/Castorice.Expression.Happy.Transition.00.png"
    ],
    "transitionFrameIntervalMs": 66.6667
  }
}
```

- [x] **Step 4: Define six built-in frames per expression**

Update `CreateExpression` to generate `00..05` paths under `Assets/Runtime/Castorice/Expressions/<Label>/Transition` and set `TimeSpan.FromMilliseconds(1000d / 15d)`. Keep the existing final PNG path unchanged.

- [x] **Step 5: Verify tests and build**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build src\CastoPet\CastoPet.csproj
```

Expected: all tests pass and build reports zero errors.

- [ ] **Step 6: Commit model and manifest files**

```powershell
git add src/CastoPet/Core/PetExpressionDefinition.cs src/CastoPet/Core/PetSkinManifestLoader.cs src/CastoPet/Core/PetSkinManifestWriter.cs src/CastoPet/Core/BuiltInPetSkins.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: describe per-expression transition frames"
```

### Task 6: Play specific transitions forward and backward in CastoPet

**Files:**
- Create: `src/CastoPet/Core/ExpressionTransitionPlanner.cs`
- Create: `src/CastoPet/Core/PetExpressionAsset.cs`
- Modify: `src/CastoPet/Core/AssetService.cs`
- Modify: `src/CastoPet/PetWindow.xaml.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [x] **Step 1: Write failing pure playback-order tests**

Require:

```csharp
public static IReadOnlyList<T> EnterFrames<T>(IReadOnlyList<T> specific, IReadOnlyList<T> fallback);
public static IReadOnlyList<T> ExitFrames<T>(IReadOnlyList<T> specific, IReadOnlyList<T> fallback);
```

Assert specific enter is unchanged, specific exit is reversed, empty specific uses generic enter/exit without reversing the generic out sequence, and both empty lists return empty.

- [x] **Step 2: Run tests and verify RED**

Run: `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj`

Expected: compilation fails because `ExpressionTransitionPlanner` does not exist.

- [x] **Step 3: Implement the pure planner and loaded asset record**

Use:

```csharp
public sealed record PetExpressionAsset(
    PetExpressionDefinition Definition,
    ImageSource Image,
    IReadOnlyList<ImageSource> TransitionFrames);
```

Add `AssetService.LoadExpressionAssets()` and load a missing transition group as an empty list while preserving the final static image. Log the expression label and failed resource path.

- [x] **Step 4: Refactor PetWindow to hold the selected sequence**

Replace the pending-image-only state with pending/active `PetExpressionAsset`, active frame list, and active interval. On selection, choose specific frames or generic in frames. On timeout, reverse specific frames or use generic out frames. Set the timer interval from the expression definition when specific frames are active, otherwise from the existing generic action.

The last entry frame is the same bitmap as the final static image; `ShowPendingExpression` must retain the current two-second timer and not restart idle/blink. After the last exit frame, restore idle index 0 and existing passive behavior.

- [x] **Step 5: Make generic transition actions optional compatibility data**

Use `TryGetAction` instead of `GetRequiredAction` for expression-transition-in/out. If a skin has neither specific nor generic transition frames, switch directly to the final image and directly restore idle on exit.

- [x] **Step 6: Verify playback tests and application build**

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build src\CastoPet\CastoPet.csproj
```

Expected: all tests pass; zero build errors.

- [ ] **Step 7: Commit playback files**

```powershell
git add src/CastoPet/Core/ExpressionTransitionPlanner.cs src/CastoPet/Core/PetExpressionAsset.cs src/CastoPet/Core/AssetService.cs src/CastoPet/PetWindow.xaml.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: play expression-specific transitions"
```

### Task 7: Generate, tune, and package all eight transition sequences

**Files:**
- Create: `src/CastoPet/Assets/Skins/Castorice/expressions/targets/*.png`
- Create: `src/CastoPet/Assets/Skins/Castorice/actions/expressions/*.transition.animator.json`
- Create: `src/CastoPet/Assets/Runtime/Castorice/Expressions/*/Transition/*.png`
- Modify: `src/CastoPet/CastoPet.csproj`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [x] **Step 1: Write a failing packaged-resource inventory test**

For all eight labels, require one target, one animator JSON, six Runtime PNGs named `00..05`, frame size 320×320, alpha support, frame 00 hash equal to Runtime Idle.00, and frame 05 hash equal to the static expression PNG. Require the project file to include `Assets\Runtime\Castorice\Expressions\*\Transition\*.png` as a WPF Resource glob.

- [x] **Step 2: Run the test and verify RED**

Run: `dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj`

Expected: FAIL listing missing expression source projects and transition frames.

- [x] **Step 3: Initialize source targets and projects**

Run from `D:\Projects\CastoPetAnimator`:

```powershell
npm run castopet -- expressions-init --root D:\Projects\CastoPet
```

Expected: eight source targets and eight non-overwritten animator JSON files are reported.

- [x] **Step 4: Render all eight sequences and create contact sheets**

Run:

```powershell
npm run castopet -- expressions-render --root D:\Projects\CastoPet --out exports\expressions
npm run contact-sheet -- --frames exports\expressions\happy --out exports\expression-happy-sheet.png
```

Repeat contact-sheet generation for shy, sleepy, surprised, pouting, confused, proud, and crying. Inspect every sheet and adjust only its corresponding JSON transform keys until the intended motion is legible without double edges or layer crossings.

- [x] **Step 5: Write all validated sequences to Runtime**

Run:

```powershell
npm run castopet -- expressions-apply --root D:\Projects\CastoPet --frames exports\expressions
```

Expected: 48 frames applied, eight backup entries reported, and no partial expression directory exists.

- [x] **Step 6: Package generated frames and verify inventory**

Add:

```xml
<Resource Include="Assets\Runtime\Castorice\Expressions\*\Transition\*.png" />
```

Run:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build src\CastoPet\CastoPet.csproj
```

Expected: resource inventory passes and build has zero errors.

- [ ] **Step 7: Commit CastoPet-owned source and generated assets**

```powershell
git add src/CastoPet/Assets/Skins/Castorice/expressions src/CastoPet/Assets/Skins/Castorice/actions/expressions src/CastoPet/Assets/Runtime/Castorice/Expressions src/CastoPet/CastoPet.csproj tests/CastoPet.Tests/Program.cs
git commit -m "feat: add Castorice expression transition assets"
```

### Task 8: End-to-end browser and runtime verification

**Files:**
- Create: `D:/Projects/CastoPetAnimator/tests/expression-transitions.spec.ts`
- Create: `D:/Projects/CastoPetAnimator/playwright.config.ts`
- Modify: `D:/Projects/CastoPetAnimator/package.json`
- Modify: `D:/Projects/CastoPetAnimator/package-lock.json`

- [ ] **Step 1: Add the Playwright test runner without downloading another browser**

Run:

```powershell
npm install --save-dev @playwright/test
```

Configure `playwright.config.ts` to use the existing `PLAYWRIGHT_BROWSERS_PATH`, start Vite on a free fixed test port, use Chromium, retain traces on failure, and write screenshots/test artifacts under `test-results` rather than source directories.

- [ ] **Step 2: Write a failing Playwright editor-flow test**

Automate: connect `D:\Projects\CastoPet`, switch to `情绪过渡`, select `开心`, verify playback stops when switching to `害羞`, toggle one layer and use `全部显示`, change a transform, save, render current transition, and assert six frames are reported. Stub only the final destructive confirmation when the test uses a temporary CastoPet root.

- [ ] **Step 3: Run the browser test and verify RED if any workflow is incomplete**

Start the Vite server on a free port and run the Playwright test. Expected before final fixes: any missing selector, status, or endpoint produces a focused failure.

- [ ] **Step 4: Fix only end-to-end wiring defects found by the test**

Do not change animation presets during this step. Keep fixes limited to labels, request payloads, state reset, and status reporting already required above.

- [x] **Step 5: Capture desktop UI and verify canvas pixels**

Capture `test-results/expression-transitions-ui.png` at 1440×1000. Assert the 320×320 canvas contains non-transparent/non-background pixels, controls do not overlap, Chinese labels fit, and the selected expression target is visible on frame 05. Keep this screenshot as local verification output rather than a tracked source asset.

- [x] **Step 6: Run final verification in both repositories**

From CastoPetAnimator:

```powershell
npm test
npm run build
```

From CastoPet:

```powershell
dotnet run --project tests\CastoPet.Tests\CastoPet.Tests.csproj
dotnet build src\CastoPet\CastoPet.csproj
```

Expected: every command exits 0; CastoPet build reports zero errors; all 48 transition frames pass endpoint and dimension checks.

- [x] **Step 7: Review both dirty worktrees without staging unrelated files**

Run `git status --short` and scoped `git diff` in each repository. Confirm no AI/mask code returned, no existing idle/move/blink PNG changed during expression writeback, and only the files named in this plan are included in expression commits.

- [ ] **Step 8: Commit the browser test separately**

```powershell
git add package.json package-lock.json playwright.config.ts tests/expression-transitions.spec.ts
git commit -m "test: cover expression transition editor flow"
```
