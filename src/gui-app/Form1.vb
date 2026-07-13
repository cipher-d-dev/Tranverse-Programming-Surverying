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
    '  DESIGN TOKENS  — one place to change the whole look
    ' =========================================================================
    ' Surfaces
    Private Shared ReadOnly S_PAGE    As Color = Color.FromArgb(248, 249, 251)  ' off-white page
    Private Shared ReadOnly S_CARD    As Color = Color.White                    ' card surface
    Private Shared ReadOnly S_HEADER  As Color = Color.FromArgb(28,  35,  51)  ' near-black slate
    ' Accent  (single teal — every interactive element uses only this)
    Private Shared ReadOnly A_MID     As Color = Color.FromArgb( 20, 184, 166)  ' teal-500
    Private Shared ReadOnly A_DARK    As Color = Color.FromArgb( 13, 148, 136)  ' teal-600 (hover)
    Private Shared ReadOnly A_LIGHT   As Color = Color.FromArgb(204, 245, 241)  ' teal-100 (row tint)
    ' Text
    Private Shared ReadOnly T_PRIMARY As Color = Color.FromArgb( 17,  24,  39)  ' near-black
    Private Shared ReadOnly T_MUTED   As Color = Color.FromArgb(107, 114, 128)  ' gray-500
    Private Shared ReadOnly T_ONSLATE As Color = Color.White
    Private Shared ReadOnly T_ONACCNT As Color = Color.White
    ' Borders / dividers
    Private Shared ReadOnly B_SUBTLE  As Color = Color.FromArgb(229, 231, 235)  ' gray-200
    Private Shared ReadOnly B_FOCUS   As Color = Color.FromArgb( 20, 184, 166)  ' teal border on focus

    ' =========================================================================
    '  TYPOGRAPHY
    ' =========================================================================
    Private Shared ReadOnly F_TITLE   As New Font("Segoe UI",  15, FontStyle.Bold)
    Private Shared ReadOnly F_SUB     As New Font("Segoe UI",   9, FontStyle.Regular)
    Private Shared ReadOnly F_SECTION As New Font("Segoe UI",   8, FontStyle.Bold)
    Private Shared ReadOnly F_LABEL   As New Font("Segoe UI",   9, FontStyle.Regular)
    Private Shared ReadOnly F_INPUT   As New Font("Segoe UI",   9, FontStyle.Regular)
    Private Shared ReadOnly F_BTN     As New Font("Segoe UI",   9, FontStyle.Bold)
    Private Shared ReadOnly F_STAT_K  As New Font("Segoe UI",   8, FontStyle.Regular)   ' stat key
    Private Shared ReadOnly F_STAT_V  As New Font("Segoe UI",  10, FontStyle.Bold)      ' stat value
    Private Shared ReadOnly F_GRID_H  As New Font("Segoe UI",   8, FontStyle.Bold)
    Private Shared ReadOnly F_GRID_C  As New Font("Consolas",   9, FontStyle.Regular)

    ' =========================================================================
    '  CONTROLS
    ' =========================================================================
    Private WithEvents btnCompute    As Button
    Private WithEvents btnLoadSample As Button
    Private WithEvents btnAddRow     As Button
    Private WithEvents btnRemoveRow  As Button

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
        Me.Font          = F_LABEL

        ' ── Outer TableLayout: header row (fixed) + body row (fills) ─────────
        Dim outer As New TableLayoutPanel With {
            .Dock        = DockStyle.Fill,
            .RowCount    = 2,
            .ColumnCount = 1,
            .Padding     = New Padding(0),
            .Margin      = New Padding(0),
            .BackColor   = S_PAGE
        }
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 74))   ' header
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100))   ' body
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        Me.Controls.Add(outer)

        ' ── Header ───────────────────────────────────────────────────────────
        Dim header As New Panel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_HEADER,
            .Padding   = New Padding(24, 0, 0, 0)
        }
        AddHandler header.Paint, AddressOf OnHeaderPaint
        outer.Controls.Add(header, 0, 0)

        header.Controls.Add(New Label With {
            .Text      = "SVY 323  —  Closed Traverse Computation",
            .Font      = F_TITLE,
            .ForeColor = T_ONSLATE,
            .AutoSize  = True,
            .Left      = 24,
            .Top       = 14
        })
        header.Controls.Add(New Label With {
            .Text      = "Angular Adjustment  ·  Bowditch (Compass Rule)  ·  Bearings  ·  Latitudes & Departures  ·  Final Coordinates  ·  Area",
            .Font      = F_SUB,
            .ForeColor = Color.FromArgb(156, 163, 175),
            .AutoSize  = True,
            .Left      = 26,
            .Top       = 46
        })

        ' ── Body TableLayout: input section + compute bar + stats + results ──
        Dim body As New TableLayoutPanel With {
            .Dock        = DockStyle.Fill,
            .RowCount    = 4,
            .ColumnCount = 1,
            .Padding     = New Padding(16, 12, 16, 12),
            .BackColor   = S_PAGE
        }
        body.RowStyles.Add(New RowStyle(SizeType.Absolute, 210))   ' input section
        body.RowStyles.Add(New RowStyle(SizeType.Absolute,  48))   ' compute button
        body.RowStyles.Add(New RowStyle(SizeType.Absolute,  96))   ' stats bar
        body.RowStyles.Add(New RowStyle(SizeType.Percent,  100))   ' results (fills)
        body.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        outer.Controls.Add(body, 0, 1)

        ' ── ROW 0 : Input section ─────────────────────────────────────────────
        Dim cardInput As Panel = MakeCard()
        cardInput.Dock    = DockStyle.Fill
        cardInput.Padding = New Padding(16, 10, 16, 10)
        body.Controls.Add(cardInput, 0, 0)

        ' Section label
        cardInput.Controls.Add(MakeSectionLabel("CONTROL DATA  &  FIELD OBSERVATIONS", 0, 0))

        ' Control-data row: bearing, N, E, buttons  (use a FlowLayoutPanel for clean spacing)
        Dim flowCtrl As New FlowLayoutPanel With {
            .FlowDirection = FlowDirection.LeftToRight,
            .AutoSize      = True,
            .Left          = 0,
            .Top           = 22,
            .Height        = 30,
            .WrapContents  = False,
            .BackColor     = Color.Transparent
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
        btnLoadSample = MakeBtn("↺  Load Sample",  A_MID, 118, 28)
        btnAddRow     = MakeBtn("+  Add Row",       A_MID, 94,  28)
        btnRemoveRow  = MakeBtn("−  Remove",        Color.FromArgb(75,85,99), 84, 28)
        flowCtrl.Controls.Add(btnLoadSample)
        flowCtrl.Controls.Add(MakeSpacer(6))
        flowCtrl.Controls.Add(btnAddRow)
        flowCtrl.Controls.Add(MakeSpacer(4))
        flowCtrl.Controls.Add(btnRemoveRow)
        cardInput.Controls.Add(flowCtrl)

        ' Hint text
        Dim hint As New Label With {
            .Text      = "Enter station name, included angle (degrees minutes seconds, space-separated) and distance to next station in metres.  Minimum 5 rows.",
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
            .Padding   = New Padding(0, 6, 0, 6)
        }
        body.Controls.Add(pnlCompute, 0, 1)

        btnCompute = New Button With {
            .Text      = "▶   COMPUTE TRAVERSE",
            .Dock      = DockStyle.Fill,
            .Font      = New Font("Segoe UI", 10, FontStyle.Bold),
            .BackColor = A_MID,
            .ForeColor = T_ONACCNT,
            .FlatStyle = FlatStyle.Flat,
            .Cursor    = Cursors.Hand
        }
        btnCompute.FlatAppearance.BorderSize      = 0
        btnCompute.FlatAppearance.MouseOverBackColor = A_DARK
        pnlCompute.Controls.Add(btnCompute)

        ' ── ROW 2 : Stats bar (5 tiles) ───────────────────────────────────────
        Dim cardStats As Panel = MakeCard()
        cardStats.Dock    = DockStyle.Fill
        cardStats.Padding = New Padding(0)
        body.Controls.Add(cardStats, 0, 2)

        Dim statFlow As New TableLayoutPanel With {
            .Dock        = DockStyle.Fill,
            .ColumnCount = 5,
            .RowCount    = 1,
            .BackColor   = Color.Transparent,
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
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
        lblAreaVal.ForeColor = A_MID   ' highlight the area tile value

        ' ── ROW 3 : Results grid ──────────────────────────────────────────────
        Dim cardResults As Panel = MakeCard()
        cardResults.Dock    = DockStyle.Fill
        cardResults.Padding = New Padding(0)
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
        Return New Panel With {
            .BackColor = S_CARD,
            .Margin    = New Padding(0, 0, 0, 8)
        }
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
            .Height      = 28,
            .TextAlign   = ContentAlignment.MiddleRight,
            .Margin      = New Padding(0, 0, 4, 0)
        }
    End Function

    Private Function MakeInput(defaultVal As String, w As Integer) As TextBox
        Return New TextBox With {
            .Text        = defaultVal,
            .Width       = w,
            .Height      = 28,
            .Font        = F_INPUT,
            .BackColor   = S_CARD,
            .ForeColor   = T_PRIMARY,
            .BorderStyle = BorderStyle.FixedSingle,
            .Margin      = New Padding(0, 0, 0, 0)
        }
    End Function

    Private Function MakeSpacer(w As Integer) As Panel
        Return New Panel With {
            .Width     = w,
            .Height    = 1,
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
            .Margin    = New Padding(0)
        }
        b.FlatAppearance.BorderSize = 0
        Return b
    End Function

    ''' <summary>Adds a two-line stat tile (key label + bold value label) to a column in the stats TableLayoutPanel.</summary>
    Private Function AddStatTile(tbl As TableLayoutPanel, col As Integer, key As String, initialVal As String) As Label
        Dim cell As New Panel With {
            .Dock      = DockStyle.Fill,
            .BackColor = Color.Transparent,
            .Padding   = New Padding(14, 10, 8, 8)
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
        g.ColumnHeadersDefaultCellStyle.ForeColor         = Color.FromArgb(209, 213, 219)
        g.ColumnHeadersDefaultCellStyle.Font              = F_GRID_H
        g.ColumnHeadersDefaultCellStyle.Alignment         = DataGridViewContentAlignment.MiddleLeft
        g.ColumnHeadersDefaultCellStyle.Padding           = New Padding(6, 0, 0, 0)
        g.DefaultCellStyle.BackColor                      = S_CARD
        g.DefaultCellStyle.ForeColor                      = T_PRIMARY
        g.DefaultCellStyle.SelectionBackColor             = A_LIGHT
        g.DefaultCellStyle.SelectionForeColor             = T_PRIMARY
        g.DefaultCellStyle.Padding                        = New Padding(6, 0, 6, 0)
        g.AlternatingRowsDefaultCellStyle.BackColor       = Color.FromArgb(250, 252, 252)
    End Sub

    ' ── Accent line under header ──
    Private Sub OnHeaderPaint(sender As Object, e As PaintEventArgs)
        Dim p As Panel = DirectCast(sender, Panel)
        Using br As New SolidBrush(A_MID)
            e.Graphics.FillRectangle(br, 0, p.Height - 3, p.Width, 3)
        End Using
    End Sub



    ' =========================================================================
    '  SAMPLE DATA  &  ROW MANAGEMENT
    ' =========================================================================
    Private Sub LoadSampleData() Handles btnLoadSample.Click
        inputGrid.Rows.Clear()
        inputGrid.Rows.Add("A", "108 0 0", "100.02")
        inputGrid.Rows.Add("B", "108 0 0",  "99.97")
        inputGrid.Rows.Add("C", "108 0 0", "100.05")
        inputGrid.Rows.Add("D", "108 0 0",  "99.94")
        inputGrid.Rows.Add("E", "108 0 0", "100.03")
        txtStartBearing.Text = "60 0 0"
        txtStartN.Text       = "1000.000"
        txtStartE.Text       = "1000.000"
        ' Reset stat tiles
        For Each lbl As Label In {lblAngularVal, lblMisclosVal, lblLinearVal, lblAccuracyVal, lblAreaVal}
            lbl.Text = "—"
        Next
        lblAreaVal.ForeColor = A_MID
        resultsGrid.Rows.Clear()
    End Sub

    Private Sub AddRow_Click() Handles btnAddRow.Click
        inputGrid.Rows.Add("", "0 0 0", "0.00")
    End Sub

    Private Sub RemoveRow_Click() Handles btnRemoveRow.Click
        If inputGrid.Rows.Count > 0 AndAlso inputGrid.CurrentRow IsNot Nothing Then
            inputGrid.Rows.Remove(inputGrid.CurrentRow)
        End If
    End Sub

    ' =========================================================================
    '  MAIN COMPUTATION
    ' =========================================================================
    Private Sub ComputeTraverse_Click() Handles btnCompute.Click
        Try
            Dim n As Integer = inputGrid.Rows.Count
            If n < 5 Then
                MessageBox.Show("Please enter at least 5 stations.",
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
            lblMisclosVal.ForeColor = If(Math.Abs(angMisclos) < 0.01, A_MID, Color.FromArgb(239, 68, 68))

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
