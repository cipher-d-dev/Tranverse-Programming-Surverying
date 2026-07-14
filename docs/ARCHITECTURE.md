# Architecture — SVY 323 Closed Traverse Computation Programme

---

## 1. High-Level Architecture

The project ships two independent executables that share the same
mathematical algorithm but have completely separate UIs.

```
┌───────────────────────────────────────────────────────────────┐
│                     Shared Algorithm Layer                     │
│  ParseDMS · DecimalToDMS · LegResult · Angular adjustment      │
│  Bearing propagation · Bowditch correction · Shoelace area     │
└───────────────────────────┬───────────────────────────────────┘
                            │  (duplicated — not a shared DLL)
           ┌────────────────┴─────────────────┐
           │                                  │
  ┌────────▼──────────┐             ┌─────────▼──────────┐
  │  Console App       │             │  GUI App            │
  │  TraverseProgramme │             │  Form1 (WinForms)   │
  │  .vb               │             │  + Program.vb       │
  │  net8.0 · Exe      │             │  net8.0-windows     │
  │  text output only  │             │  WinExe             │
  └───────────────────┘             └────────────────────┘
```

The algorithm was written first in the console app, verified to produce the
correct reference output, and then ported into `Form1.vb`. The two
codebases are intentionally kept separate — no shared project reference or
class library — which simplifies building and keeps each app self-contained.

---

## 2. File Map

```
SVY323-Closed-Traverse-Computation/
│
├── src/
│   ├── gui-app/
│   │   ├── TraverseApp.vbproj   ← build definition (net8.0-windows, WinExe)
│   │   ├── Program.vb           ← entry point: DPI, fonts, Application.Run
│   │   └── Form1.vb             ← entire GUI: layout, controls, computation
│   │
│   └── console-app/
│       ├── TraverseConsole.vbproj  ← build definition (net8.0, Exe)
│       └── TraverseProgramme.vb   ← Module Main: hard-coded data + 6-step print
│
├── docs/
│   ├── SVY323_Traverse_Assignment_Report.docx
│   ├── IMPLEMENTATION_PLAN.md
│   ├── ARCHITECTURE.md               ← this file
│   └── DEBUGGING_JOURNAL.md
│
├── assets/
│   ├── traverse_flowchart.png
│   └── gui_mockup.png
│
├── resources/
│   └── fonts/
│       ├── Inter-Regular.ttf         ← not yet wired up in code
│       ├── Inter-SemiBold.ttf
│       └── Inter-Bold.ttf
│
├── README.md
└── .gitignore
```

---

## 3. Console App — `TraverseProgramme.vb`

### 3.1 Structure

A single `Module TraverseProgramme` containing:

| Section | What it does |
|---------|-------------|
| **Hard-coded input data** | Station names, included angles (via `DMSToDecimal`), distances, start bearing, start N/E |
| **Step 1 — Angular check** | Sums angles, computes `(n-2)×180` theoretical, prints misclosure |
| **Step 2 — Bearings + DN/DE** | Iterates legs: back-bearing method → forward bearing → `cos`/`sin` for latitude/departure |
| **Step 3 — Linear misclosure** | `√(ΣDN² + ΣDE²)`, accuracy ratio `1 : perimeter / misclosure` |
| **Step 4 — Bowditch** | Correction per leg `= −ΣDN × (dist / perimeter)`, applied to get `AdjDN / AdjDE` |
| **Step 5 — Final coordinates** | Accumulates `AdjDN / AdjDE` from start station; fills a `Dictionary(Of String, Double)` |
| **Step 6 — Shoelace area** | `½ |Σ(E(i)·N(i+1) − E(i+1)·N(i))|` over all stations |

### 3.2 Data structure

```vb
Structure LegResult
    Public FromStation, ToStation As String
    Public IncludedAngle, Distance As Double
    Public ForwardBearing, BackBearing As Double
    Public DN, DE As Double           ' raw latitude, departure
    Public CorrDN, CorrDE As Double   ' Bowditch corrections
    Public AdjDN, AdjDE As Double     ' adjusted values
End Structure
```

A fixed-size array `legs(n-1)` is populated in a single forward pass.

### 3.3 Helpers

