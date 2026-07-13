'==============================================================================
'  SVY 323 — COMPUTER APPLICATION I
'  Closed Traverse Computation — Windows Forms GUI
'  Clean rewrite: modern design tokens, animated controls, Bowditch rule.
'==============================================================================
Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Collections.Generic

Public Class Form1
    Inherits Form

    '==========================================================================
    '  DESIGN TOKENS
    '==========================================================================
    Private Shared ReadOnly S_PAGE      As Color = Color.FromArgb(250, 250, 249)
    Private Shared ReadOnly S_CARD      As Color = Color.White
    Private Shared ReadOnly S_CARD_ALT  As Color = Color.FromArgb(249, 250, 251)
    Private Shared ReadOnly S_HEADER    As Color = Color.White

    Private Shared ReadOnly A_BASE      As Color = Color.FromArgb(79,  70, 229)
    Private Shared ReadOnly A_HOVER     As Color = Color.FromArgb(67,  56, 202)
    Private Shared ReadOnly A_PRESS     As Color = Color.FromArgb(55,  48, 163)
    Private Shared ReadOnly A_TINT      As Color = Color.FromArgb(238, 242, 255)

    Private Shared ReadOnly T_HIGH      As Color = Color.FromArgb(15,  23,  42)
    Private Shared ReadOnly T_BODY      As Color = Color.FromArgb(71,  85, 105)
    Private Shared ReadOnly T_MUTED     As Color = Color.FromArgb(148, 163, 184)
    Private Shared ReadOnly T_ON_ACC    As Color = Color.White

    Private Shared ReadOnly B_DEFAULT   As Color = Color.FromArgb(228, 228, 231)

    Private Shared ReadOnly C_SUCCESS   As Color = Color.FromArgb(22,  163,  74)
    Private Shared ReadOnly C_DANGER    As Color = Color.FromArgb(220,  38,  38)
    Private Shared ReadOnly C_DANGER_TNT As Color = Color.FromArgb(254, 242, 242)

    '==========================================================================
    '  TYPOGRAPHY  (system fonts — no PrivateFontCollection needed)
    '==========================================================================
    Private Const FACE      As String = "Segoe UI Variable"
    Private Const FACE_MONO As String = "Cascadia Mono"

    Private Shared ReadOnly F_DISPLAY As New Font(FACE, 17,   FontStyle.Bold)
    Private Shared ReadOnly F_BODY    As New Font(FACE,  9.5F, FontStyle.Regular)
    Private Shared ReadOnly F_BODY_B  As New Font(FACE,  9.5F, FontStyle.Bold)
    Private Shared ReadOnly F_CAPTION As New Font(FACE,  7.5F, FontStyle.Bold)
    Private Shared ReadOnly F_SUBTLE  As New Font(FACE,  9,    FontStyle.Regular)
    Private Shared ReadOnly F_BTN     As New Font(FACE,  9.5F, FontStyle.Bold)
    Private Shared ReadOnly F_BTN_LG  As New Font(FACE, 10.5F, FontStyle.Bold)
    Private Shared ReadOnly F_STAT_K  As New Font(FACE,  7.5F, FontStyle.Bold)
    Private Shared ReadOnly F_STAT_V  As New Font(FACE, 11,    FontStyle.Bold)
    Private Shared ReadOnly F_GRID_H  As New Font(FACE,  8,    FontStyle.Bold)
    Private Shared ReadOnly F_GRID_C  As New Font(FACE_MONO, 9, FontStyle.Regular)

    '==========================================================================
    '  CONTROL FIELDS
    '==========================================================================
    Private WithEvents btnCompute    As RoundButton
    Private WithEvents btnLoadSample As RoundButton
    Private WithEvents btnAddRow     As RoundButton
    Private WithEvents btnRemoveRow  As RoundButton
    Private WithEvents btnReset      As RoundButton
    Private sampleMenu               As ContextMenuStrip

    Private inputGrid   As DataGridView
    Private resultsGrid As DataGridView

    Private txtStartBearing As InputField
    Private txtStartN       As InputField
    Private txtStartE       As InputField

    Private lblAngularVal  As Label
    Private lblMisclosVal  As Label
    Private lblLinearVal   As Label
    Private lblAccuracyVal As Label
    Private lblAreaVal     As Label

    '==========================================================================
    '  DATA STRUCTURE
    '==========================================================================
    Private Structure LegResult
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

    '==========================================================================
    '  DWM ROUNDED CORNERS  (Windows 11+; silently no-ops on older builds)
    '==========================================================================
    <DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer,
                                                   ByRef value As Integer,
                                                   size As Integer) As Integer
    End Function

    Private Const DWMWA_WINDOW_CORNER_PREFERENCE As Integer = 33
    Private Const DWMWCP_ROUND                   As Integer = 2

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        Try
            Dim pref As Integer = DWMWCP_ROUND
            DwmSetWindowAttribute(Me.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, pref, 4)
        Catch
        End Try
    End Sub



    '==========================================================================
    '  CONSTRUCTOR
    '==========================================================================
    Public Sub New()
        Me.SuspendLayout()
        BuildUI()
        Me.ResumeLayout(False)
        Me.PerformLayout()
        LoadSample_Pentagon(Nothing, EventArgs.Empty)
    End Sub

    '==========================================================================
    '  BUILD UI
    '==========================================================================
    Private Sub BuildUI()
        Me.Text          = "SVY 323  ·  Closed Traverse Computation"
        Me.ClientSize    = New Size(1280, 840)
        Me.MinimumSize   = New Size(1120, 740)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor     = S_PAGE
        Me.Font          = F_BODY

        Dim outer As New TableLayoutPanel With {
            .Dock        = DockStyle.Fill,
            .RowCount    = 2,
            .ColumnCount = 1,
            .BackColor   = S_PAGE,
            .Font        = F_BODY
        }
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 76))
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        Me.Controls.Add(outer)

        BuildHeader(outer)

        Dim body As New TableLayoutPanel With {
            .Dock        = DockStyle.Fill,
            .RowCount    = 4,
            .ColumnCount = 1,
            .Padding     = New Padding(20, 14, 20, 16),
            .BackColor   = S_PAGE,
            .Font        = F_BODY
        }
        body.RowStyles.Add(New RowStyle(SizeType.Absolute, 232))
        body.RowStyles.Add(New RowStyle(SizeType.Absolute,  58))
        body.RowStyles.Add(New RowStyle(SizeType.Absolute, 104))
        body.RowStyles.Add(New RowStyle(SizeType.Percent,  100))
        body.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        outer.Controls.Add(body, 0, 1)

        BuildInputCard(body)
        BuildComputeBar(body)
        BuildStatsCard(body)
        BuildResultsCard(body)
    End Sub

    '--------------------------------------------------------------------------
    '  Header
    '--------------------------------------------------------------------------
    Private Sub BuildHeader(outer As TableLayoutPanel)
        Dim header As New Panel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_HEADER,
            .Font      = F_BODY
        }
        AddHandler header.Paint, AddressOf OnHeaderPaint
        outer.Controls.Add(header, 0, 0)

        header.Controls.Add(New Label With {
            .Text      = "SVY 323 — Closed Traverse Computation",
            .Font      = F_DISPLAY,
            .ForeColor = T_HIGH,
            .AutoSize  = True,
            .Left      = 64,
            .Top       = 16
        })
        header.Controls.Add(New Label With {
            .Text      = "Angular adjustment · Bowditch rule · bearings · latitudes & departures · final coordinates · area",
            .Font      = F_SUBTLE,
            .ForeColor = T_MUTED,
            .AutoSize  = True,
            .Left      = 65,
            .Top       = 44
        })

        Dim mark As New Panel With {
            .Left      = 20,
            .Top       = 20,
            .Width     = 36,
            .Height    = 36,
            .BackColor = Color.Transparent
        }
        AddHandler mark.Paint, AddressOf OnMarkPaint
        header.Controls.Add(mark)
    End Sub

    Private Sub OnMarkPaint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim r As New Rectangle(0, 0, 35, 35)
        Using path As New GraphicsPath()
            Dim rad As Integer = 20
            path.AddArc(r.X, r.Y, rad, rad, 180, 90)
            path.AddArc(r.Right - rad, r.Y, rad, rad, 270, 90)
            path.AddArc(r.Right - rad, r.Bottom - rad, rad, rad, 0, 90)
            path.AddArc(r.X, r.Bottom - rad, rad, rad, 90, 90)
            path.CloseFigure()
            Using br As New SolidBrush(A_BASE)
                g.FillPath(br, path)
            End Using
        End Using
        Dim p1 As New Point(18, 9)
        Dim p2 As New Point(9,  25)
        Dim p3 As New Point(27, 25)
        Using pen As New Pen(Color.FromArgb(220, Color.White), 1.4F)
            g.DrawLine(pen, p1, p2)
            g.DrawLine(pen, p2, p3)
            g.DrawLine(pen, p3, p1)
        End Using
        For Each pt As Point In New Point() {p1, p2, p3}
            Using br As New SolidBrush(Color.White)
                g.FillEllipse(br, pt.X - 2.5F, pt.Y - 2.5F, 5, 5)
            End Using
        Next
    End Sub

    Private Sub OnHeaderPaint(sender As Object, e As PaintEventArgs)
        Dim p As Panel = DirectCast(sender, Panel)
        Using pen As New Pen(B_DEFAULT, 1)
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1)
        End Using
    End Sub



    '--------------------------------------------------------------------------
    '  Input card
    '--------------------------------------------------------------------------
    Private Sub BuildInputCard(body As TableLayoutPanel)
        Const PAD_L As Integer = 22
        Const PAD_T As Integer = 16
        Const PAD_R As Integer = 22

        Dim cardInput As New ShadowPanel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_CARD,
            .Font      = F_BODY
        }
        body.Controls.Add(cardInput, 0, 0)

        cardInput.Controls.Add(MakeSectionLabel("CONTROL DATA  &  FIELD OBSERVATIONS", PAD_L, PAD_T))

        Dim flowCtrl As New FlowLayoutPanel With {
            .FlowDirection = FlowDirection.LeftToRight,
            .AutoSize      = True,
            .Left          = PAD_L,
            .Top           = PAD_T + 28,
            .Height        = 40,
            .WrapContents  = False,
            .BackColor     = Color.Transparent,
            .Font          = F_BODY
        }

        flowCtrl.Controls.Add(MakeFieldLabel("Start bearing (D M S)"))
        txtStartBearing = MakeInput("60 0 0", 92)
        flowCtrl.Controls.Add(txtStartBearing)
        flowCtrl.Controls.Add(MakeSpacer(18))

        flowCtrl.Controls.Add(MakeFieldLabel("Start northing (m)"))
        txtStartN = MakeInput("1000.000", 100)
        flowCtrl.Controls.Add(txtStartN)
        flowCtrl.Controls.Add(MakeSpacer(18))

        flowCtrl.Controls.Add(MakeFieldLabel("Start easting (m)"))
        txtStartE = MakeInput("1000.000", 100)
        flowCtrl.Controls.Add(txtStartE)
        flowCtrl.Controls.Add(MakeSpacer(28))

        btnLoadSample = MakeBtn("Sample data  ▾", "secondary",    128, 38)
        btnAddRow     = MakeBtn("+ Add row",       "secondary",     96, 38)
        btnRemoveRow  = MakeBtn("− Remove row",    "secondary",    112, 38)
        btnReset      = MakeBtn("Reset",           "danger-ghost",  78, 38)

        flowCtrl.Controls.Add(btnLoadSample)
        flowCtrl.Controls.Add(MakeSpacer(8))
        flowCtrl.Controls.Add(btnAddRow)
        flowCtrl.Controls.Add(MakeSpacer(6))
        flowCtrl.Controls.Add(btnRemoveRow)
        flowCtrl.Controls.Add(MakeSpacer(6))
        flowCtrl.Controls.Add(btnReset)
        cardInput.Controls.Add(flowCtrl)

        sampleMenu          = New ContextMenuStrip()
        sampleMenu.Font     = F_BODY
        sampleMenu.BackColor = S_CARD
        sampleMenu.Renderer = New ToolStripProfessionalRenderer(New ModernMenuColors())
        sampleMenu.Items.Add("Pentagon  (5 stations, 108° each)",        Nothing, AddressOf LoadSample_Pentagon)
        sampleMenu.Items.Add("Triangle  (3 stations, equilateral ~60°)",  Nothing, AddressOf LoadSample_Triangle)
        sampleMenu.Items.Add("Quadrilateral  (4 stations, mixed angles)", Nothing, AddressOf LoadSample_Quad)
        sampleMenu.Items.Add("Hexagon  (6 stations, irregular)",          Nothing, AddressOf LoadSample_Hex)

        inputGrid = New DataGridView With {
            .Left              = PAD_L,
            .Top               = PAD_T + 86,
            .Height            = 104,
            .AllowUserToAddRows = False,
            .BackgroundColor   = S_CARD,
            .SelectionMode     = DataGridViewSelectionMode.FullRowSelect,
            .Font              = F_BODY,
            .ScrollBars        = ScrollBars.Vertical
        }
        ApplyGridStyle(inputGrid)
        inputGrid.Columns.Add("col_stn",  "STATION")
        inputGrid.Columns.Add("col_ang",  "INCLUDED ANGLE (D  M  S)")
        inputGrid.Columns.Add("col_dist", "DISTANCE TO NEXT STATION (M)")
        inputGrid.Columns(0).Width        = 160
        inputGrid.Columns(1).Width        = 240
        inputGrid.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        cardInput.Controls.Add(inputGrid)

        AddHandler cardInput.Resize,
            Sub(s As Object, ev As EventArgs)
                Dim w As Integer = cardInput.ClientSize.Width - PAD_L - PAD_R - ShadowPanel.SHADOW_DEPTH
                If w > 50 Then inputGrid.Width = w
            End Sub
    End Sub

    '--------------------------------------------------------------------------
    '  Compute bar
    '--------------------------------------------------------------------------
    Private Sub BuildComputeBar(body As TableLayoutPanel)
        Dim pnlCompute As New Panel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_PAGE,
            .Padding   = New Padding(0, 10, 0, 10),
            .Font      = F_BODY
        }
        body.Controls.Add(pnlCompute, 0, 1)

        btnCompute = New RoundButton With {
            .Text       = "▶   COMPUTE TRAVERSE",
            .Dock       = DockStyle.Fill,
            .Font       = F_BTN_LG,
            .FillColor  = A_BASE,
            .HoverColor = A_HOVER,
            .PressColor = A_PRESS,
            .TextColor  = T_ON_ACC,
            .Radius     = 12,
            .Cursor     = Cursors.Hand,
            .TabStop    = False
        }
        pnlCompute.Controls.Add(btnCompute)
    End Sub

    '--------------------------------------------------------------------------
    '  Stats card
    '--------------------------------------------------------------------------
    Private Sub BuildStatsCard(body As TableLayoutPanel)
        Dim cardStats As New ShadowPanel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_CARD,
            .Padding   = New Padding(14, 12, 14 + ShadowPanel.SHADOW_DEPTH, 12 + ShadowPanel.SHADOW_DEPTH),
            .Font      = F_BODY
        }
        body.Controls.Add(cardStats, 0, 2)

        Dim statFlow As New TableLayoutPanel With {
            .Dock        = DockStyle.Fill,
            .ColumnCount = 5,
            .RowCount    = 1,
            .BackColor   = Color.Transparent,
            .Font        = F_BODY
        }
        For i As Integer = 0 To 4
            statFlow.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
        Next
        statFlow.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        cardStats.Controls.Add(statFlow)

        lblAngularVal  = AddStatTile(statFlow, 0, "SUM OF ANGLES",      "—", A_BASE)
        lblMisclosVal  = AddStatTile(statFlow, 1, "ANGULAR MISCLOSURE", "—", A_BASE)
        lblLinearVal   = AddStatTile(statFlow, 2, "LINEAR MISCLOSURE",  "—", A_BASE)
        lblAccuracyVal = AddStatTile(statFlow, 3, "LINEAR ACCURACY",    "—", A_BASE)
        lblAreaVal     = AddStatTile(statFlow, 4, "ENCLOSED AREA",      "—", A_BASE)
    End Sub



    '--------------------------------------------------------------------------
    '  Results card
    '--------------------------------------------------------------------------
    Private Sub BuildResultsCard(body As TableLayoutPanel)
        Dim cardResults As New ShadowPanel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_CARD,
            .Padding   = New Padding(0, 0, ShadowPanel.SHADOW_DEPTH, ShadowPanel.SHADOW_DEPTH),
            .Font      = F_BODY
        }
        body.Controls.Add(cardResults, 0, 3)

        cardResults.Controls.Add(New Label With {
            .Text      = "COMPUTATION RESULTS",
            .Font      = F_CAPTION,
            .ForeColor = T_MUTED,
            .AutoSize  = True,
            .Left      = 18,
            .Top       = 12
        })

        resultsGrid = New DataGridView With {
            .Left                  = 0,
            .Top                   = 36,
            .AllowUserToAddRows    = False,
            .ReadOnly              = True,
            .BackgroundColor       = S_CARD,
            .SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            .Font                  = F_GRID_C,
            .ScrollBars            = ScrollBars.Both
        }
        ApplyGridStyle(resultsGrid)

        Dim rCols() As String = {
            "LEG", "ANGLE", "DIST (M)", "FWD BEARING", "BACK BEARING",
            "DN (M)", "DE (M)", "CORR DN", "CORR DE", "ADJ DN", "ADJ DE",
            "TO STN", "FINAL N", "FINAL E"
        }
        For Each c As String In rCols
            resultsGrid.Columns.Add(c, c)
        Next
        cardResults.Controls.Add(resultsGrid)

        AddHandler cardResults.Resize,
            Sub(s As Object, ev As EventArgs)
                resultsGrid.Width  = cardResults.ClientSize.Width  - ShadowPanel.SHADOW_DEPTH
                resultsGrid.Height = cardResults.ClientSize.Height - 36 - ShadowPanel.SHADOW_DEPTH
            End Sub
    End Sub

    '==========================================================================
    '  FACTORY / STYLE HELPERS
    '==========================================================================
    Private Function MakeSectionLabel(txt As String, x As Integer, y As Integer) As Label
        Return New Label With {
            .Text      = txt,
            .Font      = F_CAPTION,
            .ForeColor = T_MUTED,
            .AutoSize  = True,
            .Left      = x,
            .Top       = y
        }
    End Function

    Private Function MakeFieldLabel(txt As String) As Label
        Return New Label With {
            .Text      = txt,
            .Font      = F_BODY,
            .ForeColor = T_BODY,
            .AutoSize  = False,
            .Width     = TextRenderer.MeasureText(txt, F_BODY).Width + 8,
            .Height    = 40,
            .TextAlign = ContentAlignment.MiddleRight,
            .Margin    = New Padding(0, 0, 6, 0)
        }
    End Function

    Private Function MakeInput(defaultVal As String, w As Integer) As InputField
        Dim f As New InputField With {
            .Width  = w,
            .Height = 40,
            .Margin = New Padding(0, 0, 0, 0)
        }
        f.EditBox.Font = F_BODY
        f.EditBox.Text = defaultVal
        Return f
    End Function

    Private Function MakeSpacer(w As Integer) As Panel
        Return New Panel With {
            .Width     = w,
            .Height    = 40,
            .BackColor = Color.Transparent
        }
    End Function

    Private Function MakeBtn(txt As String, btnStyle As String, w As Integer, h As Integer) As RoundButton
        Dim b As New RoundButton With {
            .Text    = txt,
            .Width   = w,
            .Height  = h,
            .Font    = F_BTN,
            .Cursor  = Cursors.Hand,
            .TabStop = False,
            .Radius  = 9
        }
        Select Case btnStyle
            Case "primary"
                b.FillColor  = A_BASE
                b.HoverColor = A_HOVER
                b.PressColor = A_PRESS
                b.TextColor  = T_ON_ACC
                b.HasBorder  = False
            Case "secondary"
                b.FillColor   = S_CARD
                b.HoverColor  = S_CARD_ALT
                b.PressColor  = B_DEFAULT
                b.TextColor   = T_BODY
                b.HasBorder   = True
                b.BorderColor = B_DEFAULT
            Case "danger-ghost"
                b.FillColor   = S_CARD
                b.HoverColor  = C_DANGER_TNT
                b.PressColor  = C_DANGER_TNT
                b.TextColor   = C_DANGER
                b.HasBorder   = True
                b.BorderColor = B_DEFAULT
        End Select
        Return b
    End Function

    '--------------------------------------------------------------------------
    '  Stat tile — named paint handler so no inline multi-statement lambda
    '--------------------------------------------------------------------------
    Private _statTileAccent As Color = A_BASE   ' set before each AddStatTile call
    Private _statTilePanelRef As Panel = Nothing ' reference captured for named handler

    Private Sub StatTilePaint(sender As Object, e As PaintEventArgs)
        Dim cell As Panel = DirectCast(sender, Panel)
        Using br As New SolidBrush(CType(cell.Tag, Color))
            e.Graphics.FillRectangle(br, 0, 0, cell.Width, 3)
        End Using
        Using pen As New Pen(B_DEFAULT, 1)
            e.Graphics.DrawRectangle(pen, 0, 0, cell.Width - 1, cell.Height - 1)
        End Using
    End Sub

    Private Function AddStatTile(tbl As TableLayoutPanel, col As Integer,
                                  key As String, initialVal As String,
                                  accent As Color) As Label
        Dim outer As New Panel With {
            .Dock      = DockStyle.Fill,
            .Padding   = New Padding(4),
            .BackColor = Color.Transparent
        }
        Dim cell As New Panel With {
            .Dock      = DockStyle.Fill,
            .BackColor = S_CARD_ALT,
            .Tag       = accent
        }
        AddHandler cell.Paint, AddressOf StatTilePaint
        outer.Controls.Add(cell)

        cell.Controls.Add(New Label With {
            .Text      = key,
            .Font      = F_STAT_K,
            .ForeColor = T_MUTED,
            .AutoSize  = True,
            .Left      = 14,
            .Top       = 10
        })
        Dim valLbl As New Label With {
            .Text      = initialVal,
            .Font      = F_STAT_V,
            .ForeColor = T_HIGH,
            .AutoSize  = True,
            .Left      = 14,
            .Top       = 26
        }
        cell.Controls.Add(valLbl)
        tbl.Controls.Add(outer, col, 0)
        Return valLbl
    End Function

    Private Sub ApplyGridStyle(gv As DataGridView)
        gv.EnableHeadersVisualStyles                        = False
        gv.BorderStyle                                      = BorderStyle.None
        gv.CellBorderStyle                                  = DataGridViewCellBorderStyle.SingleHorizontal
        gv.ColumnHeadersBorderStyle                         = DataGridViewHeaderBorderStyle.None
        gv.RowHeadersVisible                                = False
        gv.BackgroundColor                                  = S_CARD
        gv.GridColor                                        = B_DEFAULT
        gv.ColumnHeadersHeight                              = 36
        gv.RowTemplate.Height                               = 34
        gv.ColumnHeadersDefaultCellStyle.BackColor          = S_CARD_ALT
        gv.ColumnHeadersDefaultCellStyle.ForeColor          = T_BODY
        gv.ColumnHeadersDefaultCellStyle.Font               = F_GRID_H
        gv.ColumnHeadersDefaultCellStyle.Alignment          = DataGridViewContentAlignment.MiddleLeft
        gv.ColumnHeadersDefaultCellStyle.Padding            = New Padding(12, 0, 0, 0)
        gv.DefaultCellStyle.BackColor                       = S_CARD
        gv.DefaultCellStyle.ForeColor                       = T_BODY
        gv.DefaultCellStyle.SelectionBackColor              = A_TINT
        gv.DefaultCellStyle.SelectionForeColor              = T_HIGH
        gv.DefaultCellStyle.Padding                         = New Padding(12, 0, 8, 0)
        gv.AlternatingRowsDefaultCellStyle.BackColor        = S_CARD_ALT
    End Sub



    '==========================================================================
    '  SAMPLE DATA
    '==========================================================================
    Private Sub LoadSampleData() Handles btnLoadSample.Click
        sampleMenu.Show(btnLoadSample, New Point(0, btnLoadSample.Height + 4))
    End Sub

    Private Sub LoadSample_Pentagon(sender As Object, e As EventArgs)
        ClearAll()
        txtStartBearing.EditBox.Text = "60 0 0"
        txtStartN.EditBox.Text       = "1000.000"
        txtStartE.EditBox.Text       = "1000.000"
        inputGrid.Rows.Add("A", "108 0 0", "100.02")
        inputGrid.Rows.Add("B", "108 0 0",  "99.97")
        inputGrid.Rows.Add("C", "108 0 0", "100.05")
        inputGrid.Rows.Add("D", "108 0 0",  "99.94")
        inputGrid.Rows.Add("E", "108 0 0", "100.03")
    End Sub

    Private Sub LoadSample_Triangle(sender As Object, e As EventArgs)
        ClearAll()
        txtStartBearing.EditBox.Text = "30 0 0"
        txtStartN.EditBox.Text       = "500.000"
        txtStartE.EditBox.Text       = "500.000"
        inputGrid.Rows.Add("P", "60 0 10",  "120.05")
        inputGrid.Rows.Add("Q", "59 59 40", "119.98")
        inputGrid.Rows.Add("R", "60 0 20",  "120.00")
    End Sub

    Private Sub LoadSample_Quad(sender As Object, e As EventArgs)
        ClearAll()
        txtStartBearing.EditBox.Text = "45 0 0"
        txtStartN.EditBox.Text       = "2000.000"
        txtStartE.EditBox.Text       = "1500.000"
        inputGrid.Rows.Add("W", "95 12 30",  "85.44")
        inputGrid.Rows.Add("X", "82 47 50", "102.18")
        inputGrid.Rows.Add("Y", "91 35 20",  "78.65")
        inputGrid.Rows.Add("Z", "90 24 40",  "93.30")
    End Sub

    Private Sub LoadSample_Hex(sender As Object, e As EventArgs)
        ClearAll()
        txtStartBearing.EditBox.Text = "22 30 0"
        txtStartN.EditBox.Text       = "5000.000"
        txtStartE.EditBox.Text       = "5000.000"
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
        txtStartBearing.EditBox.Text = "0 0 0"
        txtStartN.EditBox.Text       = "0.000"
        txtStartE.EditBox.Text       = "0.000"
    End Sub

    Private Sub ClearAll()
        inputGrid.Rows.Clear()
        resultsGrid.Rows.Clear()
        For Each lbl As Label In New Label() {lblAngularVal, lblMisclosVal, lblLinearVal, lblAccuracyVal, lblAreaVal}
            lbl.Text      = "—"
            lbl.ForeColor = T_HIGH
        Next
    End Sub



    '==========================================================================
    '  MAIN COMPUTATION
    '==========================================================================
    Private Sub ComputeTraverse_Click() Handles btnCompute.Click
        Try
            Dim n As Integer = inputGrid.Rows.Count
            If n < 3 Then
                MessageBox.Show("Please enter at least 3 stations (minimum for a closed traverse).",
                                "Insufficient Data", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim stationNames(n - 1)   As String
            Dim includedAngles(n - 1) As Double
            Dim distances(n - 1)      As Double

            For i As Integer = 0 To n - 1
                stationNames(i)   = CStr(inputGrid.Rows(i).Cells(0).Value).Trim()
                includedAngles(i) = ParseDMS(CStr(inputGrid.Rows(i).Cells(1).Value))
                distances(i)      = Convert.ToDouble(inputGrid.Rows(i).Cells(2).Value)
            Next

            Dim startBearing As Double = ParseDMS(txtStartBearing.EditBox.Text)
            Dim startN       As Double = Convert.ToDouble(txtStartN.EditBox.Text)
            Dim startE       As Double = Convert.ToDouble(txtStartE.EditBox.Text)

            ' --- Angular misclosure ---
            Dim sumAngles  As Double = 0
            For Each a As Double In includedAngles
                sumAngles += a
            Next
            Dim theoretical As Double = (n - 2) * 180.0
            Dim angMisclos  As Double = sumAngles - theoretical

            lblAngularVal.Text  = DecimalToDMS(sumAngles) & "  (th. " & DecimalToDMS(theoretical) & ")"
            lblMisclosVal.Text  = DecimalToDMS(angMisclos)
            lblMisclosVal.ForeColor = If(Math.Abs(angMisclos) < 0.01, C_SUCCESS, C_DANGER)

            ' --- Distribute angular correction evenly ---
            Dim corrPerStn As Double = -angMisclos / n
            For i As Integer = 0 To n - 1
                includedAngles(i) += corrPerStn
            Next

            ' --- Bearings, DN, DE ---
            Dim legs(n - 1) As LegResult
            Dim fb As Double = startBearing

            For i As Integer = 0 To n - 1
                If i > 0 Then
                    Dim bbPrev As Double = (legs(i - 1).ForwardBearing + 180.0) Mod 360.0
                    fb = (bbPrev + includedAngles(i)) Mod 360.0
                    If fb < 0 Then fb += 360.0
                End If
                Dim radAng As Double = fb * Math.PI / 180.0
                legs(i).FromStation    = stationNames(i)
                legs(i).ToStation      = stationNames((i + 1) Mod n)
                legs(i).IncludedAngle  = includedAngles(i)
                legs(i).Distance       = distances(i)
                legs(i).ForwardBearing = fb
                legs(i).BackBearing    = (fb + 180.0) Mod 360.0
                legs(i).DN             = distances(i) * Math.Cos(radAng)
                legs(i).DE             = distances(i) * Math.Sin(radAng)
            Next

            ' --- Linear misclosure ---
            Dim sumDN As Double = 0
            Dim sumDE As Double = 0
            Dim perim As Double = 0
            For i As Integer = 0 To n - 1
                sumDN += legs(i).DN
                sumDE += legs(i).DE
                perim += legs(i).Distance
            Next
            Dim linMisc  As Double = Math.Sqrt(sumDN ^ 2 + sumDE ^ 2)
            Dim accDenom As Double = If(linMisc = 0, 0, perim / linMisc)

            lblLinearVal.Text   = String.Format("{0:0.0000} m  (eN={1:0.0000}, eE={2:0.0000})", linMisc, sumDN, sumDE)
            lblAccuracyVal.Text = If(linMisc = 0, "Perfect Closure", String.Format("1 : {0:0}", accDenom))

            ' --- Bowditch corrections ---
            For i As Integer = 0 To n - 1
                legs(i).CorrDN = -sumDN * (legs(i).Distance / perim)
                legs(i).CorrDE = -sumDE * (legs(i).Distance / perim)
                legs(i).AdjDN  = legs(i).DN + legs(i).CorrDN
                legs(i).AdjDE  = legs(i).DE + legs(i).CorrDE
            Next

            ' --- Final coordinates ---
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
                finalN(legs(i).ToStation) = curN
                finalE(legs(i).ToStation) = curE
            Next

            ' --- Shoelace area ---
            Dim area As Double = 0
            For i As Integer = 0 To n - 1
                Dim s1 As String = stationNames(i)
                Dim s2 As String = stationNames((i + 1) Mod n)
                area += finalE(s1) * finalN(s2) - finalE(s2) * finalN(s1)
            Next
            area = Math.Abs(area) / 2.0
            lblAreaVal.Text = String.Format("{0:0.000} m²  ({1:0.0000} ha)", area, area / 10000.0)

            ' --- Populate results grid ---
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



    '==========================================================================
    '  HELPERS
    '==========================================================================
    Private Function ParseDMS(text As String) As Double
        text = text.Trim()
        If text.Contains(" ") Then
            Dim p() As String = text.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
            Dim d  As Double  = Convert.ToDouble(p(0))
            Dim m  As Double  = If(p.Length > 1, Convert.ToDouble(p(1)), 0)
            Dim s  As Double  = If(p.Length > 2, Convert.ToDouble(p(2)), 0)
            Dim sg As Double  = If(d < 0, -1.0, 1.0)
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
        If secs >= 60.0 Then
            secs = 0
            mins += 1
        End If
        If mins >= 60 Then
            mins = 0
            deg  += 1
        End If
        Return String.Format("{0}{1}{2}'{3:00.0}""",
                             sign,
                             deg.ToString() & ChrW(176),
                             mins.ToString("00"),
                             secs)
    End Function

End Class



'==============================================================================
'  ModernMenuColors
'==============================================================================
Public Class ModernMenuColors
    Inherits ProfessionalColorTable

    Public Overrides ReadOnly Property MenuItemSelected As Color
        Get
            Return Color.FromArgb(238, 242, 255)
        End Get
    End Property

    Public Overrides ReadOnly Property MenuBorder As Color
        Get
            Return Color.FromArgb(228, 228, 231)
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemBorder As Color
        Get
            Return Color.FromArgb(199, 210, 254)
        End Get
    End Property

    Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
        Get
            Return Color.White
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
        Get
            Return Color.White
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
        Get
            Return Color.White
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
        Get
            Return Color.White
        End Get
    End Property
End Class

'==============================================================================
'  InputField — rounded bordered TextBox with animated focus ring
'==============================================================================
Public Class InputField
    Inherits Panel

    Private _editBox   As TextBox
    Private isFocused  As Boolean = False
    Private focusColor As Color   = Color.FromArgb(79,  70, 229)
    Private restColor  As Color   = Color.FromArgb(228, 228, 231)

    Public ReadOnly Property EditBox As TextBox
        Get
            Return _editBox
        End Get
    End Property

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer, True)
        Me.BackColor = Color.White
        Me.Padding   = New Padding(10, 0, 10, 0)

        _editBox = New TextBox With {
            .BorderStyle = BorderStyle.None,
            .BackColor   = Color.White,
            .Dock        = DockStyle.Fill,
            .ForeColor   = Color.FromArgb(15, 23, 42)
        }

        AddHandler _editBox.Enter,
            Sub(s As Object, ev As EventArgs)
                isFocused = True
                Invalidate()
            End Sub

        AddHandler _editBox.Leave,
            Sub(s As Object, ev As EventArgs)
                isFocused = False
                Invalidate()
            End Sub

        Me.Controls.Add(_editBox)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim r   As New Rectangle(0, 0, Width - 1, Height - 1)
        Dim rad As Integer = 8
        Using path As New GraphicsPath()
            path.AddArc(r.X,             r.Y,             rad * 2, rad * 2, 180, 90)
            path.AddArc(r.Right - rad*2, r.Y,             rad * 2, rad * 2, 270, 90)
            path.AddArc(r.Right - rad*2, r.Bottom - rad*2, rad * 2, rad * 2, 0,   90)
            path.AddArc(r.X,             r.Bottom - rad*2, rad * 2, rad * 2, 90,  90)
            path.CloseFigure()
            Using br As New SolidBrush(Color.White)
                g.FillPath(br, path)
            End Using
            Using pen As New Pen(If(isFocused, focusColor, restColor),
                                 If(isFocused, 1.6F, 1.0F))
                g.DrawPath(pen, path)
            End Using
        End Using
    End Sub
