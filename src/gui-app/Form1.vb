'==================================================================================
'  SVY 323 — COMPUTER APPLICATION I
'  Closed Traverse Computation — Windows Forms GUI
'  Design: clean light surface / dark slate sidebar-style header, single teal
'          accent, Segoe UI, no scrollbars at default size.
'==================================================================================
Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Collections.Generic

Public Class Form1
    Inherits Form

    ' =========================================================================
    '  DESIGN TOKENS  — 60-30-10 rule applied
    '  60 % → Slate-50 surfaces (dominant neutral)
    '  30 % → Slate-800 structural chrome (header, grid headers, primary text)
    '  10 % → Indigo-600 accent (buttons, links, highlights only)
    ' =========================================================================
    ' Surfaces
    Private Shared ReadOnly S_PAGE    As Color = Color.FromArgb(248, 250, 252)  ' slate-50
    Private Shared ReadOnly S_CARD    As Color = Color.FromArgb(255, 255, 255)  ' white
    Private Shared ReadOnly S_HEADER  As Color = Color.FromArgb( 15,  23,  42)  ' slate-900
    Private Shared ReadOnly S_INPUT   As Color = Color.FromArgb(249, 250, 251)  ' gray-50
    ' Accent — Indigo-600, single hue, used for every interactive element
    Private Shared ReadOnly A_BASE    As Color = Color.FromArgb( 79,  70, 229)  ' indigo-600
    Private Shared ReadOnly A_HOVER   As Color = Color.FromArgb( 67,  56, 202)  ' indigo-700
    Private Shared ReadOnly A_PRESS   As Color = Color.FromArgb( 55,  48, 163)  ' indigo-800
    Private Shared ReadOnly A_TINT    As Color = Color.FromArgb(238, 242, 255)  ' indigo-50  (row alt)
    ' Text
    Private Shared ReadOnly T_HIGH    As Color = Color.FromArgb( 15,  23,  42)  ' slate-900 — headings
    Private Shared ReadOnly T_BODY    As Color = Color.FromArgb( 51,  65,  85)  ' slate-700 — body
    Private Shared ReadOnly T_MUTED   As Color = Color.FromArgb(100, 116, 139)  ' slate-500 — captions
    Private Shared ReadOnly T_ON_DARK As Color = Color.FromArgb(241, 245, 249)  ' slate-100 — on dark bg
    Private Shared ReadOnly T_ON_ACC  As Color = Color.White
    ' Borders
    Private Shared ReadOnly B_DEFAULT As Color = Color.FromArgb(226, 232, 240)  ' slate-200
    Private Shared ReadOnly B_STRONG  As Color = Color.FromArgb(203, 213, 225)  ' slate-300

    ' =========================================================================
    '  TYPOGRAPHY  — Segoe UI Variable (Win 11 system font); falls back to
    '  Segoe UI on Win 10.  Strict scale: Display → Body → Caption.
    ' =========================================================================
    Private Shared ReadOnly FACE As String = "Segoe UI Variable"   ' Win11 variable font
    Private Shared ReadOnly FACE_MONO As String = "Cascadia Mono"  ' Win11 mono; falls back gracefully

    Private Shared ReadOnly F_DISPLAY As New Font(FACE, 16, FontStyle.Bold)     ' app title
    Private Shared ReadOnly F_H2      As New Font(FACE, 11, FontStyle.Bold)     ' section headings
    Private Shared ReadOnly F_BODY    As New Font(FACE,  9, FontStyle.Regular)  ' default body
    Private Shared ReadOnly F_BODY_B  As New Font(FACE,  9, FontStyle.Bold)     ' body emphasis
    Private Shared ReadOnly F_CAPTION As New Font(FACE,  8, FontStyle.Regular)  ' hints / captions
    Private Shared ReadOnly F_BTN     As New Font(FACE,  9, FontStyle.Bold)     ' button labels
    Private Shared ReadOnly F_STAT_K  As New Font(FACE,  8, FontStyle.Regular)  ' stat tile key
    Private Shared ReadOnly F_STAT_V  As New Font(FACE, 13, FontStyle.Bold)     ' stat tile value
    Private Shared ReadOnly F_GRID_H  As New Font(FACE,  8, FontStyle.Bold)     ' grid header
    Private Shared ReadOnly F_GRID_C  As New Font(FACE_MONO, 9, FontStyle.Regular) ' grid cell

    ' Colour aliases — map old names to new tokens so no code below breaks
    Private Shared ReadOnly A_MID     As Color = Color.FromArgb( 79,  70, 229)  ' = A_BASE
    Private Shared ReadOnly A_DARK    As Color = Color.FromArgb( 67,  56, 202)  ' = A_HOVER
    Private Shared ReadOnly A_LIGHT   As Color = Color.FromArgb(238, 242, 255)  ' = A_TINT
    Private Shared ReadOnly T_PRIMARY As Color = Color.FromArgb( 51,  65,  85)  ' = T_BODY
    Private Shared ReadOnly T_ONSLATE As Color = Color.FromArgb(241, 245, 249)  ' = T_ON_DARK
    Private Shared ReadOnly T_ONACCNT As Color = Color.White
    Private Shared ReadOnly B_SUBTLE  As Color = Color.FromArgb(226, 232, 240)  ' = B_DEFAULT

    ' Font aliases — keep old names alive so no code below breaks
    Private Shared ReadOnly F_LABEL   As Font = F_BODY
    Private Shared ReadOnly F_INPUT   As Font = F_BODY
    Private Shared ReadOnly F_SECTION As Font = F_CAPTION
    Private Shared ReadOnly F_TITLE   As Font = F_DISPLAY
    Private Shared ReadOnly F_SUB     As Font = F_BODY

    ' =========================================================================
    '  CONTROLS
    ' =========================================================================
    Private WithEvents btnCompute    As RoundButton
    Private WithEvents btnLoadSample As Button
    Private WithEvents btnAddRow     As Button
    Private WithEvents btnRemoveRow  As Button
    Private WithEvents btnReset      As Button
    Private WithEvents btnSampleMenu As Button
    Private sampleMenu               As ContextMenuStrip

    Private inputGrid   As DataGridView
    Private resultsGrid As DataGridView

    Private txtStartBearing As TextBox
    Private txtStartN       As TextBox
    Private txtStartE       As TextBox

    ' Stat value labels (the bold numbers that update after compute)
    Private lblAngularVal  As Label
    Private lblMisclosVal  As Label
    Private lblLinearVal   As Label
    Private lblAccuracyVal As Label
    Private lblAreaVal     As Label

    ' =========================================================================
    '  DATA STRUCTURE
    ' =========================================================================
    Structure LegResult
        Public FromStation    As String
        Public ToStation      As String
        Public IncludedAngle  As Double
        Public Distance       As Double
        Public ForwardBearing As Double
        Public BackBearing    As Double
        Public DN As Double,  DE As Double
        Public CorrDN As Double, CorrDE As Double
        Public AdjDN As Double,  AdjDE As Double
        Public FinalN As Double, FinalE As Double
    End Structure

    ' =========================================================================
    '  CONSTRUCTOR
    ' =========================================================================
    Public Sub New()
        Me.SuspendLayout()
        BuildUI()
        Me.ResumeLayout(False)
        Me.PerformLayout()
        LoadSampleData()
    End Sub

    ' =========================================================================
    '  BUILD UI  — single method, TableLayoutPanel drives all proportions
    ' =========================================================================
    Private Sub BuildUI()
        ' ── Form shell ───────────────────────────────────────────────────────
        Me.Text          = "SVY 323  ·  Closed Traverse Computation"
        Me.ClientSize    = New Size(1280, 820)
        Me.MinimumSize   = New Size(1100, 720)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor     = S_PAGE
        ' Set font on the form AND immediately propagate to all children
        Me.Font          = F_LABEL

        ' ── Outer TableLayout: header row (fixed) + body row (fills) ─────────
        Dim outer As New TableLayoutPanel With {
            .Dock        = DockStyle.Fill,
            .RowCount    = 2,
            .ColumnCount = 1,
            .Padding     = New Padding(0),
            .Margin      = New Padding(0),
            .BackColor   = S_PAGE,
            .Font        = F_LABEL
        }
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 74))   ' header
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100))   ' body
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        Me.Controls.Add(outer)

        ' ── Header ───────────────────────────────────────────────────────────
        Dim header As New Panel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_HEADER,
            .Padding   = New Padding(24, 0, 0, 0),
            .Font      = F_LABEL
        }
        AddHandler header.Paint, AddressOf OnHeaderPaint
        outer.Controls.Add(header, 0, 0)

        header.Controls.Add(New Label With {
            .Text      = "SVY 323  —  Closed Traverse Computation",
            .Font      = F_DISPLAY,
            .ForeColor = T_ON_DARK,
            .AutoSize  = True,
            .Left      = 24,
            .Top       = 12
        })
        header.Controls.Add(New Label With {
            .Text      = "Angular Adjustment  ·  Bowditch (Compass Rule)  ·  Bearings  ·  Latitudes & Departures  ·  Final Coordinates  ·  Area",
            .Font      = F_BODY,
            .ForeColor = T_MUTED,
            .AutoSize  = True,
            .Left      = 26,
            .Top       = 44
        })

        ' ── Body TableLayout: input section + compute bar + stats + results ──
        Dim body As New TableLayoutPanel With {
            .Dock        = DockStyle.Fill,
            .RowCount    = 4,
            .ColumnCount = 1,
            .Padding     = New Padding(16, 12, 16, 12),
            .BackColor   = S_PAGE,
            .Font        = F_LABEL
        }
        body.RowStyles.Add(New RowStyle(SizeType.Absolute, 220))   ' input section
        body.RowStyles.Add(New RowStyle(SizeType.Absolute,  54))   ' compute button
        body.RowStyles.Add(New RowStyle(SizeType.Absolute, 100))   ' stats bar
        body.RowStyles.Add(New RowStyle(SizeType.Percent,  100))   ' results (fills)
        body.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        outer.Controls.Add(body, 0, 1)

        ' ── ROW 0 : Input section ─────────────────────────────────────────────
        Dim cardInput As Panel = MakeCard()
        cardInput.Dock    = DockStyle.Fill
        cardInput.Padding = New Padding(20, 14, 20, 14)
        cardInput.Font    = F_LABEL
        body.Controls.Add(cardInput, 0, 0)

        ' Section label
        cardInput.Controls.Add(MakeSectionLabel("CONTROL DATA  &  FIELD OBSERVATIONS", 0, 0))

        ' Control-data row: bearing, N, E, buttons  (use a FlowLayoutPanel for clean spacing)
        Dim flowCtrl As New FlowLayoutPanel With {
            .FlowDirection = FlowDirection.LeftToRight,
            .AutoSize      = True,
            .Left          = 0,
            .Top           = 26,
            .Height        = 32,
            .WrapContents  = False,
            .BackColor     = Color.Transparent,
            .Font          = F_LABEL
        }
        flowCtrl.Controls.Add(MakeFieldLabel("Start Bearing (D M S)"))
        txtStartBearing = MakeInput("60 0 0", 88)
        flowCtrl.Controls.Add(txtStartBearing)
        flowCtrl.Controls.Add(MakeSpacer(20))
        flowCtrl.Controls.Add(MakeFieldLabel("Start Northing (m)"))
        txtStartN = MakeInput("1000.000", 96)
        flowCtrl.Controls.Add(txtStartN)
        flowCtrl.Controls.Add(MakeSpacer(20))
        flowCtrl.Controls.Add(MakeFieldLabel("Start Easting (m)"))
        txtStartE = MakeInput("1000.000", 96)
        flowCtrl.Controls.Add(txtStartE)
        flowCtrl.Controls.Add(MakeSpacer(32))
        btnLoadSample = MakeBtn("↺  Sample Data ▾", A_MID, 138, 36)
        btnAddRow     = MakeBtn("+  Add Row",        A_MID, 100, 36)
        btnRemoveRow  = MakeBtn("−  Remove Row",     Color.FromArgb(71, 85, 105), 110, 36)
        btnReset      = MakeBtn("⊘  Reset",          Color.FromArgb(71, 85, 105), 84,  36)
        flowCtrl.Controls.Add(btnLoadSample)
        flowCtrl.Controls.Add(MakeSpacer(8))
        flowCtrl.Controls.Add(btnAddRow)
        flowCtrl.Controls.Add(MakeSpacer(6))
        flowCtrl.Controls.Add(btnRemoveRow)
        flowCtrl.Controls.Add(MakeSpacer(6))
        flowCtrl.Controls.Add(btnReset)

        ' Build the sample-data dropdown menu
        sampleMenu = New ContextMenuStrip()
        sampleMenu.Font = F_BODY
        sampleMenu.Items.Add("Pentagon  (5 equal stations, 108° each)",     Nothing, AddressOf LoadSample_Pentagon)
        sampleMenu.Items.Add("Triangle  (3 stations, equilateral ~60°)",    Nothing, AddressOf LoadSample_Triangle)
        sampleMenu.Items.Add("Quadrilateral  (4 stations, mixed angles)",   Nothing, AddressOf LoadSample_Quad)
        sampleMenu.Items.Add("Hexagon  (6 stations, irregular distances)",  Nothing, AddressOf LoadSample_Hex)
        cardInput.Controls.Add(flowCtrl)

        ' Hint text
        Dim hint As New Label With {
            .Text      = "Enter station name, included angle (degrees minutes seconds, space-separated) and distance to next station in metres.  Minimum 3 rows.",
            .Font      = New Font("Segoe UI", 8, FontStyle.Regular),
            .ForeColor = T_MUTED,
            .AutoSize  = False,
            .Height    = 16,
            .Width     = 900,
            .Left      = 0,
            .Top       = 58
        }
        cardInput.Controls.Add(hint)

        ' Input grid
        inputGrid = New DataGridView With {
            .Left                = 0,
            .Top                 = 78,
            .Height              = 106,
            .Anchor              = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows  = False,
            .RowHeadersVisible   = False,
            .BorderStyle         = BorderStyle.None,
            .BackgroundColor     = S_CARD,
            .GridColor           = B_SUBTLE,
            .SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
            .ColumnHeadersHeight = 30,
            .Font                = F_INPUT,
            .ScrollBars          = ScrollBars.Vertical
        }
        inputGrid.RowTemplate.Height = 30
        ApplyGridStyle(inputGrid)
        inputGrid.Columns.Add("col_stn",  "Station")
        inputGrid.Columns.Add("col_ang",  "Included Angle  (D  M  S)")
        inputGrid.Columns.Add("col_dist", "Distance to Next Station (m)")
        inputGrid.Columns(0).Width = 160
        inputGrid.Columns(1).Width = 240
        inputGrid.Columns(2).FillWeight = 100
        inputGrid.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        cardInput.Controls.Add(inputGrid)

        ' ── ROW 1 : Compute button ────────────────────────────────────────────
        Dim pnlCompute As New Panel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_PAGE,
            .Padding   = New Padding(0, 8, 0, 8),
            .Font      = F_LABEL
        }
        body.Controls.Add(pnlCompute, 0, 1)

        btnCompute = New RoundButton With {
            .Text       = "▶   COMPUTE TRAVERSE",
            .Dock       = DockStyle.Fill,
            .Font       = New Font(FACE, 11, FontStyle.Bold),
            .BackColor  = A_BASE,
            .ForeColor  = T_ON_ACC,
            .HoverColor = A_HOVER,
            .Cursor     = Cursors.Hand,
            .TabStop    = False
        }
        pnlCompute.Controls.Add(btnCompute)

        ' ── ROW 2 : Stats bar (5 tiles) ───────────────────────────────────────
        Dim cardStats As Panel = MakeCard()
        cardStats.Dock    = DockStyle.Fill
        cardStats.Padding = New Padding(0)
        cardStats.Font    = F_LABEL
        body.Controls.Add(cardStats, 0, 2)

        Dim statFlow As New TableLayoutPanel With {
            .Dock            = DockStyle.Fill,
            .ColumnCount     = 5,
            .RowCount        = 1,
            .BackColor       = Color.Transparent,
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            .Font            = F_LABEL
        }
        For i As Integer = 0 To 4
            statFlow.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
        Next
        statFlow.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        cardStats.Controls.Add(statFlow)

        lblAngularVal  = AddStatTile(statFlow, 0, "Sum of Angles",       "—")
        lblMisclosVal  = AddStatTile(statFlow, 1, "Angular Misclosure",  "—")
        lblLinearVal   = AddStatTile(statFlow, 2, "Linear Misclosure",   "—")
        lblAccuracyVal = AddStatTile(statFlow, 3, "Linear Accuracy",     "—")
        lblAreaVal     = AddStatTile(statFlow, 4, "Enclosed Area",       "—")
        lblAreaVal.ForeColor = A_BASE   ' highlight the area tile value

        ' ── ROW 3 : Results grid ──────────────────────────────────────────────
        Dim cardResults As Panel = MakeCard()
        cardResults.Dock    = DockStyle.Fill
        cardResults.Padding = New Padding(0)
        cardResults.Font    = F_LABEL
        body.Controls.Add(cardResults, 0, 3)

        Dim lblResHdr As New Label With {
            .Text      = "COMPUTATION RESULTS",
            .Font      = F_SECTION,
            .ForeColor = T_MUTED,
            .AutoSize  = True,
            .Left      = 12,
            .Top       = 8
        }
        cardResults.Controls.Add(lblResHdr)

        resultsGrid = New DataGridView With {
            .Left                = 0,
            .Top                 = 28,
            .Anchor              = AnchorStyles.Top Or AnchorStyles.Bottom Or
                                   AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows  = False,
            .ReadOnly            = True,
            .RowHeadersVisible   = False,
            .BorderStyle         = BorderStyle.None,
            .BackgroundColor     = S_CARD,
            .GridColor           = B_SUBTLE,
            .SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .ColumnHeadersHeight = 32,
            .Font                = F_GRID_C,
            .ScrollBars          = ScrollBars.Both
        }
        resultsGrid.RowTemplate.Height = 28
        ApplyGridStyle(resultsGrid)

        Dim rCols() As String = {
            "Leg", "Angle", "Dist (m)",
            "Fwd Bearing", "Back Bearing",
            "DN (m)", "DE (m)",
            "Corr DN", "Corr DE",
            "Adj DN", "Adj DE",
            "To Stn", "Final N", "Final E"
        }
        For Each c As String In rCols
            resultsGrid.Columns.Add(c, c)
        Next
        cardResults.Controls.Add(resultsGrid)

        ' Keep resultsGrid filling the card
        AddHandler cardResults.Resize, Sub(s, ev)
            resultsGrid.Width  = cardResults.ClientSize.Width
            resultsGrid.Height = cardResults.ClientSize.Height - 28
        End Sub
    End Sub



    ' =========================================================================
    '  FACTORY / STYLE HELPERS
    ' =========================================================================
    Private Function MakeCard() As Panel
        Dim p As New ShadowPanel With {
            .BackColor = S_CARD,
            .Margin    = New Padding(0, 0, 0, 8),
            .Font      = F_LABEL,
            .Padding   = New Padding(0)
        }
        Return p
    End Function

    Private Function MakeSectionLabel(txt As String, x As Integer, y As Integer) As Label
        Return New Label With {
            .Text      = txt,
            .Font      = F_SECTION,
            .ForeColor = T_MUTED,
            .AutoSize  = True,
            .Left      = x,
            .Top       = y
        }
    End Function

    Private Function MakeFieldLabel(txt As String) As Label
        Return New Label With {
            .Text        = txt,
            .Font        = F_LABEL,
            .ForeColor   = T_PRIMARY,
            .AutoSize    = False,
            .Width       = TextRenderer.MeasureText(txt, F_LABEL).Width + 6,
            .Height      = 32,
            .TextAlign   = ContentAlignment.MiddleRight,
            .Margin      = New Padding(0, 0, 4, 0)
        }
    End Function

    Private Function MakeInput(defaultVal As String, w As Integer) As TextBox
        Return New TextBox With {
            .Text        = defaultVal,
            .Width       = w,
            .Height      = 32,
            .Font        = F_INPUT,
            .BackColor   = S_CARD,
            .ForeColor   = T_PRIMARY,
            .BorderStyle = BorderStyle.FixedSingle,
            .Margin      = New Padding(0, 4, 0, 0)
        }
    End Function

    Private Function MakeSpacer(w As Integer) As Panel
        Return New Panel With {
            .Width     = w,
            .Height    = 32,
            .BackColor = Color.Transparent
        }
    End Function

    Private Function MakeBtn(txt As String, bg As Color, w As Integer, h As Integer) As Button
        Dim b As New Button With {
            .Text      = txt,
            .Width     = w,
            .Height    = h,
            .Font      = F_BTN,
            .BackColor = bg,
            .ForeColor = T_ONACCNT,
            .FlatStyle = FlatStyle.Flat,
            .Cursor    = Cursors.Hand,
            .Margin    = New Padding(0),
            .TabStop   = False
        }
        b.FlatAppearance.BorderSize  = 0
        b.FlatAppearance.BorderColor = bg   ' match border to bg so it vanishes completely
        b.FlatAppearance.MouseOverBackColor  = DarkenColor(bg, 20)
        b.FlatAppearance.MouseDownBackColor  = DarkenColor(bg, 35)
        Return b
    End Function

    ''' <summary>Darkens a colour by reducing each channel by <paramref name="amount"/>.</summary>
    Private Function DarkenColor(c As Color, amount As Integer) As Color
        Return Color.FromArgb(
            Math.Max(0, CInt(c.R) - amount),
            Math.Max(0, CInt(c.G) - amount),
            Math.Max(0, CInt(c.B) - amount))
    End Function

    ''' <summary>Adds a two-line stat tile (key label + bold value label) to a column in the stats TableLayoutPanel.</summary>
    Private Function AddStatTile(tbl As TableLayoutPanel, col As Integer, key As String, initialVal As String) As Label
        Dim cell As New Panel With {
            .Dock      = DockStyle.Fill,
            .BackColor = Color.Transparent,
            .Padding   = New Padding(14, 10, 8, 8),
            .Font      = F_STAT_K
        }
        cell.Controls.Add(New Label With {
            .Text      = key,
            .Font      = F_STAT_K,
            .ForeColor = T_MUTED,
            .AutoSize  = True,
            .Left      = 0,
            .Top       = 0
        })
        Dim valLbl As New Label With {
            .Text      = initialVal,
            .Font      = F_STAT_V,
            .ForeColor = T_PRIMARY,
            .AutoSize  = True,
            .Left      = 0,
            .Top       = 18
        }
        cell.Controls.Add(valLbl)
        tbl.Controls.Add(cell, col, 0)
        Return valLbl
    End Function

    Private Sub ApplyGridStyle(g As DataGridView)
        g.EnableHeadersVisualStyles                       = False
        g.ColumnHeadersDefaultCellStyle.BackColor         = S_HEADER
        g.ColumnHeadersDefaultCellStyle.ForeColor         = T_ON_DARK
        g.ColumnHeadersDefaultCellStyle.Font              = F_GRID_H
        g.ColumnHeadersDefaultCellStyle.Alignment         = DataGridViewContentAlignment.MiddleLeft
        g.ColumnHeadersDefaultCellStyle.Padding           = New Padding(8, 0, 0, 0)
        g.DefaultCellStyle.BackColor                      = S_CARD
        g.DefaultCellStyle.ForeColor                      = T_BODY
        g.DefaultCellStyle.SelectionBackColor             = A_TINT
        g.DefaultCellStyle.SelectionForeColor             = T_HIGH
        g.DefaultCellStyle.Padding                        = New Padding(8, 0, 8, 0)
        g.AlternatingRowsDefaultCellStyle.BackColor       = Color.FromArgb(248, 250, 252)
    End Sub

    ' ── Accent line under header ──
    Private Sub OnHeaderPaint(sender As Object, e As PaintEventArgs)
        Dim p As Panel = DirectCast(sender, Panel)
        Using br As New SolidBrush(A_BASE)
            e.Graphics.FillRectangle(br, 0, p.Height - 3, p.Width, 3)
        End Using
    End Sub

    ' ── Card shadow: the OUTER wrapper paints the shadow, inner panel is white ──
    ' (Panel.BackColor = Transparent alone doesn't work because Panel is not
    '  transparent by default — we must set WS_EX_TRANSPARENT via SetStyle.)
    ' The simplest reliable approach: paint shadow on the outer wrapper whose
    ' BackColor = S_PAGE, then the inner white panel sits 0,0 offset inside.
    Private Sub PaintCardShadow(sender As Object, e As PaintEventArgs)
        ' Not used — shadows handled by ShadowPanel subclass below
    End Sub

    ' ── Compute button rounded paint — handled by RoundButton subclass below ──
    Private Sub PaintRoundButton(sender As Object, e As PaintEventArgs)
        ' No-op: RoundButton.OnPaint does the real work
    End Sub



    ' =========================================================================
    '  SAMPLE DATA  —  dropdown menu opens on btnLoadSample click
    ' =========================================================================
    Private Sub LoadSampleData() Handles btnLoadSample.Click
        ' Show the dropdown directly below the button
        sampleMenu.Show(btnLoadSample, New Point(0, btnLoadSample.Height))
    End Sub

    ' ── Sample 1: classic regular pentagon (all angles equal, slight dist variance)
    Private Sub LoadSample_Pentagon(sender As Object, e As EventArgs)
        ClearAll()
        txtStartBearing.Text = "60 0 0"
        txtStartN.Text = "1000.000" : txtStartE.Text = "1000.000"
        inputGrid.Rows.Add("A", "108 0 0",  "100.02")
        inputGrid.Rows.Add("B", "108 0 0",  " 99.97")
        inputGrid.Rows.Add("C", "108 0 0",  "100.05")
        inputGrid.Rows.Add("D", "108 0 0",  " 99.94")
        inputGrid.Rows.Add("E", "108 0 0",  "100.03")
    End Sub

    ' ── Sample 2: equilateral triangle with slight angle errors
    Private Sub LoadSample_Triangle(sender As Object, e As EventArgs)
        ClearAll()
        txtStartBearing.Text = "30 0 0"
        txtStartN.Text = "500.000" : txtStartE.Text = "500.000"
        inputGrid.Rows.Add("P", "60 0 10", "120.05")
        inputGrid.Rows.Add("Q", "59 59 40", "119.98")
        inputGrid.Rows.Add("R", "60 0 20", "120.00")
    End Sub

    ' ── Sample 3: irregular quadrilateral — mixed angles, unequal legs
    Private Sub LoadSample_Quad(sender As Object, e As EventArgs)
        ClearAll()
        txtStartBearing.Text = "45 0 0"
        txtStartN.Text = "2000.000" : txtStartE.Text = "1500.000"
        inputGrid.Rows.Add("W", "95 12 30",  "85.44")
        inputGrid.Rows.Add("X", "82 47 50", "102.18")
        inputGrid.Rows.Add("Y", "91 35 20",  "78.65")
        inputGrid.Rows.Add("Z", "90 24 40",  "93.30")
    End Sub

    ' ── Sample 4: irregular hexagon — 6 stations, all angles and distances differ
    Private Sub LoadSample_Hex(sender As Object, e As EventArgs)
        ClearAll()
        txtStartBearing.Text = "22 30 0"
        txtStartN.Text = "5000.000" : txtStartE.Text = "5000.000"
        inputGrid.Rows.Add("A", "118 42 10",  "75.20")
        inputGrid.Rows.Add("B", "122 15 50",  "88.45")
        inputGrid.Rows.Add("C", "115 30 20",  "92.10")
        inputGrid.Rows.Add("D", "120 48 40",  "68.75")
        inputGrid.Rows.Add("E", "119 05 30",  "81.30")
        inputGrid.Rows.Add("F", "123 37 50",  "77.60")
    End Sub

    ' ── Row management ──────────────────────────────────────────────────────
    Private Sub AddRow_Click() Handles btnAddRow.Click
        inputGrid.Rows.Add("", "0 0 0", "0.00")
    End Sub

    Private Sub RemoveRow_Click() Handles btnRemoveRow.Click
        If inputGrid.Rows.Count > 0 AndAlso inputGrid.CurrentRow IsNot Nothing Then
            inputGrid.Rows.Remove(inputGrid.CurrentRow)
        End If
    End Sub

    Private Sub Reset_Click() Handles btnReset.Click
        ClearAll()
        txtStartBearing.Text = "0 0 0"
        txtStartN.Text = "0.000"
        txtStartE.Text = "0.000"
    End Sub

    ''' <summary>Clears grid, stat tiles and results grid.</summary>
    Private Sub ClearAll()
        inputGrid.Rows.Clear()
        resultsGrid.Rows.Clear()
        For Each lbl As Label In {lblAngularVal, lblMisclosVal, lblLinearVal, lblAccuracyVal, lblAreaVal}
            lbl.Text     = "—"
            lbl.ForeColor = T_BODY
        Next
        lblAreaVal.ForeColor = A_BASE
    End Sub

    ' =========================================================================
    '  MAIN COMPUTATION
    ' =========================================================================
    Private Sub ComputeTraverse_Click() Handles btnCompute.Click
        Try
            Dim n As Integer = inputGrid.Rows.Count
            If n < 3 Then
                MessageBox.Show("Please enter at least 3 stations (minimum for a closed traverse).",
                    "Insufficient Data", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim stationNames(n - 1) As String
            Dim includedAngles(n - 1) As Double
            Dim distances(n - 1) As Double

            For i As Integer = 0 To n - 1
                stationNames(i)   = CStr(inputGrid.Rows(i).Cells(0).Value).Trim()
                includedAngles(i) = ParseDMS(CStr(inputGrid.Rows(i).Cells(1).Value))
                distances(i)      = Convert.ToDouble(inputGrid.Rows(i).Cells(2).Value)
            Next

            Dim startBearing As Double = ParseDMS(txtStartBearing.Text)
            Dim startN       As Double = Convert.ToDouble(txtStartN.Text)
            Dim startE       As Double = Convert.ToDouble(txtStartE.Text)

            ' 1. Angular misclosure
            Dim sumAngles     As Double = 0
            For Each a As Double In includedAngles : sumAngles += a : Next
            Dim theoretical   As Double = (n - 2) * 180.0
            Dim angMisclos    As Double = sumAngles - theoretical

            lblAngularVal.Text = DecimalToDMS(sumAngles) & "  (th. " & DecimalToDMS(theoretical) & ")"
            lblMisclosVal.Text = DecimalToDMS(angMisclos)
            lblMisclosVal.ForeColor = If(Math.Abs(angMisclos) < 0.01, A_BASE, Color.FromArgb(220, 38, 38))

            ' Distribute angular correction
            Dim corrPerStn As Double = -angMisclos / n
            For i As Integer = 0 To n - 1 : includedAngles(i) += corrPerStn : Next

            ' 2. Bearings, DN, DE
            Dim legs(n - 1) As LegResult
            Dim fb As Double = startBearing

            For i As Integer = 0 To n - 1
                If i > 0 Then
                    Dim bbPrev As Double = (legs(i - 1).ForwardBearing + 180.0) Mod 360.0
                    fb = (bbPrev + includedAngles(i)) Mod 360.0   ' CORRECTED: interior angle → addition
                    If fb < 0 Then fb += 360.0
                End If
                Dim rad As Double = fb * Math.PI / 180.0
                legs(i).FromStation    = stationNames(i)
                legs(i).ToStation      = stationNames((i + 1) Mod n)
                legs(i).IncludedAngle  = includedAngles(i)
                legs(i).Distance       = distances(i)
                legs(i).ForwardBearing = fb
                legs(i).BackBearing    = (fb + 180.0) Mod 360.0
                legs(i).DN             = distances(i) * Math.Cos(rad)
                legs(i).DE             = distances(i) * Math.Sin(rad)
            Next

            ' 3. Linear misclosure
            Dim sumDN As Double = 0, sumDE As Double = 0, perim As Double = 0
            For i As Integer = 0 To n - 1
                sumDN += legs(i).DN : sumDE += legs(i).DE : perim += legs(i).Distance
            Next
            Dim linMisc  As Double = Math.Sqrt(sumDN ^ 2 + sumDE ^ 2)
            Dim accDenom As Double = If(linMisc = 0, 0, perim / linMisc)

            lblLinearVal.Text   = String.Format("{0:0.0000} m  (eN={1:0.0000}, eE={2:0.0000})", linMisc, sumDN, sumDE)
            lblAccuracyVal.Text = If(linMisc = 0, "Perfect Closure", String.Format("1 : {0:0}", accDenom))

            ' 4. Bowditch adjustment
            For i As Integer = 0 To n - 1
                legs(i).CorrDN = -sumDN * (legs(i).Distance / perim)
                legs(i).CorrDE = -sumDE * (legs(i).Distance / perim)
                legs(i).AdjDN  = legs(i).DN + legs(i).CorrDN
                legs(i).AdjDE  = legs(i).DE + legs(i).CorrDE
            Next

            ' 5. Final coordinates
            Dim finalN As New Dictionary(Of String, Double)
            Dim finalE As New Dictionary(Of String, Double)
            finalN(stationNames(0)) = startN
            finalE(stationNames(0)) = startE
            Dim curN As Double = startN, curE As Double = startE
            For i As Integer = 0 To n - 1
                curN += legs(i).AdjDN : curE += legs(i).AdjDE
                legs(i).FinalN = curN  : legs(i).FinalE = curE
                finalN(legs(i).ToStation) = curN
                finalE(legs(i).ToStation) = curE
            Next

            ' 6. Area (shoelace)
            Dim area As Double = 0
            For i As Integer = 0 To n - 1
                Dim s1 As String = stationNames(i)
                Dim s2 As String = stationNames((i + 1) Mod n)
                area += finalE(s1) * finalN(s2) - finalE(s2) * finalN(s1)
            Next
            area = Math.Abs(area) / 2.0
            lblAreaVal.Text = String.Format("{0:0.000} m²  ({1:0.0000} ha)", area, area / 10000.0)

            ' 7. Results grid
            resultsGrid.Rows.Clear()
            For i As Integer = 0 To n - 1
                resultsGrid.Rows.Add(
                    legs(i).FromStation & "–" & legs(i).ToStation,
                    DecimalToDMS(legs(i).IncludedAngle),
                    legs(i).Distance.ToString("0.00"),
                    DecimalToDMS(legs(i).ForwardBearing),
                    DecimalToDMS(legs(i).BackBearing),
                    legs(i).DN.ToString("0.0000"),
                    legs(i).DE.ToString("0.0000"),
                    legs(i).CorrDN.ToString("0.0000"),
                    legs(i).CorrDE.ToString("0.0000"),
                    legs(i).AdjDN.ToString("0.0000"),
                    legs(i).AdjDE.ToString("0.0000"),
                    legs(i).ToStation,
                    legs(i).FinalN.ToString("0.000"),
                    legs(i).FinalE.ToString("0.000"))
            Next

        Catch ex As Exception
            MessageBox.Show("Input error: " & ex.Message,
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub



    ' =========================================================================
    '  HELPERS
    ' =========================================================================
    Private Function ParseDMS(text As String) As Double
        text = text.Trim()
        If text.Contains(" ") Then
            Dim p() As String = text.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
            Dim d As Double = Convert.ToDouble(p(0))
            Dim m As Double = If(p.Length > 1, Convert.ToDouble(p(1)), 0)
            Dim s As Double = If(p.Length > 2, Convert.ToDouble(p(2)), 0)
            Dim sg As Double = If(d < 0, -1.0, 1.0)
            Return sg * (Math.Abs(d) + m / 60.0 + s / 3600.0)
        End If
        Return Convert.ToDouble(text)
    End Function

    Private Function DecimalToDMS(dd As Double) As String
        Dim sign   As String  = If(dd < 0, "-", "")
        Dim av     As Double  = Math.Abs(dd)
        Dim deg    As Integer = CInt(Math.Truncate(av))
        Dim mFull  As Double  = (av - deg) * 60.0
        Dim mins   As Integer = CInt(Math.Truncate(mFull))
        Dim secs   As Double  = (mFull - mins) * 60.0
        If secs >= 60.0 Then secs = 0 : mins += 1
        If mins >= 60   Then mins = 0 : deg  += 1
        Return String.Format("{0}{1}{2}'{3:00.0}""",
            sign, deg.ToString() & ChrW(176), mins.ToString("00"), secs)
    End Function

End Class

' =============================================================================
'  RoundButton — fully owner-painted button with true rounded corners.
'  SetStyle(UserPaint) suppresses all default WinForms background drawing
'  so GDI+ has a clean canvas.
' =============================================================================
Public Class RoundButton
    Inherits Button

    Public Property HoverColor As Color = Color.FromArgb(67, 56, 202)  ' indigo-700
    Public Property Radius     As Integer = 8

    Private _isHovered  As Boolean = False
    Private _isPressed  As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer, True)
        FlatStyle = FlatStyle.Flat
        FlatAppearance.BorderSize = 0
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        _isHovered = True
        Invalidate()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        _isHovered = False
        Invalidate()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        _isPressed = True
        Invalidate()
        MyBase.OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        _isPressed = False
        Invalidate()
        MyBase.OnMouseUp(e)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode      = SmoothingMode.AntiAlias
        g.TextRenderingHint  = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim bg As Color
        If _isPressed Then
            bg = Color.FromArgb(
                Math.Max(0, CInt(HoverColor.R) - 20),
                Math.Max(0, CInt(HoverColor.G) - 20),
                Math.Max(0, CInt(HoverColor.B) - 20))
        ElseIf _isHovered Then
            bg = HoverColor
        Else
            bg = BackColor
        End If

        Dim r   As New Rectangle(0, 0, Width - 1, Height - 1)
        Dim rad As Integer = Radius * 2

        Using path As New Drawing2D.GraphicsPath()
            path.AddArc(r.X,              r.Y,               rad, rad, 180, 90)
            path.AddArc(r.Right - rad,    r.Y,               rad, rad, 270, 90)
            path.AddArc(r.Right - rad,    r.Bottom - rad,    rad, rad,   0, 90)
            path.AddArc(r.X,              r.Bottom - rad,    rad, rad,  90, 90)
            path.CloseFigure()

            ' Fill
            Using br As New SolidBrush(bg)
                g.FillPath(br, path)
            End Using

            ' Clip so child paint can't escape the rounded shape
            g.SetClip(path)
        End Using

        ' Text centred
        Dim sf As New StringFormat With {
            .Alignment     = StringAlignment.Center,
            .LineAlignment = StringAlignment.Center
        }
        Using tb As New SolidBrush(ForeColor)
            g.DrawString(Text, Font, tb, RectangleF.op_Implicit(r), sf)
        End Using
    End Sub
End Class

' =============================================================================
'  ShadowPanel — Panel that paints its own drop shadow by drawing offset
'  semi-transparent rectangles on the PARENT surface before painting itself.
'  Uses WS_EX_TRANSPARENT (via CreateParams) so the parent background
'  shows through, making the shadow visible.
' =============================================================================
Public Class ShadowPanel
    Inherits Panel

    Private Const SHADOW_DEPTH As Integer = 4

    Public Sub New()
        SetStyle(ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.AllPaintingInWmPaint, True)
    End Sub

    Protected Overrides ReadOnly Property CreateParams() As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or &H20  ' WS_EX_TRANSPARENT
            Return cp
        End Get
    End Property

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        ' Paint parent background behind us first (needed for transparency)
        MyBase.OnPaintBackground(e)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        ' Shadow — drawn offset so it peeks out below/right
        For i As Integer = SHADOW_DEPTH To 1 Step -1
            Dim alpha As Integer = CInt(30 * (1 - (i - 1) / CDbl(SHADOW_DEPTH)))
            Dim sr As New Rectangle(i, i, Width - SHADOW_DEPTH - 1, Height - SHADOW_DEPTH - 1)
            Using sb As New SolidBrush(Color.FromArgb(alpha, 0, 0, 0))
                g.FillRectangle(sb, sr)
            End Using
        Next

        ' White card surface (slightly inset from shadow)
        Dim cr As New Rectangle(0, 0, Width - SHADOW_DEPTH, Height - SHADOW_DEPTH)
        Using wb As New SolidBrush(BackColor)
            g.FillRectangle(wb, cr)
        End Using

        ' Hairline border
        Using bp As New Pen(Color.FromArgb(229, 231, 235), 1)
            g.DrawRectangle(bp, cr.X, cr.Y, cr.Width - 1, cr.Height - 1)
        End Using
    End Sub
End Class
