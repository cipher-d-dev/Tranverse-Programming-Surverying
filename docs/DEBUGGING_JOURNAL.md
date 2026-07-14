# Debugging Journal — SVY 323 Closed Traverse Computation Programme

This journal documents the bugs encountered during development, how each was
found, and the fix applied. Entries follow the git commit history in
chronological order.

---

## Strategy: Console Logger → GUI

The core debugging strategy for this project was **console-first validation**.
The algorithm was written and exercised entirely in the console app
(`TraverseProgramme.vb`) before any GUI code was touched. The console app
acts as a persistent "test log" — every intermediate value (angular
misclosure, each forward bearing, every DN/DE, Bowditch correction, final
coordinate) is printed to `stdout` at the step that produces it. There is no
separate logger class; the `Console.WriteLine` calls *are* the logger.

This means:
- A wrong answer in the GUI is immediately reproducible in the console by
  running `dotnet run` in `src/console-app/` — no clicking required.
- Each step's output is visible simultaneously, so the exact step where the
  error enters the chain is obvious by inspection.
- The console run is the **reference output**. Once it produces the expected
  values (`1:17,183` linear accuracy, `1.7205 ha` area on the standard
  5-station pentagon dataset), every GUI result is compared against it.

When a bug surfaced in the GUI that was not present in the console:
1. Check whether the GUI is calling the same formula as the console.
2. If the formula matches, isolate the UI layer — is it a parsing issue?
   A display issue? A state/event issue?
3. Fix the minimal amount needed, commit, re-run.

---

## Bug Log

---

### Bug 1 — Angular misclosure applied after bearing propagation

**Commit:** `03d8fcf` — 6 July 2026, 06:00 BST  
**Symptom:** Final coordinates and area were wrong; linear misclosure was
large even for a geometrically perfect regular polygon input.  
**Root cause:** The initial version computed bearings using the raw measured
angles, then applied the angular correction as a post-step. Because each
forward bearing is derived from the previous one (back-bearing method), an
uncorrected angle at station 2 cascades an error into every subsequent
bearing.

**How found:** Console output showed that the "angular misclosure" step
printed the correct theoretical sum, but the DN and DE values immediately
below it were inconsistent with what a manual calculation for the pentagon
sample would give. Tracing back, the `corrPerStn` variable was computed but
never applied to `includedAngles(i)` before the bearing loop.

**Fix:**
```vb
' Before the fix — angles used raw:
For i = 0 To n - 1
    ' ... used includedAngles(i) directly in bearing calc
Next

' After the fix — distribute correction first, then compute bearings:
Dim corrPerStn As Double = -angMisclos / n
For i = 0 To n - 1
    includedAngles(i) += corrPerStn
Next
' ... then the bearing loop
```

**Impact:** All subsequent calculations (DN/DE, Bowditch, coordinates, area)
are now correct.

---

### Bug 2 — Forward bearing formula: subtraction instead of addition

**Commit:** `093c3da` — 13 July 2026, 14:51 BST  
**Symptom:** All bearings from leg 2 onward were wrong; the errors were large
(tens of degrees off) and obviously systematic.  
**Root cause:** The interior-angle traverse requires:

```
Forward bearing (i) = Back bearing (i-1) + Interior angle (i)
```

The original code used subtraction instead of addition:

```vb
' Wrong:
fb = (bbPrev - includedAngles(i)) Mod 360.0

' Correct:
fb = (bbPrev + includedAngles(i)) Mod 360.0
```

For the pentagon sample (108° interior angles), subtraction produces
bearings that decrease around the traverse instead of cycling correctly
through the quadrants.

**How found:** Console output from Step 2 showed the A–B bearing was
correct (it came from the hard-coded start bearing of 60°), but B–C was
clearly wrong. Manually computing B–C on paper (back bearing of A–B = 240°,
+ 108° = 348°) and comparing to the console output immediately identified
the subtraction.

**Fix:** Changed `−` to `+` in `Form1.vb` and in the console app for
consistency. Both apps were updated in the same commit.

**Verification note (from commit message):**
> "Verified: console app produces 1:17183 accuracy and 1.7205 ha area
> matching the README sample dataset specification."

---

### Bug 3 — `DecimalToDMS` producing seconds ≥ 60

**Commit:** `093c3da` — 13 July 2026, 14:51 BST (same commit as Bug 2)  
**Symptom:** Some DMS display strings showed `"45°59'60.0""` or similar
values where seconds had rolled over past 59.