End Class



'==============================================================================
'  RoundButton — owner-painted with timer-driven hover animation
'==============================================================================
Public Class RoundButton
    Inherits Button

    Public Property FillColor   As Color   = Color.FromArgb(79, 70, 229)
    Public Property HoverColor  As Color   = Color.FromArgb(67, 56, 202)
    Public Property PressColor  As Color   = Color.FromArgb(55, 48, 163)
    Public Property TextColor   As Color   = Color.White
    Public Property BorderColor As Color   = Color.FromArgb(228, 228, 231)
    Public Property HasBorder   As Boolean = False
    Public Property Radius      As Integer = 10

    Private hoverT    As Double  = 0.0
    Private targetT   As Double  = 0.0
    Private _pressed  As Boolean = False
    Private animTimer As Timer

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or
                 ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer, True)
        FlatStyle = FlatStyle.Flat
        FlatAppearance.BorderSize = 0
        animTimer = New Timer With {.Interval = 15}
        AddHandler animTimer.Tick, AddressOf OnAnimTick
    End Sub

    Private Sub OnAnimTick(sender As Object, e As EventArgs)
        Const stepV As Double = 0.22
        If hoverT < targetT Then
            hoverT = Math.Min(targetT, hoverT + stepV)
        ElseIf hoverT > targetT Then
            hoverT = Math.Max(targetT, hoverT - stepV)
        End If
        hoverT = Math.Max(0.0, Math.Min(1.0, hoverT))  ' hard clamp — prevents Lerp overflow
        Invalidate()
        If hoverT = targetT Then animTimer.Stop()
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        targetT = 1.0
        If Not animTimer.Enabled Then animTimer.Start()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        targetT  = 0.0
        _pressed = False
        If Not animTimer.Enabled Then animTimer.Start()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        _pressed = True
        Invalidate()
        MyBase.OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        _pressed = False
        Invalidate()
        MyBase.OnMouseUp(e)
    End Sub

    Private Function Lerp(a As Color, b As Color, t As Double) As Color
        Dim tc As Double = Math.Max(0.0, Math.Min(1.0, t))
        ' Clamp the Double result to [0,255] BEFORE CInt to prevent overflow
        Dim r As Integer = CInt(Math.Max(0.0, Math.Min(255.0, a.R + (b.R - a.R) * tc)))
        Dim g As Integer = CInt(Math.Max(0.0, Math.Min(255.0, a.G + (b.G - a.G) * tc)))
        Dim bl As Integer = CInt(Math.Max(0.0, Math.Min(255.0, a.B + (b.B - a.B) * tc)))
        Return Color.FromArgb(r, g, bl)
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode      = SmoothingMode.AntiAlias
        g.TextRenderingHint  = Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim bg  As Color    = If(_pressed, PressColor, Lerp(FillColor, HoverColor, hoverT))
        Dim r   As New Rectangle(0, 0, Width - 1, Height - 1)
        Dim rad As Integer  = Math.Min(Radius * 2, Height)

        Using path As New GraphicsPath()
            path.AddArc(r.X,             r.Y,             rad, rad, 180, 90)
            path.AddArc(r.Right - rad,   r.Y,             rad, rad, 270, 90)
            path.AddArc(r.Right - rad,   r.Bottom - rad,  rad, rad, 0,   90)
            path.AddArc(r.X,             r.Bottom - rad,  rad, rad, 90,  90)
            path.CloseFigure()

            Using br As New SolidBrush(bg)
                g.FillPath(br, path)
            End Using
            If HasBorder Then
                Using pen As New Pen(BorderColor, 1)
                    g.DrawPath(pen, path)
                End Using
            End If
            g.SetClip(path)
        End Using

        Dim sf As New StringFormat With {
            .Alignment     = StringAlignment.Center,
            .LineAlignment = StringAlignment.Center
        }
        Using tb As New SolidBrush(TextColor)
            g.DrawString(Text, Font, tb, RectangleF.op_Implicit(r), sf)
        End Using
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If animTimer IsNot Nothing Then animTimer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class



