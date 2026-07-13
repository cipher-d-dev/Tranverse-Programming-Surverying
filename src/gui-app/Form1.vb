'==================================================================================
'  SVY 323 — COMPUTER APPLICATION I  |  Closed Traverse Computation  |  GUI
'==================================================================================
Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Collections.Generic

Public Class Form1
    Inherits Form

    ' ── Palette  (60-30-10: slate surfaces / slate-900 chrome / indigo accent) ──
    Private Shared ReadOnly S_PAGE   As Color = Color.FromArgb(248, 250, 252)
    Private Shared ReadOnly S_CARD   As Color = Color.White
    Private Shared ReadOnly S_HEADER As Color = Color.FromArgb( 15,  23,  42)
    Private Shared ReadOnly A_BASE   As Color = Color.FromArgb( 79,  70, 229)
    Private Shared ReadOnly A_HOVER  As Color = Color.FromArgb( 67,  56, 202)
    Private Shared ReadOnly A_TINT   As Color = Color.FromArgb(238, 242, 255)
    Private Shared ReadOnly T_HIGH   As Color = Color.FromArgb( 15,  23,  42)
    Private Shared ReadOnly T_BODY   As Color = Color.FromArgb( 51,  65,  85)
    Private Shared ReadOnly T_MUTED  As Color = Color.FromArgb(100, 116, 139)
    Private Shared ReadOnly T_LIGHT  As Color = Color.FromArgb(241, 245, 249)
    Private Shared ReadOnly T_WHITE  As Color = Color.White
    Private Shared ReadOnly B_LINE   As Color = Color.FromArgb(226, 232, 240)
    Private Shared ReadOnly SLATE_MID As Color = Color.FromArgb(71, 85, 105)

    ' ── Typography  (Segoe UI Variable = Win11 system font; graceful fallback) ──
    Private Shared ReadOnly FACE      As String = "Segoe UI Variable"
    Private Shared ReadOnly FACE_MONO As String = "Cascadia Mono"
    Private Shared ReadOnly F_DISPLAY As New Font(FACE, 15, FontStyle.Bold)
    Private Shared ReadOnly F_BODY    As New Font(FACE,  9, FontStyle.Regular)
    Private Shared ReadOnly F_BOLD    As New Font(FACE,  9, FontStyle.Bold)
    Private Shared ReadOnly F_CAPTION As New Font(FACE,  8, FontStyle.Regular)
    Private Shared ReadOnly F_CAP_B   As New Font(FACE,  8, FontStyle.Bold)
    Private Shared ReadOnly F_BTN     As New Font(FACE,  9, FontStyle.Bold)
    Private Shared ReadOnly F_STAT_K  As New Font(FACE,  8, FontStyle.Regular)
    Private Shared ReadOnly F_STAT_V  As New Font(FACE, 12, FontStyle.Bold)
    Private Shared ReadOnly F_GRID_H  As New Font(FACE,  8, FontStyle.Bold)
    Private Shared ReadOnly F_GRID_C  As New Font(FACE_MONO, 9, FontStyle.Regular)

    ' ── Controls ──
    Private WithEvents btnCompute    As Button
    Private WithEvents btnLoadSample As Button
    Private WithEvents btnAddRow     As Button
    Private WithEvents btnRemoveRow  As Button
    Private WithEvents btnReset      As Button
    Private sampleMenu               As ContextMenuStrip

    Private inputGrid   As DataGridView
    Private resultsGrid As DataGridView

    Private txtStartBearing As TextBox
    Private txtStartN       As TextBox
    Private txtStartE       As TextBox

    Private lblAngularVal  As Label
    Private lblMisclosVal  As Label
    Private lblLinearVal   As Label
    Private lblAccuracyVal As Label
    Private lblAreaVal     As Label

    ' ── Data ──
    Structure LegResult
        Public FromStation As String,  ToStation As String
        Public IncludedAngle As Double, Distance As Double
        Public ForwardBearing As Double, BackBearing As Double
        Public DN As Double,  DE As Double
        Public CorrDN As Double, CorrDE As Double
        Public AdjDN As Double,  AdjDE As Double
        Public FinalN As Double, FinalE As Double
    End Structure

    ' ══════════════════════════════════════════════════════════════════════════
    Public Sub New()
        Me.SuspendLayout()
        BuildUI()
        Me.ResumeLayout(False)
        Me.PerformLayout()
        LoadSample_Pentagon(Nothing, Nothing)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  BUILD UI
    ' ══════════════════════════════════════════════════════════════════════════
    Private Sub BuildUI()
        Me.Text          = "SVY 323  ·  Closed Traverse Computation"
        Me.ClientSize    = New Size(1280, 840)
        Me.MinimumSize   = New Size(1100, 740)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor     = S_PAGE
        Me.Font          = F_BODY

        ' ── Outer layout: header (fixed 74) + body (fills) ──
        Dim outer As New TableLayoutPanel With {
            .Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1,
            .Padding = New Padding(0), .Margin = New Padding(0), .BackColor = S_PAGE, .Font = F_BODY
        }
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 74))
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        Me.Controls.Add(outer)

        ' ── Header ──
        Dim header As New Panel With {
            .Dock = DockStyle.Fill, .BackColor = S_HEADER, .Font = F_BODY
        }
        AddHandler header.Paint, Sub(s As Object, ev As PaintEventArgs)
            Using br As New SolidBrush(A_BASE)
                ev.Graphics.FillRectangle(br, 0, header.Height - 3, header.Width, 3)
            End Using
        End Sub
        outer.Controls.Add(header, 0, 0)

        header.Controls.Add(New Label With {
            .Text = "SVY 323  —  Closed Traverse Computation",
            .Font = F_DISPLAY, .ForeColor = T_LIGHT, .AutoSize = True, .Left = 24, .Top = 12
        })
        header.Controls.Add(New Label With {
            .Text = "Angular Adjustment  ·  Bowditch (Compass Rule)  ·  Bearings  ·  Latitudes & Departures  ·  Final Coordinates  ·  Area",
            .Font = F_CAPTION, .ForeColor = T_MUTED, .AutoSize = True, .Left = 26, .Top = 46
        })

        ' ── Body layout: input(220) + compute(54) + stats(104) + results(fill) ──
        Dim body As New TableLayoutPanel With {
            .Dock = DockStyle.Fill, .RowCount = 4, .ColumnCount = 1,
            .Padding = New Padding(16, 12, 16, 12), .BackColor = S_PAGE, .Font = F_BODY
        }
        body.RowStyles.Add(New RowStyle(SizeType.Absolute, 220))
        body.RowStyles.Add(New RowStyle(SizeType.Absolute,  54))
        body.RowStyles.Add(New RowStyle(SizeType.Absolute, 104))
        body.RowStyles.Add(New RowStyle(SizeType.Percent,  100))
        body.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        outer.Controls.Add(body, 0, 1)

        ' ── Row 0: Input card ──
        Dim cardInput As New Panel With {
            .Dock = DockStyle.Fill, .BackColor = S_CARD,
            .Padding = New Padding(20, 12, 20, 12), .Font = F_BODY
        }
        body.Controls.Add(cardInput, 0, 0)

        cardInput.Controls.Add(New Label With {
            .Text = "CONTROL DATA  &  FIELD OBSERVATIONS", .Font = F_CAP_B,
            .ForeColor = T_MUTED, .AutoSize = True, .Left = 0, .Top = 0
        })

        ' Control row (FlowLayoutPanel)
        Dim flow As New FlowLayoutPanel With {
            .FlowDirection = FlowDirection.LeftToRight, .AutoSize = True,
            .Left = 0, .Top = 22, .Height = 36, .WrapContents = False,
            .BackColor = Color.Transparent, .Font = F_BODY
        }

        flow.Controls.Add(MakeFieldLbl("Start Bearing (D M S)"))
        txtStartBearing = MakeTxt("60 0 0", 90)
        flow.Controls.Add(txtStartBearing)
        flow.Controls.Add(MakeGap(16))

        flow.Controls.Add(MakeFieldLbl("Start Northing (m)"))
        txtStartN = MakeTxt("1000.000", 96)
        flow.Controls.Add(txtStartN)
        flow.Controls.Add(MakeGap(16))

        flow.Controls.Add(MakeFieldLbl("Start Easting (m)"))
        txtStartE = MakeTxt("1000.000", 96)
        flow.Controls.Add(txtStartE)
        flow.Controls.Add(MakeGap(28))

        btnLoadSample = MakeBtn("↺  Sample Data ▾", A_BASE,  138, 36)
        btnAddRow     = MakeBtn("+  Add Row",        A_BASE,  100, 36)
        btnRemoveRow  = MakeBtn("−  Remove Row",     SLATE_MID, 112, 36)
        btnReset      = MakeBtn("⊘  Reset",           SLATE_MID,  86, 36)
        flow.Controls.Add(btnLoadSample)
        flow.Controls.Add(MakeGap(8))
        flow.Controls.Add(btnAddRow)
        flow.Controls.Add(MakeGap(6))
        flow.Controls.Add(btnRemoveRow)
        flow.Controls.Add(MakeGap(6))
        flow.Controls.Add(btnReset)
        cardInput.Controls.Add(flow)

        ' Sample dropdown
        sampleMenu = New ContextMenuStrip With {.Font = F_BODY}
        sampleMenu.Items.Add("Pentagon  — 5 stations, 108° each (slight distance variance)",   Nothing, AddressOf LoadSample_Pentagon)
        sampleMenu.Items.Add("Triangle  — 3 stations, ~60° equilateral with angle errors",     Nothing, AddressOf LoadSample_Triangle)
        sampleMenu.Items.Add("Quadrilateral  — 4 stations, mixed irregular angles",            Nothing, AddressOf LoadSample_Quad)
        sampleMenu.Items.Add("Hexagon  — 6 stations, all angles and distances differ",         Nothing, AddressOf LoadSample_Hex)

        ' Hint
        cardInput.Controls.Add(New Label With {
            .Text = "Station name  ·  Included angle (D M S, space-separated)  ·  Distance to next station (m).  Minimum 3 stations.",
            .Font = F_CAPTION, .ForeColor = T_MUTED, .AutoSize = False,
            .Width = 900, .Height = 16, .Left = 0, .Top = 66
        })

        ' Input grid
        inputGrid = New DataGridView With {
            .Left = 0, .Top = 86, .Height = 110,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows = False, .RowHeadersVisible = False,
            .BorderStyle = BorderStyle.None, .BackgroundColor = S_CARD,
            .GridColor = B_LINE, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .ColumnHeadersHeight = 30, .Font = F_BODY, .ScrollBars = ScrollBars.Vertical
        }
        inputGrid.RowTemplate.Height = 30
        StyleGrid(inputGrid)
        inputGrid.Columns.Add("col_stn",  "Station")
        inputGrid.Columns.Add("col_ang",  "Included Angle  (D  M  S)")
        inputGrid.Columns.Add("col_dist", "Distance to Next Station (m)")
        inputGrid.Columns(0).Width = 160
        inputGrid.Columns(1).Width = 240
        inputGrid.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        cardInput.Controls.Add(inputGrid)

        ' ── Row 1: Compute button ──
        Dim pnlBtn As New Panel With {
            .Dock = DockStyle.Fill, .BackColor = S_PAGE,
            .Padding = New Padding(0, 8, 0, 8), .Font = F_BODY
        }
        body.Controls.Add(pnlBtn, 0, 1)

        btnCompute = New Button With {
            .Text = "▶   COMPUTE TRAVERSE", .Dock = DockStyle.Fill,
            .Font = New Font(FACE, 11, FontStyle.Bold),
            .BackColor = A_BASE, .ForeColor = T_WHITE,
            .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand, .TabStop = False
        }
        btnCompute.FlatAppearance.BorderSize         = 0
        btnCompute.FlatAppearance.BorderColor        = A_BASE
        btnCompute.FlatAppearance.MouseOverBackColor = A_HOVER
        btnCompute.FlatAppearance.MouseDownBackColor = Color.FromArgb(55, 48, 163)
        pnlBtn.Controls.Add(btnCompute)

        ' ── Row 2: Stats bar — 5 tiles ──
        Dim cardStats As New Panel With {
            .Dock = DockStyle.Fill, .BackColor = S_CARD, .Font = F_BODY
        }
        body.Controls.Add(cardStats, 0, 2)

        Dim statTbl As New TableLayoutPanel With {
            .Dock = DockStyle.Fill, .ColumnCount = 5, .RowCount = 1,
            .BackColor = Color.Transparent,
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        }
        For i As Integer = 0 To 4
            statTbl.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
        Next
        statTbl.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        cardStats.Controls.Add(statTbl)

        lblAngularVal  = AddStatTile(statTbl, 0, "Sum of Angles",      "—")
        lblMisclosVal  = AddStatTile(statTbl, 1, "Angular Misclosure", "—")
        lblLinearVal   = AddStatTile(statTbl, 2, "Linear Misclosure",  "—")
        lblAccuracyVal = AddStatTile(statTbl, 3, "Linear Accuracy",    "—")
        lblAreaVal     = AddStatTile(statTbl, 4, "Enclosed Area",      "—")
        lblAreaVal.ForeColor = A_BASE

        ' ── Row 3: Results grid ──
        Dim cardRes As New Panel With {
            .Dock = DockStyle.Fill, .BackColor = S_CARD, .Font = F_BODY
        }
        body.Controls.Add(cardRes, 0, 3)

        cardRes.Controls.Add(New Label With {
            .Text = "COMPUTATION RESULTS", .Font = F_CAP_B,
            .ForeColor = T_MUTED, .AutoSize = True, .Left = 12, .Top = 8
        })

        resultsGrid = New DataGridView With {
            .Left = 0, .Top = 28,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or
                      AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows = False, .ReadOnly = True,
            .RowHeadersVisible = False, .BorderStyle = BorderStyle.None,
            .BackgroundColor = S_CARD, .GridColor = B_LINE,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .ColumnHeadersHeight = 32, .Font = F_GRID_C, .ScrollBars = ScrollBars.Both
        }
        resultsGrid.RowTemplate.Height = 28
        StyleGrid(resultsGrid)
        For Each col As String In {"Leg", "Angle", "Dist (m)", "Fwd Bearing",
            "Back Bearing", "DN (m)", "DE (m)", "Corr DN", "Corr DE",
            "Adj DN", "Adj DE", "To Stn", "Final N", "Final E"}
            resultsGrid.Columns.Add(col, col)
        Next
        cardRes.Controls.Add(resultsGrid)

        AddHandler cardRes.Resize, Sub(s As Object, ev As EventArgs)
            resultsGrid.Width  = cardRes.ClientSize.Width
            resultsGrid.Height = cardRes.ClientSize.Height - 28
        End Sub
    End Sub



    ' ══════════════════════════════════════════════════════════════════════════
    '  FACTORY HELPERS
    ' ══════════════════════════════════════════════════════════════════════════
    Private Function MakeFieldLbl(txt As String) As Label
        Return New Label With {
            .Text = txt, .Font = F_BODY, .ForeColor = T_BODY, .AutoSize = False,
            .Width = TextRenderer.MeasureText(txt, F_BODY).Width + 8,
            .Height = 36, .TextAlign = ContentAlignment.MiddleRight,
            .Margin = New Padding(0, 0, 6, 0)
        }
    End Function

    Private Function MakeTxt(val As String, w As Integer) As TextBox
        Return New TextBox With {
            .Text = val, .Width = w, .Font = F_BODY,
            .BackColor = S_CARD, .ForeColor = T_HIGH,
            .BorderStyle = BorderStyle.FixedSingle,
            .Margin = New Padding(0, 4, 0, 0)
        }
    End Function

    Private Function MakeGap(w As Integer) As Panel
        Return New Panel With {.Width = w, .Height = 36, .BackColor = Color.Transparent}
    End Function

    Private Function MakeBtn(txt As String, bg As Color, w As Integer, h As Integer) As Button
        Dim b As New Button With {
            .Text = txt, .Width = w, .Height = h, .Font = F_BTN,
            .BackColor = bg, .ForeColor = T_WHITE,
            .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand,
            .Margin = New Padding(0), .TabStop = False
        }
        b.FlatAppearance.BorderSize  = 0
        b.FlatAppearance.BorderColor = bg
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(
            Math.Max(0, CInt(bg.R) - 18),
            Math.Max(0, CInt(bg.G) - 18),
            Math.Max(0, CInt(bg.B) - 18))
        b.FlatAppearance.MouseDownBackColor = Color.FromArgb(
            Math.Max(0, CInt(bg.R) - 36),
            Math.Max(0, CInt(bg.G) - 36),
            Math.Max(0, CInt(bg.B) - 36))
        Return b
    End Function

    Private Function AddStatTile(tbl As TableLayoutPanel, col As Integer,
                                  key As String, init As String) As Label
        Dim cell As New Panel With {
            .Dock = DockStyle.Fill, .BackColor = Color.Transparent,
            .Padding = New Padding(16, 10, 8, 8), .Font = F_STAT_K
        }
        cell.Controls.Add(New Label With {
            .Text = key, .Font = F_STAT_K, .ForeColor = T_MUTED,
            .AutoSize = True, .Left = 0, .Top = 0
        })
        Dim v As New Label With {
            .Text = init, .Font = F_STAT_V, .ForeColor = T_HIGH,
            .AutoSize = True, .Left = 0, .Top = 18
        }
        cell.Controls.Add(v)
        tbl.Controls.Add(cell, col, 0)
        Return v
    End Function

    Private Sub StyleGrid(g As DataGridView)
        g.EnableHeadersVisualStyles = False
        g.ColumnHeadersDefaultCellStyle.BackColor   = S_HEADER
        g.ColumnHeadersDefaultCellStyle.ForeColor   = T_LIGHT
        g.ColumnHeadersDefaultCellStyle.Font        = F_GRID_H
        g.ColumnHeadersDefaultCellStyle.Alignment   = DataGridViewContentAlignment.MiddleLeft
        g.ColumnHeadersDefaultCellStyle.Padding     = New Padding(8, 0, 0, 0)
        g.DefaultCellStyle.BackColor                = S_CARD
        g.DefaultCellStyle.ForeColor                = T_BODY
        g.DefaultCellStyle.SelectionBackColor       = A_TINT
        g.DefaultCellStyle.SelectionForeColor       = T_HIGH
        g.DefaultCellStyle.Padding                  = New Padding(8, 0, 8, 0)
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)
    End Sub



    ' ══════════════════════════════════════════════════════════════════════════
    '  SAMPLE DATA
    ' ══════════════════════════════════════════════════════════════════════════
    Private Sub LoadSampleData() Handles btnLoadSample.Click
        sampleMenu.Show(btnLoadSample, New Point(0, btnLoadSample.Height))
    End Sub

    Private Sub LoadSample_Pentagon(sender As Object, e As EventArgs)
        ClearAll() : txtStartBearing.Text = "60 0 0"
        txtStartN.Text = "1000.000" : txtStartE.Text = "1000.000"
        inputGrid.Rows.Add("A", "108 0 0",  "100.02")
        inputGrid.Rows.Add("B", "108 0 0",   "99.97")
        inputGrid.Rows.Add("C", "108 0 0",  "100.05")
        inputGrid.Rows.Add("D", "108 0 0",   "99.94")
        inputGrid.Rows.Add("E", "108 0 0",  "100.03")
    End Sub

    Private Sub LoadSample_Triangle(sender As Object, e As EventArgs)
        ClearAll() : txtStartBearing.Text = "30 0 0"
        txtStartN.Text = "500.000" : txtStartE.Text = "500.000"
        inputGrid.Rows.Add("P", "60 0 10",  "120.05")
        inputGrid.Rows.Add("Q", "59 59 40",  "119.98")
        inputGrid.Rows.Add("R", "60 0 20",  "120.00")
    End Sub

    Private Sub LoadSample_Quad(sender As Object, e As EventArgs)
        ClearAll() : txtStartBearing.Text = "45 0 0"
        txtStartN.Text = "2000.000" : txtStartE.Text = "1500.000"
        inputGrid.Rows.Add("W", "95 12 30",  "85.44")
        inputGrid.Rows.Add("X", "82 47 50", "102.18")
        inputGrid.Rows.Add("Y", "91 35 20",  "78.65")
        inputGrid.Rows.Add("Z", "90 24 40",  "93.30")
    End Sub

    Private Sub LoadSample_Hex(sender As Object, e As EventArgs)
        ClearAll() : txtStartBearing.Text = "22 30 0"
        txtStartN.Text = "5000.000" : txtStartE.Text = "5000.000"
        inputGrid.Rows.Add("A", "118 42 10",  "75.20")
        inputGrid.Rows.Add("B", "122 15 50",  "88.45")
        inputGrid.Rows.Add("C", "115 30 20",  "92.10")
        inputGrid.Rows.Add("D", "120 48 40",  "68.75")
        inputGrid.Rows.Add("E", "119 5 30",   "81.30")
        inputGrid.Rows.Add("F", "123 37 50",  "77.60")
    End Sub

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
        txtStartN.Text = "0.000" : txtStartE.Text = "0.000"
    End Sub

    Private Sub ClearAll()
        inputGrid.Rows.Clear()
        resultsGrid.Rows.Clear()
        For Each lbl As Label In {lblAngularVal, lblMisclosVal, lblLinearVal, lblAccuracyVal, lblAreaVal}
            lbl.Text = "—" : lbl.ForeColor = T_HIGH
        Next
        lblAreaVal.ForeColor = A_BASE
    End Sub



    ' ══════════════════════════════════════════════════════════════════════════
    '  COMPUTATION
    ' ══════════════════════════════════════════════════════════════════════════
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
            Dim startN As Double = Convert.ToDouble(txtStartN.Text)
            Dim startE As Double = Convert.ToDouble(txtStartE.Text)

            ' 1. Angular misclosure
            Dim sumAng As Double = 0
            For Each a As Double In includedAngles : sumAng += a : Next
            Dim theory   As Double = (n - 2) * 180.0
            Dim angMisc  As Double = sumAng - theory

            lblAngularVal.Text = DecimalToDMS(sumAng) & "  (th. " & DecimalToDMS(theory) & ")"
            lblMisclosVal.Text = DecimalToDMS(angMisc)
            lblMisclosVal.ForeColor = If(Math.Abs(angMisc) < 0.01,
                                         Color.FromArgb(22, 163, 74),
                                         Color.FromArgb(220, 38, 38))

            Dim corrPerStn As Double = -angMisc / n
            For i As Integer = 0 To n - 1 : includedAngles(i) += corrPerStn : Next

            ' 2. Bearings, DN, DE
            Dim legs(n - 1) As LegResult
            Dim fb As Double = startBearing
            For i As Integer = 0 To n - 1
                If i > 0 Then
                    Dim bbPrev As Double = (legs(i - 1).ForwardBearing + 180.0) Mod 360.0
                    fb = (bbPrev + includedAngles(i)) Mod 360.0
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

            ' 4. Bowditch
            For i As Integer = 0 To n - 1
                legs(i).CorrDN = -sumDN * (legs(i).Distance / perim)
                legs(i).CorrDE = -sumDE * (legs(i).Distance / perim)
                legs(i).AdjDN  = legs(i).DN + legs(i).CorrDN
                legs(i).AdjDE  = legs(i).DE + legs(i).CorrDE
            Next

            ' 5. Coordinates
            Dim finalN As New Dictionary(Of String, Double)
            Dim finalE As New Dictionary(Of String, Double)
            finalN(stationNames(0)) = startN : finalE(stationNames(0)) = startE
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
            MessageBox.Show("Input error: " & ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub



    ' ══════════════════════════════════════════════════════════════════════════
    '  HELPERS
    ' ══════════════════════════════════════════════════════════════════════════
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
        Dim sign  As String  = If(dd < 0, "-", "")
        Dim av    As Double  = Math.Abs(dd)
        Dim deg   As Integer = CInt(Math.Truncate(av))
        Dim mFull As Double  = (av - deg) * 60.0
        Dim mins  As Integer = CInt(Math.Truncate(mFull))
        Dim secs  As Double  = (mFull - mins) * 60.0
        If secs >= 60.0 Then secs = 0 : mins += 1
        If mins >= 60   Then mins = 0 : deg  += 1
        Return String.Format("{0}{1}{2}'{3:00.0}""",
            sign, deg.ToString() & ChrW(176), mins.ToString("00"), secs)
    End Function

End Class
