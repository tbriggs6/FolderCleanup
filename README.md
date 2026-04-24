# UtilitySuite (Windows C# Multi-Utility Starter)

This repository is a starter template for a **single C# app that hosts many small utilities**.

The project is intentionally split into:

- `UtilitySuite.Core` - shared interfaces and models
- `UtilitySuite.Modules` - individual utility implementations
- `UtilitySuite.App` - host application (currently a console shell)

## Why this shape?

You mentioned wanting a Windows utility app with multiple sub-tools and flexibility in UI.

This structure lets you:

- Keep utility logic independent of UI
- Start quickly with a console host
- Later swap/add a Windows GUI (WPF, WinUI 3, Avalonia) while reusing modules

---

## Current starter utilities

1. **Current Timestamp**
   - Returns local and UTC timestamps
2. **Word Count**
   - Counts words from text input
3. **Reverse Text**
   - Reverses text input
4. **Keep Raw**
   - Recursively removes `.JPG` files when a same-name `.CR3` exists in the same folder

---

## Suggested next UI options (Windows)

- **WPF**: mature, stable, great for desktop tooling
- **WinUI 3**: modern Windows look/feel, native direction from Microsoft
- **Avalonia**: if you may later want cross-platform support

Recommended default for a pure Windows utility suite: **WPF**.

---

## Prerequisites

Install the .NET SDK (recommended: .NET 8):

```powershell
winget install Microsoft.DotNet.SDK.8
```

Then run:

```bash
dotnet restore
dotnet build
dotnet run --project src/UtilitySuite.App/UtilitySuite.App.csproj
```

---

## Adding a new utility module

1. Create a class in `src/UtilitySuite.Modules/` implementing `IUtilityModule`
2. Register it in `ModuleCatalog`
3. The host app will automatically surface it in the menu

This keeps growth clean as your utility list expands.

---

## Keep Raw usage notes

- Provide `directory` as an absolute or relative folder path to scan.
- The action compares names within each folder and deletes `.JPG` only if a `.CR3` peer exists.
- Matching is case-insensitive (`IMG_1950.CR3` + `IMG_1950.JPG` => JPG removed).