**Root cause:** Floating-point arithmetic. When computing:
```vb
Dim mFull As Double = (av - deg) * 60.0
Dim secs  As Double = (mFull - mins) * 60.0
```
rounding errors occasionally push `secs` to exactly `60.0` or a hair above.

**Fix:** Added carry logic after computing `secs`:
```vb
If secs >= 60.0 Then
    secs = 0
    mins += 1
End If
If mins >= 60 Then
    mins = 0
    deg += 1
End If
```

---

### Bug 4 — Font inheritance not propagating to nested containers

**Commit:** `458402a` — 13 July 2026, 15:02 BST  
**Symptom:** Some labels, stat tiles, and grid headers rendered in Microsoft
Sans Serif 8.25pt (the Windows OS fallback font) instead of Segoe UI.  
**Root cause:** Windows Forms' ambient font property only propagates to
**direct children** of the `Form`. Controls nested inside
`TableLayoutPanel` and `FlowLayoutPanel` containers need those containers to
also have `.Font` set explicitly, or they will fall through to the system
default.

**How found:** Visible immediately on running the GUI — some text was
clearly a different typeface. Setting a breakpoint on `Form1.New` and
inspecting `.Font` on a deeply nested `Label` showed it was `Microsoft Sans
Serif` rather than `Segoe UI`.

**Fix (two-part):**

Part 1 — `Program.vb`: call `Application.SetDefaultFont` before
`Application.Run` so the process-wide default is correct before any control
is constructed:
```vb
Application.SetDefaultFont(New Font("Segoe UI", 9, FontStyle.Regular))
```

Part 2 — `Form1.vb`: explicitly assign `.Font = F_BODY` on every
intermediate container (`outer` TLP, `body` TLP, header `Panel`, `cardInput`,
`flowCtrl`, `pnlCompute`, `cardStats`, `statFlow` TLP, `cardResults`, stat
cell panels) so that inheritance works at every nesting level.

---

### Bug 5 — `OverflowException` in `RoundButton` colour animation (3 commits)

**Commits:**
- `f427b8d` — 16:26 BST  "Fix OverflowException in RoundButton.Lerp — clamp t and channels to valid range"
- `f67c25e` — 16:32 BST  "Fix Lerp overflow: clamp Double before CInt, not after"
- `dcdeefe` — 16:36 BST  "Fix OverflowException: replace animated Lerp with simple state-based hover in RoundButton"

**Symptom:** The app crashed with `System.OverflowException: Arithmetic
operation resulted in an overflow` the moment the mouse moved over any
`RoundButton`.

**Root cause (evolving diagnosis across three commits):**

The original `RoundButton` used a `System.Windows.Forms.Timer` and a
floating-point linear interpolation (`Lerp`) to animate hover transitions:

```vb
Private Function Lerp(a As Integer, b As Integer, t As Double) As Integer
    Return CInt(a + (b - a) * t)
End Function
```

When `t` approached 1.0 due to floating-point representation, the expression
`a + (b - a) * t` could evaluate to a value slightly above `Integer.MaxValue`
before the `CInt` cast tried to truncate it, causing the overflow.

**Attempt 1 (`f427b8d`) — clamp before and after:**
```vb
t = Math.Max(0.0, Math.Min(1.0, t))
Return CInt(Math.Max(0, Math.Min(255, CInt(a + (b - a) * t))))
```
Still crashed — the problem was that individual R/G/B channel lerp values
could overflow before the outer `Math.Min(255, ...)` clamp ran.

**Attempt 2 (`f67c25e`) — clamp the Double result before CInt:**
```vb
Dim result As Double = a + (b - a) * t
Return CInt(Math.Max(-2147483648.0, Math.Min(2147483647.0, result)))
```
Still crashed in practice — timer firing at high frequency meant `_animT`
could skip past the guard in a race.

**Root fix (`dcdeefe`) — drop the animation entirely:**  
The lerp + timer approach added complexity that was not worth the visual
payoff for an academic presentation. Replaced with simple state-based colour
selection in `OnPaint`:

```vb
Dim bg As Color
If _pressed Then
    bg = PressColor
ElseIf _hover Then
    bg = HoverColor
Else
    bg = FillColor
End If
```

`OnMouseEnter`, `OnMouseLeave`, `OnMouseDown`, `OnMouseUp` each set the
relevant boolean and call `Invalidate()`. No timer, no arithmetic, no
overflow possible.

