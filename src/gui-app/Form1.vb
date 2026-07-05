'==================================================================================
'  SVY 323 - COMPUTER APPLICATION I
'  TRAVERSE COMPUTATION PROGRAMME (Closed Traverse) - WINDOWS FORMS (GUI) VERSION
'  Language : Visual Basic .NET (VB 2019 or higher) - Windows Forms App
'
'  HOW TO USE THIS FILE
'  ---------------------
'  1. In Visual Studio: File > New > Project > "Windows Forms App" (Visual Basic).
'  2. In Solution Explorer, DELETE the auto-generated "Form1.Designer.vb" file
'     (right-click it > Delete) - it is not needed because this single file
'     builds the whole user interface in code.
'  3. Open the auto-generated "Form1.vb" and REPLACE its entire contents with
'     everything in this file.
'  4. Press F5 (Start) to run. The input grid is pre-loaded with sample data
'     for five stations (A-B-C-D-E) - click "Compute Traverse" to see results,
'     or edit the grid with your own group's field data first.
'
'  WHAT THE PROGRAMME DOES
'  ------------------------
'   1. Angular misclosure check       (Sum of interior angles vs (n-2)*180)
'   2. Forward & back bearings        for every traverse leg
'   3. Latitudes (DN) & Departures(DE) for every leg
'   4. Linear misclosure & accuracy   (1 : X)
'   5. Bowditch (Compass Rule) corrections, proportional to leg length
'   6. Adjusted DN/DE and Final Northings & Eastings of every station
'   7. Area of the closed traverse polygon (Coordinate / Shoelace method)
'==================================================================================

Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Collections.Generic

