# Implementation Plan — SVY 323 Closed Traverse Computation Programme

**Course:** SVY 323 — Computer Application I  
**Repository:** https://github.com/cipher-d-dev/Tranverse-Programming-Surverying  
**Branch:** `main` (single-branch workflow)  
**Language / Stack:** VB.NET · .NET 8 · Windows Forms (GUI) + Console

---

## 1. Project Goal

Deliver a working VB.NET programme that performs a complete closed-traverse
computation — angular adjustment, forward/back bearings, latitudes and
departures, Bowditch correction, final coordinates, linear accuracy, and
polygon area — in two forms:

| Form | Purpose |
|------|---------|
| **Console app** | Prove the algorithm correctness; fast to iterate on; no UI noise. |
| **GUI app** | Presentation-ready Windows Forms interface for the group submission. |

The console app was written first, used to verify the maths, and then the
same algorithm was ported to the GUI.

---

## 2. Repository Setup

### 2.1 Folder layout decided at project start

```
SVY323-Closed-Traverse-Computation/
├── README.md
├── .gitignore              # ignores bin/, obj/, *.suo, *.user
├── src/
│   ├── gui-app/
│   │   ├── TraverseApp.vbproj   (net8.0-windows, WinExe, UseWindowsForms)
│   │   ├── Form1.vb
│   │   └── Program.vb
│   └── console-app/
│       ├── TraverseConsole.vbproj   (net8.0, Exe)
│       └── TraverseProgramme.vb
├── docs/
│   └── SVY323_Traverse_Assignment_Report.docx
└── assets/
    ├── traverse_flowchart.png
    └── gui_mockup.png
```

The `resources/fonts/` directory was added later (Inter font family for
planned future use) and is currently untracked.

### 2.2 .gitignore

Covers standard .NET build artefacts:

```
bin/
obj/
*.suo
*.user
*.vs/
.vscode/
.DS_Store
Thumbs.db
```

### 2.3 Remote

Single remote `origin` pointing to GitHub:
`https://github.com/cipher-d-dev/Tranverse-Programming-Surverying.git`

Branch strategy: **trunk-based / single `main`**. All commits went directly to
`main`. This was a solo-authored assignment project so a feature-branch
workflow was not needed.

---

## 3. Development Timeline

All work happened across two calendar days: **5 July 2026** and **13 July 2026**.

### Day 1 — 5 July 2026

#### Commit `d86150b` — 21:54 (BST)
**"feat full visual basic computational analysis on control points and relative bearing coordination"**

First working version committed. Both the console app and an early GUI were
included in the same push. The core algorithm (angular check, bearings,
DN/DE, Bowditch, coordinates, shoelace area) was present from this commit
onward. The project file targets were already set correctly (`net8.0` for
console, `net8.0-windows` with `UseWindowsForms` for GUI).

---

### Gap — 6 – 12 July

Between the initial commit and the major redesign session there was a small
fix commit:

#### Commit `03d8fcf` — 6 July 2026, 06:00 (BST)
**"fix: balance angular misclosure before bearing propagation"**

The original code propagated bearings using the raw measured angles, then
adjusted them after. This produced wrong bearings for legs 2–n because the
accumulated error was never removed before the bearing calculation. Fix:
distribute the angular correction first, then compute bearings.

---

### Day 2 — 13 July 2026 (major redesign & debug session)

All 13 commits on this day happened between 14:51 and 16:45 — roughly two
hours of intensive iteration.

| Time (BST) | Commit | Summary |
|---|---|---|
| 14:51 | `093c3da` | Fix bearing formula bug (subtraction → addition); dark-theme GUI redesign |
| 14:56 | `a4b589f` | Redesign GUI with coherent teal/slate/white design system |
| 15:02 | `458402a` | Fix font inheritance — `SetDefaultFont` + explicit `.Font` on every container |
| 15:18 | `077f3e4` | Lower minimum stations from 5 → 3; algorithm verified on triangle |
| 15:25 | `e2f209b` | Visual polish: rounded buttons, card shadows, flow alignment |
| 15:31 | `6b75e7a` | Fix rounded corners and shadows using proper subclassed controls |
| 15:38 | `8927f16` | Redesign: indigo palette, Segoe UI Variable, 4 sample datasets, Reset button |
| 16:02 | `2e4ac3b` | Complete rewrite: all build errors fixed, indigo palette confirmed |
| 16:22 | `c25b0a1` | Full clean rewrite: modern design tokens, subclassed controls solid |
| 16:26 | `f427b8d` | Fix `OverflowException` in `RoundButton.Lerp` — clamp range |
| 16:32 | `f67c25e` | Fix Lerp overflow: clamp `Double` before `CInt`, not after |
| 16:36 | `dcdeefe` | Fix `OverflowException`: drop animated Lerp entirely, use state-based hover |
| 16:45 | `fed8a6a` | Lighten grid headers to indigo-500 + white text; compute button background |

---

## 4. Build Instructions

### Lightweight (just the .NET SDK)

```powershell
# GUI
cd src\gui-app
dotnet run

# Console
cd src\console-app
dotnet run
```

### Full Visual Studio

1. VS 2019+ Community with ".NET desktop development" workload.
2. File → New → Project → Windows Forms App (Visual Basic).
3. Delete auto-generated `Form1.Designer.vb`.
4. Paste `src/gui-app/Form1.vb` into `Form1.vb`.
5. Press **F5**.

---

## 5. Envisaged Next Steps

- [ ] Add file-import support (read `.csv` / `.txt` field observations).
- [ ] Export results to `.csv` or formatted PDF.
- [ ] Add a coordinate plot panel (GDI+ traverse polygon preview).
- [ ] Ship `resources/fonts/Inter-*.ttf` properly via `PrivateFontCollection` for
      cross-machine font consistency.
- [ ] Write unit tests for `ParseDMS`, `DecimalToDMS`, and the core bearing/
      Bowditch computation routines.