```vb
Function DMSToDecimal(deg, min, sec) As Double
Function DecimalToDMS(decimalDeg)    As String
```

These are the same logic as in the GUI app, just parameterised differently
(three explicit integers vs a single space-delimited string).

---

## 4. GUI App — `Form1.vb`

`Form1.vb` is a single file (~700 lines) that contains the form class, the
computation logic, and four fully custom control classes — all deliberately
kept in one file for ease of submission and presentation.

### 4.1 Design token system

At the top of `Form1.vb`, every colour and font is declared as a
`Private Shared ReadOnly` constant using a named token:

```
S_PAGE / S_CARD / S_CARD_ALT / S_HEADER   ← surface layer
A_BASE / A_HOVER / A_PRESS / A_TINT       ← indigo accent (#4F46E5 family)
T_HIGH / T_BODY / T_MUTED / T_ON_ACC      ← text hierarchy
B_DEFAULT                                  ← border colour
C_SUCCESS / C_DANGER / C_DANGER_TNT        ← semantic status
```

Font tokens follow a strict scale:

| Token | Size | Weight | Use |
|-------|------|--------|-----|
| `F_DISPLAY` | 17pt Bold | title in header |
| `F_BODY` | 9.5pt Regular | all body text |
| `F_BODY_B` | 9.5pt Bold | emphasis |
| `F_CAPTION` | 7.5pt Bold | section labels |
| `F_BTN / F_BTN_LG` | 9.5/10.5pt Bold | buttons |
| `F_STAT_K / F_STAT_V` | 7.5/11pt | stat tile key/value |
| `F_GRID_H / F_GRID_C` | 8pt Bold / 9pt Regular | grid header / cell |

### 4.2 Layout tree

```
Form1 (1920×1080, CenterScreen)
└── outer: TableLayoutPanel  (2 rows)
    ├── Row 0 [76px]  → Header Panel
    └── Row 1 [fill]  → body: TableLayoutPanel  (4 rows, 20px padding)
        ├── Row 0 [334px] → BuildInputCard  (ShadowPanel)
        │                   ├── Section label "CONTROL DATA & FIELD OBSERVATIONS"
        │                   ├── FlowLayoutPanel  (start bearing, N, E textboxes + buttons)
        │                   └── inputGrid (DataGridView — station, angle, distance)
        ├── Row 1 [58px]  → BuildComputeBar (btnCompute, full-width)
        ├── Row 2 [104px] → BuildStatsCard  (ShadowPanel → 5-column TLP → 5 stat tiles)
        └── Row 3 [fill]  → BuildResultsCard (ShadowPanel → resultsGrid DataGridView)
```

### 4.3 Custom controls

All four control classes live at the bottom of `Form1.vb`:

#### `InputField : Panel`
A `TextBox` wrapped in a custom-painted `Panel`. Draws a rounded rectangle
border that changes colour and weight (from grey to indigo, 1px → 1.6px) on
`Enter`/`Leave` focus events. The inner `TextBox` has `BorderStyle.None` so
only the panel's drawn border is visible.

#### `RoundButton : Button`
Owner-painted button with a `GraphicsPath`-based rounded rectangle. Tracks
three states (`_hover`, `_pressed`, normal) and picks the corresponding
`FillColor / HoverColor / PressColor` on each `OnPaint`. Originally used a
floating-point colour-lerp animation — replaced with simple state-based
colour switching after `OverflowException` bugs (see Debugging Journal).

#### `ShadowPanel : Panel`
Renders a card with a soft drop shadow. Draws six passes of a rounded
rectangle, each shifted by one pixel and fading in alpha (14 → ~2), before
drawing the white card fill with a hairline border. Uses
`WS_EX_TRANSPARENT` to avoid painting over siblings.

#### `ModernMenuColors : ProfessionalColorTable`
Overrides WinForms' default ugly system-blue context menu colours for the
"Sample data ▾" dropdown to match the app's white/indigo palette.

### 4.4 Computation — `ComputeTraverse_Click`

The method runs in a single `Try/Catch` block. Any input error (bad DMS
string, non-numeric distance, etc.) surfaces a `MessageBox` rather than
crashing. The algorithm is identical to the console version:

1. Read grid rows → parse DMS angles and distances.
2. Angular misclosure → distribute correction evenly across all stations.
3. Loop legs: compute forward bearing (back-bearing method), `DN = dist × cos(θ)`, `DE = dist × sin(θ)`.
4. Linear misclosure → Bowditch corrections proportional to leg length.
5. Accumulate adjusted DN/DE to get final N/E for each station.
6. Shoelace area.
7. Populate `resultsGrid` and update five stat tile labels.

### 4.5 DMS helpers

```vb
' Space-delimited "D M S" string (from TextBox) → decimal degrees
Function ParseDMS(text As String) As Double

' Decimal degrees → "DDD°MM'SS.S"" display string
Function DecimalToDMS(dd As Double) As String
```

`DecimalToDMS` includes carry logic: if `seconds ≥ 60` they are rolled into
minutes, and if `minutes ≥ 60` they are rolled into degrees. This prevents
display strings like `"45°59'60.0""` that can appear from floating-point
rounding.

### 4.6 Sample datasets

Four preloaded samples are available from the "Sample data ▾" dropdown:

| Name | Stations | Notes |
|------|----------|-------|
| Pentagon | 5 (A–E) | 108° each, ~100 m legs — default on startup |
| Triangle | 3 (P–Q–R) | ~60° equilateral, ~120 m legs |
| Quadrilateral | 4 (W–X–Y–Z) | Mixed angles, unequal legs |
| Hexagon | 6 (A–F) | Irregular, 6 mixed angles |

### 4.7 `Program.vb` — entry point

```vb
Application.SetHighDpiMode(HighDpiMode.SystemAware)
Application.EnableVisualStyles()
Application.SetCompatibleTextRenderingDefault(False)
Application.SetDefaultFont(New Font("Segoe UI", 9, Regular))
Application.Run(New Form1())
```

`SetDefaultFont` is critical — without it, controls nested inside
`TableLayoutPanel` and `FlowLayoutPanel` containers inherit the OS fallback
font (Microsoft Sans Serif 8.25pt on older Windows builds) instead of
Segoe UI, because Windows Forms ambient font propagation only reaches direct
children of the `Form`.

---

## 5. Key Design Decisions

### 5.1 Console-first, GUI-second
Writing the console app first provided a fast, noise-free loop for getting
the maths right. The console output is a direct printout of each step — you
can read the intermediate values without clicking anything. Once the
reference output matched expected values (`1:17,183` accuracy, `1.72 ha`
area on the sample dataset), the same algorithm was embedded in the GUI's
`ComputeTraverse_Click` handler.

### 5.2 No Designer file
`Form1.Designer.vb` is intentionally absent. The entire UI is built
programmatically in `BuildUI()` and its sub-methods. This avoids
Designer-generated code that breaks when the `.vb` file is copied outside
Visual Studio, and makes every pixel of the layout visible in the source
file without switching windows.

### 5.3 Single-file GUI
All form logic and custom controls are in `Form1.vb`. For an assignment
project this is fine — there is no production maintenance concern, and a
single file is easier to paste into the report appendix.

### 5.4 `LegResult` as a `Structure`
`LegResult` is a value type (`Structure`, not `Class`). Array of structures
allocates contiguously and is perfectly adequate for a traverse of a few
hundred stations. No heap pressure, no null references.

### 5.5 Indigo design system
The final colour scheme (`#4F46E5` indigo base, `#F8F8FA` page background,
white cards) was chosen after several prototype themes (dark navy, teal,
mixed-colour buttons) were tried and discarded. The single-accent approach
with a strict 3-layer surface hierarchy gives a clean, professional look
without visual noise.

---

## 6. Envisaged Extensions

| Feature | Where it fits |
|---------|--------------|
| File import (CSV/TXT) | New `LoadFromFile` method in `Form1.vb`; console equivalent via `File.ReadAllLines` |
| Export to CSV | Iterate `resultsGrid.Rows`, write with `StreamWriter` |
| Coordinate plot | New `Panel` below or beside results grid; GDI+ `DrawPolygon` using `finalN/E` |
| `PrivateFontCollection` for Inter fonts | `resources/fonts/` already in place; add loader in `Program.vb` |
| Shared algorithm library | Extract `LegResult` + computation into a `net8.0` class library referenced by both apps |
