AGENTS.md

Purpose
-------
This file provides concise guidance for agentic coding agents operating in the ParseLord3 repository. It documents the canonical build/test commands, developer verification steps, and the code-style and runtime conventions agents must follow when making changes.

Quick repo facts
----------------
- Language: C# (.NET 10 / net10.0-windows)
- Solution: RotationSolver.sln
- Main plugin project: RotationSolver/RotationSolver.csproj (assembly: ParseLord3)
- Core library: RotationSolver.Basic
- Nullable reference types enabled (Directory.Build.props)
- Target platform: x64, Dalamud API 14

1) Build / Run / Test commands
------------------------------
General build (whole solution):

  dotnet build RotationSolver.sln -c Release

Build single project (plugin):

  dotnet build RotationSolver/RotationSolver.csproj -c Release

Build single core library (useful for iterating on rotations):

  dotnet build RotationSolver.Basic/RotationSolver.Basic.csproj -c Release

Run unit tests (if/when tests are added):

  dotnet test --no-build -c Release

Run a single test (by fully-qualified name or display name):

  dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName" -c Release
  dotnet test --filter "DisplayName=MyTestName" -c Release

Notes on single-test selection:
- Use --filter with FullyQualifiedName or DisplayName. Regex-like matches are supported (use ~ for contains).
- If using an xUnit/NUnit/MSTest runner specific adapter, specify the test project path instead of the solution.

Formatting and static checks
---------------------------
- Run formatter (recommended before committing):

  dotnet tool install -g dotnet-format   # if not installed
  dotnet format RotationSolver.sln

- Run analyzers and restore:

  dotnet restore
  dotnet build -c Release    # analyzers run during build if configured

- CI note: Directory.Build.props sets <EnforceCodeStyleInBuild>False. Still run dotnet format and any repository linters locally.

2) Developer workflow checklist (agents)
----------------------------------------
For any code change, follow this checklist before proposing a PR or committing:

1. Ensure a focused, single purpose change per PR.
2. Run `dotnet build` for any touched projects.
3. Run `dotnet format` and fix any formatting issues.
4. Run unit tests (or add tests) and verify they pass.
5. Run `dotnet build` Release and verify the post-build CopyToDevPlugins target (plugin project) places files in `%APPDATA%\XIVLauncher\devPlugins\ParseLord3\`.
6. Lint and static analysis: ensure no new warnings are introduced unless necessary; explain unavoidable warnings in PR.

3) Code-style & conventions (must-follow)
----------------------------------------
A. General principles
- Match the repository’s prevailing patterns. If the repo already uses a particular naming or formatting pattern, follow it.
- Keep changes small and reversible. Prefer minimal fixes when addressing bugs.
- No blind refactors in a bug-fix PR — separate refactors into their own PRs.

B. File & namespace layout
- Namespace hierarchy mirrors folder structure: RotationSolver, RotationSolver.Basic, RotationSolver.Updaters, etc.
- One type per file generally. Keep file names matching public types.

C. Naming
- Types (classes, structs, enums, interfaces): PascalCase (e.g., MajorUpdater, DataCenter).
- Public methods / properties: PascalCase (e.g., BeginParseLordTargetChange()).
- Private fields: _camelCase with leading underscore (e.g., _lastKnownTargetId, _parseLordIsSettingTarget).
- Constants: follow existing repository convention. The project historically uses UPPER_SNAKE or ALL_CAPS for const command tokens (e.g., COMMAND, ALTCOMMAND). Match the existing form in the file you change.
- Local variables: camelCase.

D. Usings and import order
- Prefer file-scoped usings when appropriate (project uses implicit usings via Directory.Build.props).
- Order: System / Microsoft / third-party / project-specific. Keep related groups separated by a blank line.
- Minimize global usings; prefer clear, file-scoped using declarations introduced by the project.

E. Types & nullability
- Nullable reference types are enabled. Annotate nullability correctly (e.g., string? when a value can be null).
- Avoid null-forgiving operator (! ) except when absolutely certain; prefer explicit checks.
- Prefer explicit public API types; use var for local variables when the type is obvious from the right-hand side.

F. Error handling & logging
- Never use empty catch blocks. If swallowing an exception is intentional, add a comment explaining why and log at least at Verbose/Debug level.
- Use PluginLog (ECommons.Logging) for logging: PluginLog.Info / Warning / Error / Debug / Verbose as appropriate.
- For user-facing warnings, use BasicWarningHelper.AddSystemWarning or Service.Config toggles where appropriate.
- Do not suppress compiler/type errors with hacks (no @ts-ignore equivalents). Fix root cause.

G. Concurrency & async
- Prefer Task-based async for asynchronous operations; use ConfigureAwait(false) only in library code if necessary.
- When interacting with Dalamud framework ticks (Svc.Framework.RunOnTick), follow existing patterns (wrap asynchronous tasks with Task.Run where needed and handle exceptions).
- Always catch exceptions on framework tick handlers and log them (avoid crashing the update loop).

H. ImGui/UI updates
- Separate visual changes from logic changes. If change is visual-only (colors, layout), delegate to the frontend UI owner or a dedicated UI/UX agent.
- Keep ImGui draws idempotent and fast. Avoid expensive allocations during Draw loops.

I. Tests & coverage
- Add unit tests for business logic (RotationSolver.Basic) where possible.
- Keep tests focused, fast, and hermetic. Prefer pure logic tests over heavy integration tests that require game hooks.

J. Documentation
- Update README.md, YAML manifests, and new_docs when UX or user-facing behavior changes.
- Keep release notes concise and specific.

4) Project-specific conventions & helpers
---------------------------------------
- Use the Svc wrapper (ECommons.DalamudServices) for accessing Dalamud services (Svc.Targets, Svc.Framework, etc.). Do not directly access Dalamud API instances unless necessary.
- Use ECommons helpers where available (ECommons.Logging, ImGuiMethods, etc.). Follow existing usage patterns in code.
- When modifying plugin manifest or dev-deploy logic, ensure CopyToDevPlugins target continues to copy all required assemblies (ParseLord3.dll, ParseLord3.json, RotationSolver.Basic.dll, ECommons.dll).

5) Committing and PRs (agent behavior)
--------------------------------------
- Commit message style: short imperative summary line (50 chars or less), optional body describing why.
- Group related edits into one PR. If multiple modules changed, split into smaller PRs.
- Include in PR description: what changed, why, how to test locally (build + test commands), and any risk/regression notes.

6) Cursor / Copilot / AI assistant rules
---------------------------------------
- Cursor rules: no repository-level .cursor/rules/ or .cursorrules were found. If present, follow repository rules and include them in the AGENTS.md.
- GitHub Copilot instructions: no .github/copilot-instructions.md found. If added, agents must respect those instructions.

7) Safe editing constraints (agents must obey)
---------------------------------------------
- Do not commit or push without explicit user instruction. (Tool policy)
- When editing front-end visuals (ImGui layout, CSS, or image assets), consult the UI/UX owner or run visual regression checks.
- Do not introduce breaking API changes without a migration plan and documentation.

8) Example quick commands (summary)
-----------------------------------
- Build solution: dotnet build RotationSolver.sln -c Release
- Build plugin: dotnet build RotationSolver/RotationSolver.csproj -c Release
- Build core lib: dotnet build RotationSolver.Basic/RotationSolver.Basic.csproj -c Release
- Run all tests: dotnet test -c Release
- Run single test: dotnet test --filter "FullyQualifiedName~Namespace.Class.Method" -c Release
- Format: dotnet format RotationSolver.sln

If you find additional linter or curator files (Cursor/Copilot) in the repo, add an "AI Rules" subsection here describing them.

Contact
-------
If behavior is ambiguous, open an issue in the repository describing the ambiguity and recommended options. When in doubt, ask the repo owner before making large cross-cutting changes.

-- End of AGENTS.md
