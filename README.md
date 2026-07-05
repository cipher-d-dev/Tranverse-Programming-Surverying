# SVY 323 — Closed Traverse Computation Programme

A VB.NET programme (Windows Forms GUI + a plain console version) that performs a
complete closed-traverse computation for Surveying: forward/back bearings,
latitudes & departures, Bowditch (compass rule) adjustment, final station
coordinates, linear accuracy, and the enclosed area of the traverse polygon.

Built for the SVY 323 (Computer Application I) group assignment.

---

## What this does

Given the included angles and leg distances of a closed traverse (minimum 5
stations), plus a starting bearing and starting coordinates from an initial
control station, the programme computes:

1. **Angular misclosure** — sum of included angles vs. the theoretical `(n-2) x 180°`
2. **Forward & back bearings** for every leg (back-bearing method)
3. **Latitude (DN)** and **Departure (DE)** for every leg
4. **Linear misclosure** and **linear accuracy** (expressed as `1 : X`)
5. **Bowditch (Compass Rule) corrections**, distributed in proportion to leg length
6. **Adjusted DN/DE** and **Final Northings & Eastings** of every station
7. **Area** of the closed traverse polygon, by the coordinate (shoelace) method

## Repository structure

```
SVY323-Closed-Traverse-Computation/
├── README.md
├── src/
│   ├── gui-app/
│   │   ├── TraverseApp.vbproj    # project file (builds with just the .NET SDK)
│   │   ├── Form1.vb              # Windows Forms (GUI) version — used for the presentation
│   │   └── Program.vb            # small entry point (Visual Studio normally auto-generates this)
│   └── console-app/
│       ├── TraverseConsole.vbproj
│       └── TraverseProgramme.vb  # plain console version, same logic, text output only
├── docs/
│   └── SVY323_Traverse_Assignment_Report.docx   # Full assignment report
└── assets/
    ├── traverse_flowchart.png
    └── gui_mockup.png
```

## Getting started

You do **not** need the full Visual Studio IDE or the 8+ GB ".NET desktop
development" workload. Everything here can be built with just the free
**.NET SDK**, which is a much smaller install.

### Lightweight setup (recommended if you're short on disk space)

1. Download and install the **.NET SDK** (not Visual Studio) from
   https://dotnet.microsoft.com/download — this is typically a ~300 MB
   download and about 1 GB installed, vs. several GB for Visual Studio's
   desktop workload.
2. Open a terminal (Command Prompt / PowerShell / Windows Terminal) in the
   project folder.
3. To build and run the **GUI version**:
   ```
   cd src/gui-app
   dotnet run
   ```
4. To build and run the **console version**:
   ```
   cd src/console-app
   dotnet run
   ```

Any free lightweight editor (VS Code, Notepad++, or even Notepad) is enough
to edit the `.vb` files — you don't need Visual Studio to write or tweak the
code, only the SDK to compile and run it.

> **Note:** Windows Forms only runs on Windows. If you're developing on a
> Mac or Linux machine, you'll need to copy this folder onto a Windows PC
> (e.g. a lab computer) to build/run the GUI version — the console version
> has the same limitation, since Windows Forms and the VB runtime used here
> target Windows.

### Full Visual Studio setup (if you have the disk space / a lab PC)

1. Install **Visual Studio 2019+ Community Edition** with the **".NET desktop
   development"** workload (free, from visualstudio.microsoft.com).
2. **File → New → Project → Windows Forms App** (choose **Visual Basic** as
   the language).
3. In Solution Explorer, **delete** the auto-generated `Form1.Designer.vb` —
   it isn't needed, since the whole interface is built in code.
4. Open `Form1.vb` and replace its entire contents with `src/gui-app/Form1.vb`.
5. Press **F5** to run.

Either way, the app opens with an editable input grid pre-loaded with sample
data for 5 stations (A–B–C–D–E). Edit the grid with your group's real field
data (or click **Load Sample Data** to reset it), then click **Compute
Traverse** to see bearings, DN/DE, corrections and final coordinates fill the
results grid, with a summary of misclosure, accuracy and area above it.

## Sample dataset

Both versions ship with the same demonstration dataset: a 5-station closed
traverse (A→B→C→D→E→A), each included angle 108°00'00", leg lengths close to
100 m, starting bearing 60°00'00" and starting coordinates N=1000.000,
E=1000.000. This dataset intentionally includes small measurement variations
so the Bowditch adjustment and linear-accuracy calculation have something to
correct — closing with a linear accuracy of about **1:17,183** and an
enclosed area of about **1.72 hectares**.

Replace this sample data with your group's actual field observations before
final submission/presentation.

## Report

`docs/SVY323_Traverse_Assignment_Report.docx` contains the full written
report required by the assignment brief: Introduction, Literature Review,
Methodology (algorithm + flowchart + GUI mock-up), full programme code,
input data, results/output, and Conclusion/Recommendation. Open it in Word
and:
- Fill in your group name, member names, and submission date on the cover page.
- Right-click the Table of Contents and choose **Update Field** to populate it.
- Replace the sample results with your own group's computed results if you
  use different field data.

## License

Prepared for academic coursework (SVY 323). Free to reuse and adapt for
educational purposes.
