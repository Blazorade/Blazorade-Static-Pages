---
description: "Build and pack the Blazorade.StaticPages NuGet package using the repository packaging script."
name: "Build and Pack"
argument-hint: "Optional configuration or output path"
agent: "agent"
---
Run the repository packaging script at [Build-And-Pack.ps1](../../scripts/Build-And-Pack.ps1) from the Blazorade-Static-Pages workspace root.

Use PowerShell and execute the script with its default settings unless arguments are provided. If arguments are provided, pass them to the script, such as `-Configuration Debug` or `-OutputPath artifacts/packages`.

Do not modify source files, commit changes, or push anything. Report the package path and whether package verification succeeded.