'==============================================================================
'  ShadowPanel — rounded card with soft multi-pass drop shadow
'==============================================================================
Public Class ShadowPanel
    Inherits Panel

    Public Const SHADOW_DEPTH   As Integer = 6
    Public Const CORNER_RADIUS  As Integer = 14

    Public Sub New()
        SetStyle(ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.AllPaintingInWmPaint, True)
    End Sub

    Protected Overrides ReadOnly Property CreateParams() As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or &H20   ' WS_EX_TRANSPARENT
            Return cp
        End Get
    End Property

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        MyBase.OnPaintBackground(e)
    End Sub

    Private Function RoundedRect(r As Rectangle, rad As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d    As Integer = rad * 2
        path.AddArc(r.X,         r.Y,          d, d, 180, 90)
        path.AddArc(r.Right - d, r.Y,          d, d, 270, 90)
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0,   90)
        path.AddArc(r.X,         r.Bottom - d, d, d, 90,  90)
        path.CloseFigure()
        Return path
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        ' 6 shadow passes, alpha fades 14 → ~2
        For i As Integer = SHADOW_DEPTH To 1 Step -1
            Dim alpha As Integer = CInt(14 * (1 - (i - 1) / CDbl(SHADOW_DEPTH)))
            Dim sr As New Rectangle(i, i, Width - SHADOW_DEPTH - 1, Height - SHADOW_DEPTH - 1)
            Using sp As GraphicsPath = RoundedRect(sr, CORNER_RADIUS)
                Using sb As New SolidBrush(Color.FromArgb(alpha, 15, 23, 42))
                    g.FillPath(sb, sp)
                End Using
            End Using
        Next

        ' White card fill + hairline border
        Dim cr As New Rectangle(0, 0, Width - SHADOW_DEPTH, Height - SHADOW_DEPTH)
        Using cp As GraphicsPath = RoundedRect(cr, CORNER_RADIUS)
            Using wb As New SolidBrush(BackColor)
                g.FillPath(wb, cp)
            End Using
            Using bp As New Pen(Color.FromArgb(228, 228, 231), 1)
                g.DrawPath(bp, cp)
            End Using
        End Using
    End Sub
End Class