---

### Bug 6 — Rounded corners and shadows missing / clipping children

**Commits:**
- `e2f209b` — 15:25 BST  "Visual polish: kill button outlines, rounded compute btn, card shadows, fix flow alignment"
- `6b75e7a` — 15:31 BST  "Fix rounded corners and card shadows using proper subclassed controls"

**Symptom:** The card panels showed square corners despite custom `OnPaint`
code that drew rounded rectangles. Children of the panel painted over the
rounded corners, making the rounding invisible.

**Root cause:** Standard `Panel` controls do not clip their children to a
custom painted shape. A child control sitting in the corner of the panel
paints over whatever the panel draws — including the rounded-corner fill.

**Fix:** Implemented `ShadowPanel` as a proper subclassed control with:
- `ControlStyles.OptimizedDoubleBuffer` to avoid flicker.
- `WS_EX_TRANSPARENT` in `CreateParams` so the parent background shows
  through the shadow region (the area between the card edge and the outer
  shadow rect).
- All child controls are padded inward by `SHADOW_DEPTH` (6px) and
  `CORNER_RADIUS` (14px) so they never reach the painted corner arcs.

The `InputField` rounded border uses the same `GraphicsPath`-based approach
on a `Panel` wrapper, but in that case the only child (`TextBox`) is docked
to fill inside the padding and never reaches the corners.

---

### Bug 7 — Minimum station guard set to 5, algorithm valid from 3

**Commit:** `077f3e4` — 13 July 2026, 15:18 BST  
**Symptom:** The "Load sample — Triangle (3 stations)" dataset triggered the
"Please enter at least 5 stations" error dialog immediately on computing.

**Root cause:** The guard was copied from the assignment brief which specifies
a minimum of 5 for the submitted dataset. Mathematically, a closed traverse
requires only 3 stations (a triangle is the degenerate closed traverse).

**Fix:**
```vb
' Before:
If n < 5 Then ...

' After:
If n < 3 Then
    MessageBox.Show("Please enter at least 3 stations (minimum for a closed traverse).")
```

The triangle sample dataset was then used to verify the algorithm on a
known-answer case: equilateral triangle, 60° angles, ~120 m sides.

---

### Bug 8 — Grid header colour too dark (indigo-900 → indigo-500)

**Commit:** `fed8a6a` — 13 July 2026, 16:45 BST  
**Symptom:** Grid column headers and the Compute button background appeared
nearly black — the header text was illegible against the very dark fill.

**Root cause:** Initial colour value for `ColumnHeadersDefaultCellStyle.BackColor`
was `Color.FromArgb(55, 48, 163)` (indigo-800/900). The white text had
insufficient contrast and the visual weight was too heavy compared to the
surrounding white cards.

**Fix:** Lightened the grid header to indigo-500 (`Color.FromArgb(99, 102, 241)`)
and confirmed white text at this weight has adequate contrast:

```vb
gv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(99, 102, 241)  ' indigo-500
gv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
```

---

## Summary Table

| # | Bug | Detection method | Fix approach | Commit |
|---|-----|-----------------|-------------|--------|
| 1 | Angular correction applied after bearing loop | Console step output mismatched manual calc | Move correction before bearing loop | `03d8fcf` |
| 2 | Bearing formula used subtraction | Console bearing output vs manual calculation | Change `−` to `+` in bearing formula | `093c3da` |
| 3 | `DecimalToDMS` produces `secs ≥ 60` | Visual inspection of DMS display strings | Add carry logic for min/sec rollover | `093c3da` |
| 4 | Font not inheriting in nested containers | Visual inspection — wrong typeface visible | `SetDefaultFont` + explicit `.Font` on containers | `458402a` |
| 5 | `OverflowException` in `RoundButton` Lerp | Runtime crash on mouse hover | Drop animation; use state-based colour selection | `dcdeefe` |
| 6 | Rounded card corners clipped by children | Visual inspection — corners appeared square | Subclass `ShadowPanel`; pad children inside | `6b75e7a` |
| 7 | Min station guard at 5 (should be 3) | Triangle sample triggered incorrect error dialog | Lower guard to 3; add triangle test case | `077f3e4` |
| 8 | Grid headers too dark, low contrast | Visual inspection — header text near-invisible | Lighten to indigo-500 | `fed8a6a` |