Public Class Form1
    Inherits Form

    ' ---------------- Controls ----------------
    Private WithEvents btnCompute As Button
    Private WithEvents btnLoadSample As Button
    Private WithEvents btnAddRow As Button
    Private WithEvents btnRemoveRow As Button

    Private inputGrid As DataGridView
    Private resultsGrid As DataGridView

    Private txtStartBearing As TextBox
    Private txtStartN As TextBox
    Private txtStartE As TextBox

    Private lblAngularSum As Label
    Private lblAngularMisclosure As Label
    Private lblLinearMisclosure As Label
    Private lblAccuracy As Label
    Private lblArea As Label

    ' ---------------- Result structure ----------------
    Structure LegResult
        Public FromStation As String
        Public ToStation As String
        Public IncludedAngle As Double
        Public Distance As Double
        Public ForwardBearing As Double
        Public BackBearing As Double
        Public DN As Double
        Public DE As Double
        Public CorrDN As Double
        Public CorrDE As Double
        Public AdjDN As Double
        Public AdjDE As Double
        Public FinalN As Double
        Public FinalE As Double
    End Structure

    Public Sub New()
        BuildUserInterface()
        LoadSampleData()
    End Sub

    ' ==================================================================================
    '  UI CONSTRUCTION (done entirely in code - no Designer.vb file required)
    ' ==================================================================================
    Private Sub BuildUserInterface()
        Me.Text = "SVY 323 - Closed Traverse Computation Programme"
        Me.Width = 1150
        Me.Height = 800
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Segoe UI", 9)

        ' ---- Top panel: start bearing / start coordinates ----
        Dim lblHeader As New Label With {
            .Text = "Starting Control Data (carried forward from initial control station):",
            .Left = 15, .Top = 12, .Width = 500, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        Me.Controls.Add(lblHeader)

        Dim lbl1 As New Label With {.Text = "Start Bearing (D M S):", .Left = 15, .Top = 40, .Width = 150}
        txtStartBearing = New TextBox With {.Left = 170, .Top = 37, .Width = 100, .Text = "60 0 0"}
        Dim lbl2 As New Label With {.Text = "Start Northing (m):", .Left = 290, .Top = 40, .Width = 130}
        txtStartN = New TextBox With {.Left = 420, .Top = 37, .Width = 90, .Text = "1000.000"}
        Dim lbl3 As New Label With {.Text = "Start Easting (m):", .Left = 525, .Top = 40, .Width = 120}
        txtStartE = New TextBox With {.Left = 645, .Top = 37, .Width = 90, .Text = "1000.000"}

        Me.Controls.AddRange({lbl1, txtStartBearing, lbl2, txtStartN, lbl3, txtStartE})

        ' ---- Buttons ----
        btnLoadSample = New Button With {.Text = "Load Sample Data", .Left = 760, .Top = 35, .Width = 150, .Height = 28}
        btnAddRow = New Button With {.Text = "Add Row", .Left = 920, .Top = 35, .Width = 90, .Height = 28}
        btnRemoveRow = New Button With {.Text = "Remove Row", .Left = 1015, .Top = 35, .Width = 105, .Height = 28}
        Me.Controls.AddRange({btnLoadSample, btnAddRow, btnRemoveRow})

        ' ---- Input grid ----
        Dim lblInput As New Label With {
            .Text = "Field Data Input (Station, Included Angle in D M S, Distance to NEXT station in metres) - minimum 5 rows:",
            .Left = 15, .Top = 75, .Width = 750, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        Me.Controls.Add(lblInput)

        inputGrid = New DataGridView With {
            .Left = 15, .Top = 100, .Width = 1105, .Height = 150,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows = False,
            .RowHeadersVisible = False
        }
        inputGrid.Columns.Add("Station", "Station")
        inputGrid.Columns.Add("Angle", "Included Angle (D M S)")
        inputGrid.Columns.Add("Distance", "Distance to Next Stn (m)")
        inputGrid.Columns(0).Width = 340
        inputGrid.Columns(1).Width = 340
        inputGrid.Columns(2).Width = 340
        Me.Controls.Add(inputGrid)

        ' ---- Compute button ----
        btnCompute = New Button With {
            .Text = "COMPUTE TRAVERSE", .Left = 15, .Top = 260, .Width = 220, .Height = 36,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold), .BackColor = Color.LightSteelBlue
        }
        Me.Controls.Add(btnCompute)

        ' ---- Summary labels ----
        lblAngularSum = New Label With {.Left = 260, .Top = 265, .Width = 420, .Text = "Sum of Angles / Misclosure: -"}
        lblAngularMisclosure = New Label With {.Left = 260, .Top = 285, .Width = 420, .Text = ""}
        lblLinearMisclosure = New Label With {.Left = 700, .Top = 265, .Width = 420, .Text = "Linear Misclosure: -"}
        lblAccuracy = New Label With {.Left = 700, .Top = 285, .Width = 420, .Text = "Accuracy: -"}
        lblArea = New Label With {.Left = 700, .Top = 305, .Width = 420, .Text = "Area: -", .Font = New Font("Segoe UI", 9, FontStyle.Bold)}
        lblAngularSum.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        lblLinearMisclosure.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        lblAccuracy.Font = New Font("Segoe UI", 9, FontStyle.Bold)

        Me.Controls.AddRange({lblAngularSum, lblAngularMisclosure, lblLinearMisclosure, lblAccuracy, lblArea})

        ' ---- Results grid ----
        Dim lblResults As New Label With {
            .Text = "Results: Bearings, Latitudes/Departures, Bowditch Corrections and Final Coordinates",
            .Left = 15, .Top = 340, .Width = 750, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        Me.Controls.Add(lblResults)

        resultsGrid = New DataGridView With {
            .Left = 15, .Top = 365, .Width = 1105, .Height = 380,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right,
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .RowHeadersVisible = False
        }
        Dim cols() As String = {"Leg", "Angle", "Dist(m)", "Fwd Bearing", "Back Bearing",
                                  "DN(m)", "DE(m)", "Corr.DN", "Corr.DE", "Adj.DN", "Adj.DE",
                                  "To Stn", "Final N", "Final E"}
        For Each c As String In cols
            resultsGrid.Columns.Add(c, c)
        Next
        Me.Controls.Add(resultsGrid)
    End Sub

    ' ==================================================================================
    '  SAMPLE DATA
    ' ==================================================================================
    Private Sub LoadSampleData() Handles btnLoadSample.Click
        inputGrid.Rows.Clear()
        inputGrid.Rows.Add("A", "108 0 0", "100.02")
        inputGrid.Rows.Add("B", "108 0 0", "99.97")
        inputGrid.Rows.Add("C", "108 0 0", "100.05")
        inputGrid.Rows.Add("D", "108 0 0", "99.94")
        inputGrid.Rows.Add("E", "108 0 0", "100.03")
        txtStartBearing.Text = "60 0 0"
        txtStartN.Text = "1000.000"
        txtStartE.Text = "1000.000"
    End Sub

    Private Sub AddRow_Click() Handles btnAddRow.Click
        inputGrid.Rows.Add("", "0 0 0", "0.00")
    End Sub

    Private Sub RemoveRow_Click() Handles btnRemoveRow.Click
        If inputGrid.Rows.Count > 0 And inputGrid.CurrentRow IsNot Nothing Then
            inputGrid.Rows.Remove(inputGrid.CurrentRow)
        End If
    End Sub

    ' ==================================================================================
    '  MAIN COMPUTATION - fired when "COMPUTE TRAVERSE" is clicked
    ' ==================================================================================
    Private Sub ComputeTraverse_Click() Handles btnCompute.Click
        Try
            Dim rowCount As Integer = inputGrid.Rows.Count
            If rowCount < 5 Then
                MessageBox.Show("Please enter at least 5 stations (5 included angles and 5 leg distances) as required by the assignment.",
                                 "Insufficient Data", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim n As Integer = rowCount
            Dim stationNames(n - 1) As String
            Dim includedAngles(n - 1) As Double
            Dim distances(n - 1) As Double

            For i As Integer = 0 To n - 1
                stationNames(i) = CStr(inputGrid.Rows(i).Cells(0).Value).Trim()
                includedAngles(i) = ParseDMS(CStr(inputGrid.Rows(i).Cells(1).Value))
                distances(i) = Convert.ToDouble(inputGrid.Rows(i).Cells(2).Value)
            Next

            Dim startBearing As Double = ParseDMS(txtStartBearing.Text)
            Dim startN As Double = Convert.ToDouble(txtStartN.Text)
            Dim startE As Double = Convert.ToDouble(txtStartE.Text)

            ' ---------------- 1. Angular misclosure ----------------
            Dim sumAngles As Double = 0
            For Each a As Double In includedAngles
                sumAngles += a
            Next
            Dim theoreticalSum As Double = (n - 2) * 180.0
            Dim angularMisclosure As Double = sumAngles - theoreticalSum

            lblAngularSum.Text = "Sum of Angles = " & DecimalToDMS(sumAngles) & "   (Theoretical = " & DecimalToDMS(theoreticalSum) & ")"
            lblAngularMisclosure.Text = "Angular Misclosure = " & DecimalToDMS(angularMisclosure)

            ' ---------------- 2. Bearings, DN, DE ----------------
            Dim legs(n - 1) As LegResult
            Dim fb As Double = startBearing

            For i As Integer = 0 To n - 1
                Dim fromStn As String = stationNames(i)
                Dim toStn As String = stationNames((i + 1) Mod n)

                If i > 0 Then
                    Dim bbPrev As Double = (legs(i - 1).ForwardBearing + 180.0) Mod 360.0
                    fb = (bbPrev - includedAngles(i)) Mod 360.0
                    If fb < 0 Then fb += 360.0
                End If

                Dim bb As Double = (fb + 180.0) Mod 360.0
                Dim dist As Double = distances(i)
                Dim rad As Double = fb * Math.PI / 180.0

                legs(i).FromStation = fromStn
                legs(i).ToStation = toStn
                legs(i).IncludedAngle = includedAngles(i)
                legs(i).Distance = dist
                legs(i).ForwardBearing = fb
                legs(i).BackBearing = bb
                legs(i).DN = dist * Math.Cos(rad)
                legs(i).DE = dist * Math.Sin(rad)
            Next

            ' ---------------- 3. Linear misclosure & accuracy ----------------
            Dim sumDN As Double = 0, sumDE As Double = 0, perimeter As Double = 0
            For i As Integer = 0 To n - 1
                sumDN += legs(i).DN
                sumDE += legs(i).DE
                perimeter += legs(i).Distance
            Next
            Dim linearMisclosure As Double = Math.Sqrt(sumDN ^ 2 + sumDE ^ 2)
            Dim accuracyDenominator As Double = If(linearMisclosure = 0, 0, perimeter / linearMisclosure)

            lblLinearMisclosure.Text = String.Format("Linear Misclosure = {0:0.0000} m  (eN={1:0.0000}, eE={2:0.0000})", linearMisclosure, sumDN, sumDE)
            lblAccuracy.Text = If(linearMisclosure = 0, "Accuracy = Perfect Closure", String.Format("Linear Accuracy = 1 : {0:0}", accuracyDenominator))

            ' ---------------- 4. Bowditch adjustment ----------------
            For i As Integer = 0 To n - 1
                legs(i).CorrDN = -sumDN * (legs(i).Distance / perimeter)
                legs(i).CorrDE = -sumDE * (legs(i).Distance / perimeter)
                legs(i).AdjDN = legs(i).DN + legs(i).CorrDN
                legs(i).AdjDE = legs(i).DE + legs(i).CorrDE
            Next

            ' ---------------- 5. Final coordinates ----------------
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

            ' ---------------- 6. Area (Shoelace / coordinate method) ----------------
            Dim area As Double = 0
            For i As Integer = 0 To n - 1
                Dim s1 As String = stationNames(i)
                Dim s2 As String = stationNames((i + 1) Mod n)
                area += finalE(s1) * finalN(s2) - finalE(s2) * finalN(s1)
            Next
            area = Math.Abs(area) / 2.0
            lblArea.Text = String.Format("Area = {0:0.000} sq.m  ({1:0.0000} hectares)", area, area / 10000.0)

            ' ---------------- 7. Populate results grid ----------------
            resultsGrid.Rows.Clear()
            For i As Integer = 0 To n - 1
                resultsGrid.Rows.Add(
                    legs(i).FromStation & "-" & legs(i).ToStation,
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
                    legs(i).FinalE.ToString("0.000")
                )
            Next

        Catch ex As Exception
            MessageBox.Show("Error in input data: " & ex.Message, "Computation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ==================================================================================
    '  HELPER FUNCTIONS
    ' ==================================================================================

    ''' <summary>Parses a "D M S" (degrees minutes seconds, space separated) or a
    ''' plain decimal-degree string into decimal degrees.</summary>
    Private Function ParseDMS(text As String) As Double
        text = text.Trim()
        If text.Contains(" ") Then
            Dim parts() As String = text.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
            Dim deg As Double = Convert.ToDouble(parts(0))
            Dim mins As Double = If(parts.Length > 1, Convert.ToDouble(parts(1)), 0)
            Dim secs As Double = If(parts.Length > 2, Convert.ToDouble(parts(2)), 0)
            Dim sign As Double = If(deg < 0, -1.0, 1.0)
            Return sign * (Math.Abs(deg) + mins / 60.0 + secs / 3600.0)
        Else
            Return Convert.ToDouble(text)
        End If
    End Function

    ''' <summary>Formats decimal degrees as D°MM'SS.S" for display.</summary>
    Private Function DecimalToDMS(decimalDeg As Double) As String
        Dim d As Double = decimalDeg
        Dim deg As Integer = CInt(Math.Truncate(d))
        Dim minFull As Double = Math.Abs(d - deg) * 60.0
        Dim mins As Integer = CInt(Math.Truncate(minFull))
        Dim secs As Double = (minFull - mins) * 60.0
        Return String.Format("{0}{1}'{2:00.0}""", deg.ToString() & ChrW(176), mins.ToString("00"), secs)
    End Function

End Class

' ==================================================================================
'  APPLICATION ENTRY POINT
'  (Visual Studio's Windows Forms App template normally provides this
'   automatically in "Program.vb" / "ApplicationEvents.vb". If your project
'   does not already start Form1 automatically, make sure "Form1" is set as
'   the Startup Form under Project Properties > Application.)
' ==================================================================================
