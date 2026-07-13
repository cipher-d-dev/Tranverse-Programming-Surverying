'==================================================================================
'  SVY 323 - COMPUTER APPLICATION I
'  TRAVERSE COMPUTATION PROGRAMME (Closed Traverse) - WINDOWS FORMS (GUI) VERSION
'  Redesigned: modern dark-header / card layout, accent colours, styled grids
'==================================================================================

Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Collections.Generic

Public Class Form1
    Inherits Form

    ' ── Theme colours ──────────────────────────────────────────────────────────
    Private Shared ReadOnly CLR_BG         As Color = Color.FromArgb(245, 246, 250)   ' page background
    Private Shared ReadOnly CLR_HEADER_TOP As Color = Color.FromArgb(15,  32,  65)    ' deep navy
    Private Shared ReadOnly CLR_HEADER_BOT As Color = Color.FromArgb(26,  58, 110)    ' lighter navy
    Private Shared ReadOnly CLR_CARD       As Color = Color.White
    Private Shared ReadOnly CLR_ACCENT     As Color = Color.FromArgb(0,  122, 255)    ' iOS-style blue
    Private Shared ReadOnly CLR_ACCENT_HOV As Color = Color.FromArgb(0,   95, 210)
    Private Shared ReadOnly CLR_ACCENT_TXT As Color = Color.White
    Private Shared ReadOnly CLR_DANGER     As Color = Color.FromArgb(220,  53,  69)
    Private Shared ReadOnly CLR_SUCCESS    As Color = Color.FromArgb(40,  167,  69)
    Private Shared ReadOnly CLR_ROW_ALT    As Color = Color.FromArgb(240, 245, 255)
    Private Shared ReadOnly CLR_GRID_HDR   As Color = Color.FromArgb(26,  58, 110)
    Private Shared ReadOnly CLR_BORDER     As Color = Color.FromArgb(220, 225, 235)
    Private Shared ReadOnly CLR_LBL_DIM    As Color = Color.FromArgb(120, 130, 150)
    Private Shared ReadOnly CLR_TXT        As Color = Color.FromArgb(30,  40,  60)

    ' ── Fonts ──────────────────────────────────────────────────────────────────
    Private Shared ReadOnly FNT_H1   As New Font("Segoe UI", 16, FontStyle.Bold)
    Private Shared ReadOnly FNT_H2   As New Font("Segoe UI",  9, FontStyle.Bold)
    Private Shared ReadOnly FNT_BODY As New Font("Segoe UI",  9, FontStyle.Regular)
    Private Shared ReadOnly FNT_MONO As New Font("Consolas",  9, FontStyle.Regular)
    Private Shared ReadOnly FNT_BTN  As New Font("Segoe UI",  9, FontStyle.Bold)
    Private Shared ReadOnly FNT_STAT As New Font("Segoe UI", 10, FontStyle.Bold)

    ' ── Controls ───────────────────────────────────────────────────────────────
    Private WithEvents btnCompute    As Button
    Private WithEvents btnLoadSample As Button
    Private WithEvents btnAddRow     As Button
    Private WithEvents btnRemoveRow  As Button

    Private inputGrid   As DataGridView
    Private resultsGrid As DataGridView

    Private txtStartBearing As TextBox
    Private txtStartN       As TextBox
    Private txtStartE       As TextBox

    Private lblAngularSum       As Label
    Private lblAngularMisc      As Label
    Private lblLinearMisc       As Label
    Private lblAccuracy         As Label
    Private lblArea             As Label
    Private pnlHeader           As Panel
    Private pnlStats            As Panel

    ' ── Leg result structure ───────────────────────────────────────────────────
    Structure LegResult
        Public FromStation    As String
        Public ToStation      As String
        Public IncludedAngle  As Double
        Public Distance       As Double
        Public ForwardBearing As Double
        Public BackBearing    As Double
        Public DN             As Double
        Public DE             As Double
        Public CorrDN         As Double
        Public CorrDE         As Double
        Public AdjDN          As Double
        Public AdjDE          As Double
        Public FinalN         As Double
        Public FinalE         As Double
    End Structure

    ' ══════════════════════════════════════════════════════════════════════════
    Public Sub New()
        BuildUserInterface()
        LoadSampleData()
    End Sub



    ' ══════════════════════════════════════════════════════════════════════════
    '  UI CONSTRUCTION
    ' ══════════════════════════════════════════════════════════════════════════
    Private Sub BuildUserInterface()
        ' ── Form shell ──
        Me.Text            = "SVY 323  ·  Closed Traverse Computation"
        Me.Width           = 1220
        Me.Height          = 880
        Me.MinimumSize     = New Size(1000, 750)
        Me.StartPosition   = FormStartPosition.CenterScreen
        Me.BackColor       = CLR_BG
        Me.Font            = FNT_BODY
        Me.ForeColor       = CLR_TXT

        ' ── Gradient header banner ──
        pnlHeader = New Panel With {
            .Dock   = DockStyle.Top,
            .Height = 72
        }
        AddHandler pnlHeader.Paint, AddressOf PaintHeader
        Me.Controls.Add(pnlHeader)

        Dim lblTitle As New Label With {
            .Text      = "SVY 323  —  Closed Traverse Computation",
            .Font      = FNT_H1,
            .ForeColor = Color.White,
            .AutoSize  = True,
            .Left      = 22,
            .Top       = 14
        }
        Dim lblSub As New Label With {
            .Text      = "Bowditch (Compass Rule) Adjustment  ·  Bearings  ·  DN/DE  ·  Final Coordinates  ·  Area",
            .Font      = New Font("Segoe UI", 8, FontStyle.Regular),
            .ForeColor = Color.FromArgb(180, 210, 255),
            .AutoSize  = True,
            .Left      = 24,
            .Top       = 46
        }
        pnlHeader.Controls.AddRange({lblTitle, lblSub})

        ' ── Input card ──
        Dim cardInput As Panel = MakeCard(10, 82, Me.Width - 28, 195)
        Me.Controls.Add(cardInput)

        Dim lblInputTitle As New Label With {
            .Text      = "CONTROL DATA  &  FIELD OBSERVATIONS",
            .Font      = FNT_H2,
            .ForeColor = CLR_ACCENT,
            .AutoSize  = True,
            .Left      = 14,
            .Top       = 12
        }
        cardInput.Controls.Add(lblInputTitle)

        ' Start bearing / N / E row
        Dim yRow As Integer = 38
        cardInput.Controls.Add(MakeLabel("Start Bearing (D  M  S):", 14, yRow + 3))
        txtStartBearing = MakeTextBox("60 0 0", 172, yRow, 90)
        cardInput.Controls.Add(txtStartBearing)

        cardInput.Controls.Add(MakeLabel("Start Northing (m):", 278, yRow + 3))
        txtStartN = MakeTextBox("1000.000", 420, yRow, 100)
        cardInput.Controls.Add(txtStartN)

        cardInput.Controls.Add(MakeLabel("Start Easting (m):", 534, yRow + 3))
        txtStartE = MakeTextBox("1000.000", 666, yRow, 100)
        cardInput.Controls.Add(txtStartE)

        ' Buttons
        btnLoadSample = MakeButton("⟳  Load Sample",  780, yRow - 1, 130, 28, CLR_ACCENT)
        btnAddRow     = MakeButton("+  Add Row",       918, yRow - 1, 105, 28, CLR_SUCCESS)
        btnRemoveRow  = MakeButton("−  Remove Row",   1027, yRow - 1, 120, 28, CLR_DANGER)
        cardInput.Controls.AddRange({btnLoadSample, btnAddRow, btnRemoveRow})

        ' Field data label
        Dim lblGridHdr As New Label With {
            .Text      = "Field Data  —  Station name, Included Angle (D M S), Distance to next station (m).  Minimum 5 rows.",
            .Font      = New Font("Segoe UI", 8, FontStyle.Italic),
            .ForeColor = CLR_LBL_DIM,
            .AutoSize  = True,
            .Left      = 14,
            .Top       = 74
        }
        cardInput.Controls.Add(lblGridHdr)

        ' Input DataGridView
        inputGrid = BuildInputGrid()
        inputGrid.Left   = 14
        inputGrid.Top    = 92
        inputGrid.Width  = cardInput.Width - 28
        inputGrid.Height = 90
        inputGrid.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        cardInput.Controls.Add(inputGrid)
    End Sub



    ' ── Second half of BuildUserInterface: Compute button, stats card, results grid ──
    Private Sub BuildUILowerHalf()
        Dim formW As Integer = Me.ClientSize.Width

        ' Compute button (full-width accent bar)
        btnCompute = New Button With {
            .Text      = "▶   COMPUTE TRAVERSE",
            .Font      = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = CLR_ACCENT_TXT,
            .BackColor = CLR_ACCENT,
            .FlatStyle = FlatStyle.Flat,
            .Height    = 42,
            .Left      = 10,
            .Top       = 284,
            .Width     = formW - 28,
            .Anchor    = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .Cursor    = Cursors.Hand
        }
        btnCompute.FlatAppearance.BorderSize = 0
        Me.Controls.Add(btnCompute)

        ' Stats card
        pnlStats = MakeCard(10, 336, formW - 28, 78)
        pnlStats.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(pnlStats)

        lblAngularSum  = MakeStatLabel("Sum of Angles / Misclosure: —",  14,  8)
        lblAngularMisc = MakeStatLabel("",                                14, 30)
        lblLinearMisc  = MakeStatLabel("Linear Misclosure: —",           440,  8)
        lblAccuracy    = MakeStatLabel("Accuracy: —",                    440, 30)
        lblArea        = MakeStatLabel("Area: —",                        440, 52)
        lblArea.ForeColor = CLR_ACCENT
        lblArea.Font      = New Font("Segoe UI", 10, FontStyle.Bold)
        pnlStats.Controls.AddRange({lblAngularSum, lblAngularMisc, lblLinearMisc, lblAccuracy, lblArea})

        ' Results label
        Dim lblResTitle As New Label With {
            .Text      = "COMPUTATION RESULTS  —  Bearings  ·  DN / DE  ·  Bowditch Corrections  ·  Final Coordinates",
            .Font      = FNT_H2,
            .ForeColor = CLR_ACCENT,
            .AutoSize  = True,
            .Left      = 22,
            .Top       = 424
        }
        Me.Controls.Add(lblResTitle)

        ' Results card + grid
        Dim cardResults As Panel = MakeCard(10, 444, formW - 28, 380)
        cardResults.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(cardResults)

        resultsGrid = BuildResultsGrid()
        resultsGrid.Left   = 6
        resultsGrid.Top    = 6
        resultsGrid.Width  = cardResults.Width  - 12
        resultsGrid.Height = cardResults.Height - 12
        resultsGrid.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        cardResults.Controls.Add(resultsGrid)
    End Sub

    ' Override OnLoad to call the lower-half builder after the form has a real ClientSize
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        BuildUILowerHalf()
    End Sub

    ' ── OnResize: keep the header card width in sync ──
    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        pnlHeader?.Invalidate()
    End Sub

    ' ──────────────────────────────────────────────────────────────────────────
    '  FACTORY HELPERS
    ' ──────────────────────────────────────────────────────────────────────────
    Private Function MakeCard(x As Integer, y As Integer, w As Integer, h As Integer) As Panel
        Dim p As New Panel With {
            .Left      = x,
            .Top       = y,
            .Width     = w,
            .Height    = h,
            .BackColor = CLR_CARD,
            .Padding   = New Padding(0)
        }
        AddHandler p.Paint, AddressOf PaintCardBorder
        Return p
    End Function

    Private Function MakeLabel(txt As String, x As Integer, y As Integer) As Label
        Return New Label With {
            .Text      = txt,
            .Left      = x,
            .Top       = y,
            .AutoSize  = True,
            .ForeColor = CLR_TXT
        }
    End Function

    Private Function MakeStatLabel(txt As String, x As Integer, y As Integer) As Label
        Return New Label With {
            .Text      = txt,
            .Left      = x,
            .Top       = y,
            .AutoSize  = True,
            .Font      = FNT_STAT,
            .ForeColor = CLR_TXT
        }
    End Function

    Private Function MakeTextBox(defaultText As String, x As Integer, y As Integer, w As Integer) As TextBox
        Return New TextBox With {
            .Text      = defaultText,
            .Left      = x,
            .Top       = y,
            .Width     = w,
            .Font      = FNT_BODY,
            .BackColor = Color.White,
            .ForeColor = CLR_TXT,
            .BorderStyle = BorderStyle.FixedSingle
        }
    End Function

    Private Function MakeButton(txt As String, x As Integer, y As Integer, w As Integer, h As Integer, bg As Color) As Button
        Dim b As New Button With {
            .Text      = txt,
            .Left      = x,
            .Top       = y,
            .Width     = w,
            .Height    = h,
            .Font      = FNT_BTN,
            .BackColor = bg,
            .ForeColor = CLR_ACCENT_TXT,
            .FlatStyle = FlatStyle.Flat,
            .Cursor    = Cursors.Hand
        }
        b.FlatAppearance.BorderSize = 0
        Return b
    End Function

    Private Function BuildInputGrid() As DataGridView
        Dim g As New DataGridView With {
            .AllowUserToAddRows    = False,
            .RowHeadersVisible     = False,
            .BorderStyle           = BorderStyle.None,
            .BackgroundColor       = CLR_CARD,
            .GridColor             = CLR_BORDER,
            .SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeRowsMode      = DataGridViewAutoSizeRowsMode.None,
            .ColumnHeadersHeight   = 28,
            .Font                  = FNT_BODY
        }
        g.RowTemplate.Height = 26
        StyleGridHeaders(g)
        g.Columns.Add("Station", "Station")
        g.Columns.Add("Angle",   "Included Angle  (D  M  S)")
        g.Columns.Add("Dist",    "Distance to Next Stn (m)")
        g.Columns(0).Width = 200
        g.Columns(1).Width = 260
        g.Columns(2).Width = 240
        AddHandler g.RowPostPaint, AddressOf AlternateRowColor
        Return g
    End Function

    Private Function BuildResultsGrid() As DataGridView
        Dim g As New DataGridView With {
            .AllowUserToAddRows  = False,
            .ReadOnly            = True,
            .RowHeadersVisible   = False,
            .BorderStyle         = BorderStyle.None,
            .BackgroundColor     = CLR_CARD,
            .GridColor           = CLR_BORDER,
            .SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .ColumnHeadersHeight = 30,
            .Font                = FNT_MONO
        }
        g.RowTemplate.Height = 26
        StyleGridHeaders(g)
        Dim cols() As String = {
            "Leg", "Angle (DMS)", "Dist (m)",
            "Fwd Bearing", "Back Bearing",
            "DN (m)", "DE (m)",
            "Corr.DN", "Corr.DE",
            "Adj.DN", "Adj.DE",
            "To Stn", "Final N", "Final E"
        }
        For Each c As String In cols
            g.Columns.Add(c, c)
        Next
        AddHandler g.RowPostPaint, AddressOf AlternateRowColor
        Return g
    End Function

    Private Sub StyleGridHeaders(g As DataGridView)
        g.EnableHeadersVisualStyles           = False
        g.ColumnHeadersDefaultCellStyle.BackColor   = CLR_GRID_HDR
        g.ColumnHeadersDefaultCellStyle.ForeColor   = Color.White
        g.ColumnHeadersDefaultCellStyle.Font        = FNT_H2
        g.ColumnHeadersDefaultCellStyle.Alignment   = DataGridViewContentAlignment.MiddleCenter
        g.DefaultCellStyle.ForeColor                = CLR_TXT
        g.DefaultCellStyle.Alignment                = DataGridViewContentAlignment.MiddleRight
        g.DefaultCellStyle.SelectionBackColor       = CLR_ACCENT
        g.DefaultCellStyle.SelectionForeColor       = Color.White
        g.AlternatingRowsDefaultCellStyle.BackColor = CLR_ROW_ALT
    End Sub

    ' ──────────────────────────────────────────────────────────────────────────
    '  PAINT HANDLERS
    ' ──────────────────────────────────────────────────────────────────────────
    Private Sub PaintHeader(sender As Object, e As PaintEventArgs)
        Dim p As Panel = CType(sender, Panel)
        Using br As New LinearGradientBrush(p.ClientRectangle, CLR_HEADER_TOP, CLR_HEADER_BOT, LinearGradientMode.Horizontal)
            e.Graphics.FillRectangle(br, p.ClientRectangle)
        End Using
    End Sub

    Private Sub PaintCardBorder(sender As Object, e As PaintEventArgs)
        Dim p As Panel = CType(sender, Panel)
        Using pen As New Pen(CLR_BORDER, 1)
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1)
        End Using
    End Sub

    Private Sub AlternateRowColor(sender As Object, e As DataGridViewRowPostPaintEventArgs)
        Dim g As DataGridView = CType(sender, DataGridView)
        If e.RowIndex Mod 2 = 1 Then
            g.Rows(e.RowIndex).DefaultCellStyle.BackColor = CLR_ROW_ALT
        Else
            g.Rows(e.RowIndex).DefaultCellStyle.BackColor = CLR_CARD
        End If
    End Sub



    ' ══════════════════════════════════════════════════════════════════════════
    '  SAMPLE DATA
    ' ══════════════════════════════════════════════════════════════════════════
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
    End Sub

    Private Sub AddRow_Click() Handles btnAddRow.Click
        inputGrid.Rows.Add("", "0 0 0", "0.00")
    End Sub

    Private Sub RemoveRow_Click() Handles btnRemoveRow.Click
        If inputGrid.Rows.Count > 0 AndAlso inputGrid.CurrentRow IsNot Nothing Then
            inputGrid.Rows.Remove(inputGrid.CurrentRow)
        End If
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  MAIN COMPUTATION
    ' ══════════════════════════════════════════════════════════════════════════
    Private Sub ComputeTraverse_Click() Handles btnCompute.Click
        Try
            Dim rowCount As Integer = inputGrid.Rows.Count
            If rowCount < 5 Then
                MessageBox.Show(
                    "Please enter at least 5 stations as required by the assignment.",
                    "Insufficient Data", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim n As Integer = rowCount
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

            ' ── 1. Angular misclosure ──────────────────────────────────────
            Dim sumAngles As Double = 0
            For Each a As Double In includedAngles
                sumAngles += a
            Next
            Dim theoreticalSum     As Double = (n - 2) * 180.0
            Dim angularMisclosure  As Double = sumAngles - theoreticalSum

            lblAngularSum.Text  = "Sum of Angles = " & DecimalToDMS(sumAngles) &
                                  "     Theoretical = " & DecimalToDMS(theoreticalSum)
            lblAngularMisc.Text = "Angular Misclosure = " & DecimalToDMS(angularMisclosure)

            ' Distribute angular correction equally across all stations
            Dim angCorrPerStn As Double = -angularMisclosure / n
            For i As Integer = 0 To n - 1
                includedAngles(i) += angCorrPerStn
            Next

            ' ── 2. Bearings, DN, DE ───────────────────────────────────────
            '  FIX: interior-angle traverses use ADDITION not subtraction:
            '       Forward bearing(i) = Back bearing(i-1) + Interior angle(i)
            Dim legs(n - 1) As LegResult
            Dim fb As Double = startBearing

            For i As Integer = 0 To n - 1
                If i > 0 Then
                    ' Back bearing of previous leg
                    Dim bbPrev As Double = (legs(i - 1).ForwardBearing + 180.0) Mod 360.0
                    ' *** CORRECTED: + not - ***
                    fb = (bbPrev + includedAngles(i)) Mod 360.0
                    If fb < 0 Then fb += 360.0
                End If

                Dim bb   As Double = (fb + 180.0) Mod 360.0
                Dim dist As Double = distances(i)
                Dim rad  As Double = fb * Math.PI / 180.0

                legs(i).FromStation    = stationNames(i)
                legs(i).ToStation      = stationNames((i + 1) Mod n)
                legs(i).IncludedAngle  = includedAngles(i)
                legs(i).Distance       = dist
                legs(i).ForwardBearing = fb
                legs(i).BackBearing    = bb
                legs(i).DN             = dist * Math.Cos(rad)
                legs(i).DE             = dist * Math.Sin(rad)
            Next

            ' ── 3. Linear misclosure & accuracy ───────────────────────────
            Dim sumDN As Double = 0, sumDE As Double = 0, perimeter As Double = 0
            For i As Integer = 0 To n - 1
                sumDN     += legs(i).DN
                sumDE     += legs(i).DE
                perimeter += legs(i).Distance
            Next
            Dim linearMisclosure    As Double = Math.Sqrt(sumDN ^ 2 + sumDE ^ 2)
            Dim accuracyDenominator As Double = If(linearMisclosure = 0, 0, perimeter / linearMisclosure)

            lblLinearMisc.Text = String.Format(
                "Linear Misclosure = {0:0.0000} m     eN = {1:0.0000} m,  eE = {2:0.0000} m",
                linearMisclosure, sumDN, sumDE)
            lblAccuracy.Text = If(linearMisclosure = 0,
                "Accuracy = Perfect Closure",
                String.Format("Linear Accuracy = 1 : {0:0}", accuracyDenominator))

            ' ── 4. Bowditch adjustment ─────────────────────────────────────
            For i As Integer = 0 To n - 1
                legs(i).CorrDN = -sumDN * (legs(i).Distance / perimeter)
                legs(i).CorrDE = -sumDE * (legs(i).Distance / perimeter)
                legs(i).AdjDN  = legs(i).DN + legs(i).CorrDN
                legs(i).AdjDE  = legs(i).DE + legs(i).CorrDE
            Next

            ' ── 5. Final coordinates ──────────────────────────────────────
            Dim finalN As New Dictionary(Of String, Double)
            Dim finalE As New Dictionary(Of String, Double)
            finalN(stationNames(0)) = startN
            finalE(stationNames(0)) = startE

            Dim curN As Double = startN
            Dim curE As Double = startE
            For i As Integer = 0 To n - 1
                curN += legs(i).AdjDN
                curE += legs(i).AdjDE
                legs(i).FinalN = curN
                legs(i).FinalE = curE
                ' Use index-keyed store to avoid duplicate-name overwrite issues
                finalN(legs(i).ToStation) = curN
                finalE(legs(i).ToStation) = curE
            Next

            ' ── 6. Area (Shoelace) ─────────────────────────────────────────
            Dim area As Double = 0
            For i As Integer = 0 To n - 1
                Dim s1 As String = stationNames(i)
                Dim s2 As String = stationNames((i + 1) Mod n)
                area += finalE(s1) * finalN(s2) - finalE(s2) * finalN(s1)
            Next
            area = Math.Abs(area) / 2.0
            lblArea.Text = String.Format(
                "Area = {0:0.000} m²     ({1:0.0000} hectares)", area, area / 10000.0)

            ' ── 7. Populate results grid ───────────────────────────────────
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
            MessageBox.Show("Error in input data: " & ex.Message,
                            "Computation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub



    ' ══════════════════════════════════════════════════════════════════════════
    '  HELPER FUNCTIONS
    ' ══════════════════════════════════════════════════════════════════════════

    ''' <summary>
    ''' Parses "D M S" (space-separated) or a plain decimal-degree string
    ''' into decimal degrees.  Handles negative degrees correctly.
    ''' </summary>
    Private Function ParseDMS(text As String) As Double
        text = text.Trim()
        If text.Contains(" ") Then
            Dim parts() As String = text.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
            Dim deg  As Double = Convert.ToDouble(parts(0))
            Dim mins As Double = If(parts.Length > 1, Convert.ToDouble(parts(1)), 0)
            Dim secs As Double = If(parts.Length > 2, Convert.ToDouble(parts(2)), 0)
            Dim sign As Double = If(deg < 0, -1.0, 1.0)
            Return sign * (Math.Abs(deg) + mins / 60.0 + secs / 3600.0)
        Else
            Return Convert.ToDouble(text)
        End If
    End Function

    ''' <summary>
    ''' Formats decimal degrees as D°MM'SS.S" for display.
    ''' Works correctly for both positive and negative values.
    ''' </summary>
    Private Function DecimalToDMS(decimalDeg As Double) As String
        Dim sign   As String  = If(decimalDeg < 0, "-", "")
        Dim absVal As Double  = Math.Abs(decimalDeg)
        Dim deg    As Integer = CInt(Math.Truncate(absVal))
        Dim minFull As Double = (absVal - deg) * 60.0
        Dim mins   As Integer = CInt(Math.Truncate(minFull))
        Dim secs   As Double  = (minFull - mins) * 60.0
        ' Guard against floating-point rounding pushing seconds to 60
        If secs >= 60.0 Then
            secs  = 0
            mins += 1
        End If
        If mins >= 60 Then
            mins  = 0
            deg  += 1
        End If
        Return String.Format("{0}{1}{2}'{3:00.0}""",
                             sign, deg.ToString() & ChrW(176),
                             mins.ToString("00"), secs)
    End Function

End Class
