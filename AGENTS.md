# AGENTS.md

## Purpose

LocalSmtpCapture is a .NET 10 console app for local SMTP capture. For product requirements and task order, read `docs/requirements.md` and `docs/implementation-plan.md`.

## Board Workflow

Kanban project ID:

```text
project_26331d37-5180-4334-9bc1-2efb709ba72b
```

Use this sequence for implementation tasks:

```text
promote unblocked backlog -> claim as claude-agent -> in_progress -> implement -> verify -> record artifacts -> request review -> release claim
```

Hold only one claim at a time.

## Important Paths

```text
src/LocalSmtpCapture/Configuration/
src/LocalSmtpCapture/Hosting/
tests/LocalSmtpCapture.Tests/
```

The app project targets `net10.0` and uses package ID `LocalSmtpCapture`.

## Configuration Notes

Defaults live in `src/LocalSmtpCapture/appsettings.json`. Keep tests aligned with that file instead of duplicating defaults unnecessarily.

`Program.CreateBuilder` loads `appsettings.json` from `AppContext.BaseDirectory`, then applies environment variables. Keep this behavior: `dotnet run --project ...` may use the repo root as content root, while `appsettings.json` is copied to the app output directory.

Current startup is host/configuration only. The SMTP listener, persistence, and message formatting are later tasks.

## Verification

Run from the repository root:

```powershell
dotnet build LocalSmtpCapture.sln
dotnet test LocalSmtpCapture.sln
dotnet run --project src\LocalSmtpCapture\LocalSmtpCapture.csproj --no-build
```

Startup should log SMTP host, SMTP port, authentication enabled state, email output folder, and configuration sources.

When using PowerShell `Start-Process` for startup smoke checks, do not pass the same file to `-RedirectStandardOutput` and `-RedirectStandardError`; PowerShell fails before starting the process. Use separate stdout and stderr log files instead.

The sandbox may deny access to user-level NuGet config files. If build or test fails with access denied for `C:\Users\MONTEITH\AppData\Roaming\NuGet\NuGet.Config`, rerun the same `dotnet build` or `dotnet test` command with escalated permissions.

## Coding Notes

- Follow the task boundaries in `docs/implementation-plan.md`.
- Add XML documentation for public C# types and members.
- Keep options, validation, persistence, and formatting testable without opening a socket.
- Use `appsettings.json` plus environment variables as supported configuration sources.
- Do not commit or rely on generated `bin/` or `obj/` output.
